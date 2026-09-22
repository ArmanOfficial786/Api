
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


        private static string BuildSqlOrderBy(LoanFineInterestPrinciplePaymentSummaryRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
                return string.Empty;

            return request.OrderBy.Trim() switch
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


        private static string SanitizeBranchIds(string? branchIds)
        {
            if (string.IsNullOrWhiteSpace(branchIds) || branchIds == "-1" || branchIds == "string")
                return string.Empty;

            var validIds = branchIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => long.TryParse(id, out _));

            return string.Join(",", validIds);
        }

        public async Task<LoanFineInterestPrinciplePaymentSummaryData> GetReportDataAsync(LoanFineInterestPrinciplePaymentSummaryRequestDto request)
        {
            try
            {
                var branchIds = SanitizeBranchIds(request.BranchIds);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sqlFilterExp = new StringBuilder();
                var sqlFilterExpBranchId = new StringBuilder();
                string? collectionCenterName = null;
                string? collectorName = null;

                if (request.MemberRegistrationId > 0)
                {
                    sqlFilterExp.Append(" And Mr.MemMemberRegistrationId = ").Append(request.MemberRegistrationId);
                }
                else if (!string.IsNullOrEmpty(request.FromDateBs) && !string.IsNullOrEmpty(request.ToDateBs)
                    && request.FromDateBs != "-1" && request.ToDateBs != "-1")
                {
                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                    var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                    var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                    sqlFilterExp.Append(" And Tr.TransactionOn between '").Append(fromDateStr)
                                .Append("' And '").Append(toDateStr).Append("'");
                }

                if (!string.IsNullOrEmpty(branchIds))
                {
                    sqlFilterExpBranchId.Append(" And Tr.UsmOfficeId in(").Append(branchIds).Append(")");
                }

                if (!string.IsNullOrEmpty(request.CollectionCenterId) && request.CollectionCenterId != "-1")
                {
                    sqlFilterExpBranchId.Append(" And LS.SycCollectionCenterId in(").Append(request.CollectionCenterId).Append(")");


                    collectionCenterName = await connection.QueryFirstOrDefaultAsync<string>(
                        $"SELECT STRING_AGG(CollectionCenterName, ', ') FROM SycCollectionCenter WHERE SycCollectionCenterId IN ({request.CollectionCenterId})");
                }

                if (!string.IsNullOrEmpty(request.CollectorId) && request.CollectorId != "-1")
                {
                    sqlFilterExp.Append(" and TR.HurCollectorId = ").Append(request.CollectorId);


                    collectorName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT CollectorFullName FROM HurCollector WHERE HurCollectorId = @Id",
                        new { Id = request.CollectorId });
                }

                var sqlFilterExpAllOrOnlyCash = new StringBuilder();
                if (request.SelectAllOrOnlyCash == "-1")
                {
                    sqlFilterExpAllOrOnlyCash.Append(" and Tr.AcoTransactionTypeId in (5,6,7,20,21,22,39,40,41,44,45,46)");
                }
                else
                {
                    sqlFilterExpAllOrOnlyCash.Append(" and Tr.AcoTransactionTypeId in (5,6,7)");
                }

                var sqlFilterExpOrderBy = BuildSqlOrderBy(request);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpbranchId", sqlFilterExpBranchId.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpAllOrOnlyCash", sqlFilterExpAllOrOnlyCash.ToString(), DbType.String, size: -1);

                var rows = (await connection.QueryAsync<LoanFineInterestPrinciplePaymentSummaryRowDto>(
                    "sp_7_16_LoanFineInterestPrinciplePaymentSumaryReport",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                )).AsList();

                string branchName = "All";
                if (!string.IsNullOrEmpty(branchIds))
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
                }

                return new LoanFineInterestPrinciplePaymentSummaryData
                {
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync");
                throw;
            }
        }
    }
}