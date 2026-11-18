using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport;
using System.Data;

namespace NexgenCosysReport.Repository.Loan.LoanAnalysisReport
{
    public class LoanAgingArrealCalculationRepository : ILoanAgingArrealCalculationRepository
    {
        private const string NormalSp = "sp_7_16_LoanAgingArrealCalculationReport";
        private const string OneToThirtySp = "sp_7_16_LoanAgingArreal1To30CalculationReport";

        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanAgingArrealCalculationRepository> _logger;

        public LoanAgingArrealCalculationRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanAgingArrealCalculationRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        // "1, 2,3" -> [1,2,3]; ignores "-1", "string" and non-numeric values
        private static List<long> ParseIds(string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
                return new List<long>();

            return csv
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => long.TryParse(s, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();
        }

        // Same mapping as the webform's ddl_orderBy switch
        private static string BuildSqlOrderBy(string? orderBy)
        {
            if (string.IsNullOrEmpty(orderBy) || orderBy == "-1")
                return string.Empty;

            return orderBy.Trim() switch
            {
                "MemberId" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId ",
                "FullName" => " order by FullName ",
                "LoanAccountNo" => " order by substring(LoanAccountNo, 1,(len(LoanAccountNo)-charindex('-', LoanAccountNo))-1), LoanAccountNo ",
                "LoanIssueAmount" => " order by LoanIssueAmount DESC",
                "BalanceAmount" => " order by BalanceAmount DESC",
                "Noofdays" => " order by Noofdays DESC",
                _ => string.Empty
            };
        }

        public async Task<LoanAgingArrealCalculationData> GetReportDataAsync(
            LoanAgingArrealCalculationRequestDto request, long userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.TillDateBs) || request.TillDateBs == "-1")
                    throw new ArgumentException("Till Date is required.");

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

                // 2. Till date -> SP @SqlFilterExp (quoted date, as in the BLL)
                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDateBs);
                var sqlFilterExp = $"'{tillDateAd:yyyy-MM-dd}'";

                // 3. Office filter (always), then collector, then collection center, in the BLL's order
                var sqlFilterExpBranchId = $" And LS.UsmOfficeId in({string.Join(",", officeIds)})";

                string? collectorName = null;
                if (long.TryParse(request.CollectorId, out var collectorId) && collectorId > 0)
                {
                    sqlFilterExpBranchId += $" and LS.HurCollectorId = {collectorId}";

                    collectorName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT CollectorFullName FROM HurCollector WHERE HurCollectorId = @Id",
                        new { Id = collectorId });
                }

                string? collectionCenterName = null;
                var centerIds = ParseIds(request.CollectionCenterId);
                if (centerIds.Count > 0)
                {
                    sqlFilterExpBranchId += $" And LS.SycCollectionCenterId in({string.Join(",", centerIds)})";

                    var centerNames = await connection.QueryAsync<string>(
                        "SELECT CollectionCenterName FROM SycCollectionCenter WHERE SycCollectionCenterId IN @Ids",
                        new { Ids = centerIds });
                    var centerNameList = centerNames.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    collectionCenterName = centerNameList.Count > 0 ? string.Join(", ", centerNameList) : null;
                }

                // 4. Order by, penalty type (sent as the webform sends it: S / R / A), and which SP
                var sqlFilterExpOrderby = BuildSqlOrderBy(request.OrderBy);
                var penaltyType = request.PenaltyType;
                var spName = request.Enable1To30Days ? OneToThirtySp : NormalSp;

                _logger.LogInformation(
                    "SP {Sp} | exp={Exp} | branch={Branch} | order={Order} | penalty={Penalty}",
                    spName, sqlFilterExp, sqlFilterExpBranchId, sqlFilterExpOrderby, penaltyType);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpbranchId", sqlFilterExpBranchId, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderby", sqlFilterExpOrderby, DbType.String, size: -1);
                // Quotes are doubled so a stray ' can't break the SP's dynamic SQL
                parameters.Add("@SqlPenaltyType", $"'{penaltyType?.Replace("'", "''")}'", DbType.String, size: -1);

                var rows = (await connection.QueryAsync<LoanAgingArrealCalculationRowDto>(
                    spName,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                )).AsList();

                // 5. Header branch names
                var names = await connection.QueryAsync<string>(
                    "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN @Ids",
                    new { Ids = officeIds });
                var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                var branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";

                return new LoanAgingArrealCalculationData
                {
                    Rows = rows,
                    TotalRecords = rows.Count,
                    TotalLoanIssueAmount = rows.Sum(r => r.LoanIssueAmount ?? 0),
                    TotalBalanceAmount = rows.Sum(r => r.BalanceAmount ?? 0),
                    TotalGoodloan = rows.Sum(r => r.Goodloan ?? 0),
                    TotalArrear = rows.Sum(r => r.Arrear ?? 0),
                    TotalArrearFor0 = rows.Sum(r => r.ArrearFor0 ?? 0),
                    TotalArrearfrm1to365 = rows.Sum(r => r.Arrearfrm1to365 ?? 0),
                    TotalArreargrtthan365 = rows.Sum(r => r.Arreargrtthan365 ?? 0),
                    TotalGoodAmt = rows.Sum(r => r.GoodAmt ?? 0),
                    TotalFrm1to365 = rows.Sum(r => r.Frm1to365 ?? 0),
                    TotalGrtthan365 = rows.Sum(r => r.Grtthan365 ?? 0),
                    TotalProvision = rows.Sum(r => r.TotalProvision ?? 0),
                    TotalRescheduleAmt = rows.Sum(r => r.RescheduleAmt ?? 0),
                    TillDateBs = request.TillDateBs,
                    BranchName = branchName,
                    PenaltyType = penaltyType,
                    CollectionCenterName = collectionCenterName,
                    CollectorName = collectorName,
                    Enable1To30Days = request.Enable1To30Days,
                    OrderBy = request.OrderBy
                };
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync");
                throw;
            }
        }
    }
}