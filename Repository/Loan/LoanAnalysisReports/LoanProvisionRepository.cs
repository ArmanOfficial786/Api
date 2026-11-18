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
    public class LoanProvisionRepository : ILoanProvisionRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanProvisionRepository> _logger;

        public LoanProvisionRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanProvisionRepository> logger)
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

        // Webform radio values: S = Schedule Wise (default), R = Remaining Principal, A = After Maturity
        private static string NormalizePenaltyType(string? penaltyType) =>
            penaltyType?.Trim().ToUpperInvariant() switch
            {
                "R" => "R",
                "A" => "A",
                _ => "S"
            };

        private static string GetPenaltyTypeName(string penaltyType) =>
            penaltyType switch
            {
                "R" => "Remaining Principal",
                "A" => "After Maturity",
                _ => "Schedule Wise"
            };

        public async Task<LoanProvisionData> GetReportDataAsync(LoanProvisionRequestDto request, long userId)
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

                // 3. Office filter is always applied; member group appended when selected
                var sqlFilterExpBranchId = $" And v.UsmOfficeId in ({string.Join(",", officeIds)})";

                string? memberGroupName = null;
                if (long.TryParse(request.MemberGroupId, out var memberGroupId) && memberGroupId > 0)
                {
                    sqlFilterExpBranchId += $" AND MR.SycMemberGroupId = {memberGroupId}";

                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id",
                        new { Id = memberGroupId });
                }

                // 4. Penalty type
                var penaltyType = NormalizePenaltyType(request.PenaltyType);

                _logger.LogInformation(
                    "SP sp_7_16_LoanProvisionReport | exp={Exp} | branch={Branch} | penalty={Penalty}",
                    sqlFilterExp, sqlFilterExpBranchId, penaltyType);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpbranchId", sqlFilterExpBranchId, DbType.String, size: -1);
                parameters.Add("@SqlPenaltyType", $"'{penaltyType}'", DbType.String, size: -1);

                var rows = (await connection.QueryAsync<LoanProvisionRowDto>(
                    "sp_7_16_LoanProvisionReport",
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

                return new LoanProvisionData
                {
                    Rows = rows,
                    TotalRecords = rows.Count,
                    TotalAmount = rows.Sum(r => r.Amount ?? 0),
                    TotalProvisionAmount = rows.Sum(r => r.ProvisionAmount ?? 0),
                    TotalLoans = rows.Sum(r => r.NoofLoan ?? 0),
                    TillDateBs = request.TillDateBs,
                    BranchName = branchName,
                    MemberGroupName = memberGroupName,
                    PenaltyType = penaltyType,
                    PenaltyTypeName = GetPenaltyTypeName(penaltyType)
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