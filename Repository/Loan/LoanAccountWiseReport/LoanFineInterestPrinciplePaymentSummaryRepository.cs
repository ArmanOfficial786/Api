using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.Loan.OtherReports
{
    public class LoanFineInterestPrinciplePaymentSummaryRepository : ILoanFineInterestPrinciplePaymentSummaryRepository
    {
        private const string PaymentSp = "sp_7_16_LoanFineInterestPrinciplePaymentReport";
        private const string SummarySp = "sp_7_16_LoanFineInterestPrinciplePaymentSumaryReport";
        private const string AllTransactionTypes = "5,6,7,20,21,22,39,40,41,44,45,46";
        private const string CashTransactionTypes = "5,6,7";

        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanFineInterestPrinciplePaymentSummaryRepository> _logger;

        public LoanFineInterestPrinciplePaymentSummaryRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanFineInterestPrinciplePaymentSummaryRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        private static bool IsSummary(string? reportType) =>
            string.Equals(reportType?.Trim(), "Summary", StringComparison.OrdinalIgnoreCase);

        private static string BuildSqlOrderBy(string? orderBy)
        {
            if (string.IsNullOrEmpty(orderBy) || orderBy == "-1")
                return string.Empty;

            return orderBy.Trim() switch
            {
                "MemberId" => "order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId ",
                "FullName" => "order by FullName",
                "LoanAccountNo" => "order by substring(LoanAccountNo, 1,(len(LoanAccountNo)-charindex('-', LoanAccountNo))-1), LoanAccountNo ",
                "LoanTypeName" => "order by LoanTypeName",
                "Fine" => "order by Fine DESC",
                "Interest" => "order by Interest DESC",
                "Principal" => "order by Principal DESC",
                "TotalAmount" => "order by TotalAmount DESC",
                "DateOnBs" => "order by DateOnBs",
                _ => string.Empty
            };
        }

        private static List<long> ParseIds(string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
                return new List<long>();

            return csv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => long.TryParse(s, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();
        }

        public async Task<LoanFineInterestPrinciplePaymentSummaryData> GetReportDataAsync(
            LoanFineInterestPrinciplePaymentSummaryRequestDto request,
            long userId)
        {
            try
            {
                var summary = IsSummary(request.ReportType);
                var spName = summary ? SummarySp : PaymentSp;

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // 1. Offices: webform checkbox list = CUsmOffice.GetByUsmUserId(userId)
                var allowedOfficeIds = (await connection.QueryAsync<long>(
                    "SELECT DISTINCT UsmOfficeId FROM UsmRelationUserToOffice WHERE UsmUserId = @UserId",
                    new { UserId = userId })).ToList();

                if (allowedOfficeIds.Count == 0)
                    throw new UnauthorizedAccessException("User has no office access.");

                var requestedOfficeIds = ParseIds(request.BranchIds);
                if (requestedOfficeIds.Except(allowedOfficeIds).Any())
                    throw new UnauthorizedAccessException("You do not have access to one or more selected branches.");

                // Nothing sent = webform "Select All" = all of the user's offices
                var officeIds = requestedOfficeIds.Count > 0 ? requestedOfficeIds : allowedOfficeIds;

                var sqlFilterExp = new StringBuilder();
                var sqlFilterExpBranchId = new StringBuilder();
                string? collectionCenterName = null;
                string? collectorName = null;

                // Always filter by office
                sqlFilterExpBranchId.Append(" And Tr.UsmOfficeId in(").Append(string.Join(",", officeIds)).Append(")");

                // 2. Collection center
                var centerIds = ParseIds(request.CollectionCenterId);
                if (centerIds.Count > 0)
                {
                    sqlFilterExpBranchId.Append(" And LS.SycCollectionCenterId in(").Append(string.Join(",", centerIds)).Append(")");

                    var centerNames = await connection.QueryAsync<string>(
                        "SELECT CollectionCenterName FROM SycCollectionCenter WHERE SycCollectionCenterId IN @Ids",
                        new { Ids = centerIds });
                    collectionCenterName = string.Join(", ", centerNames.Where(n => !string.IsNullOrWhiteSpace(n)));
                }

                // 3. Member (BtnViewReport) or date range (BtnViewReport1)
                if (!string.IsNullOrEmpty(request.MemberId))
                {
                    sqlFilterExp.Append(" And MR.MemberId = '").Append(request.MemberId).Append("'");
                }
                else if (!string.IsNullOrEmpty(request.FromDateBs) && !string.IsNullOrEmpty(request.ToDateBs)
                    && request.FromDateBs != "-1" && request.ToDateBs != "-1")
                {
                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                    sqlFilterExp.Append(" And Tr.TransactionOn between '").Append(fromDateAd.ToString("yyyy-MM-dd"))
                                .Append("' And '").Append(toDateAd.ToString("yyyy-MM-dd")).Append("'");
                }

                // 4. Collector
                if (long.TryParse(request.CollectorId, out var collectorId) && collectorId > 0)
                {
                    sqlFilterExp.Append(" and TR.HurCollectorId = ").Append(collectorId);

                    collectorName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT CollectorFullName FROM HurCollector WHERE HurCollectorId = @Id",
                        new { Id = collectorId });
                }

                // 5. All ("-1") / Cash only ("Cash")
                var onlyCash = string.Equals(request.SelectAllOrOnlyCash?.Trim(), "Cash", StringComparison.OrdinalIgnoreCase);
                var sqlFilterExpAllOrOnlyCash =
                    " and Tr.AcoTransactionTypeId in (" + (onlyCash ? CashTransactionTypes : AllTransactionTypes) + ")";

                var sqlFilterExpOrderBy = BuildSqlOrderBy(request.OrderBy);

                // Log what the SP receives so it can be compared with the webform in SSMS
                _logger.LogInformation(
                    "SP {Sp} | exp={Exp} | branch={Branch} | cash={Cash} | order={Order}",
                    spName, sqlFilterExp, sqlFilterExpBranchId, sqlFilterExpAllOrOnlyCash, sqlFilterExpOrderBy);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpbranchId", sqlFilterExpBranchId.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpAllOrOnlyCash", sqlFilterExpAllOrOnlyCash, DbType.String, size: -1);

                var rows = (await connection.QueryAsync<LoanFineInterestPrinciplePaymentSummaryRowDto>(
                    spName,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                )).AsList();

                // 6. Header branch names
                var names = await connection.QueryAsync<string>(
                    "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN @Ids",
                    new { Ids = officeIds });
                var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                var branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";

                return new LoanFineInterestPrinciplePaymentSummaryData
                {
                    ReportType = summary ? "Summary" : "Payment",
                    Rows = rows,
                    TotalRecords = rows.Count,
                    TotalFine = rows.Sum(r => r.Fine ?? 0),
                    TotalInterest = rows.Sum(r => r.Interest ?? 0),
                    TotalPrincipal = rows.Sum(r => r.Principal ?? 0),
                    TotalAmount = rows.Sum(r => r.TotalAmount ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchName = branchName,
                    CollectionCenterName = collectionCenterName,
                    CollectorName = collectorName,
                    SelectAllOrOnlyCash = request.SelectAllOrOnlyCash,
                    OrderBy = request.OrderBy
                };
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync ({ReportType})", request.ReportType);
                throw;
            }
        }
    }
}