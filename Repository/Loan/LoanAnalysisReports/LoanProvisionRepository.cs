
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

        private static string SanitizeBranchIds(string? branchIds)
        {
            if (string.IsNullOrWhiteSpace(branchIds) || branchIds == "-1" || branchIds == "string")
                return "-1";

            var validIds = branchIds
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => long.TryParse(id, out _));

            var joined = string.Join(",", validIds);
            return string.IsNullOrEmpty(joined) ? "-1" : joined;
        }

        private static string GetPenaltyTypeName(string penaltyType)
        {
            return penaltyType?.Trim().ToUpper() switch
            {
                "R" => "Remaining Principal",
                "A" => "After Maturity",
                "S" => "Schedule Wise",
                _ => "Schedule Wise"
            };
        }

        public async Task<LoanProvisionData> GetReportDataAsync(LoanProvisionRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.TillDateBs) || request.TillDateBs == "-1")
                {
                    throw new ArgumentException("Till Date is required.");
                }

                var branchIds = SanitizeBranchIds(request.BranchIds);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDateBs);
                var tillDateStr = $"'{tillDateAd:yyyy-MM-dd}'";


                var sqlFilterExp = tillDateStr;

                var sqlFilterExpBranchId = string.Empty;
                if (branchIds != "-1")
                {
                    sqlFilterExpBranchId += $" And v.UsmOfficeId in ({branchIds})";
                }

                if (!string.IsNullOrWhiteSpace(request.MemberGroupId) &&
                    request.MemberGroupId != "-1" &&
                    request.MemberGroupId != "string" &&
                    long.TryParse(request.MemberGroupId, out var memberGroupId))
                {
                    sqlFilterExpBranchId += $" AND MR.SycMemberGroupId = {memberGroupId}";
                }

                var sqlPenaltyType = $"'{request.PenaltyType.Trim().ToUpper()}'";

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpbranchId", sqlFilterExpBranchId, DbType.String, size: -1);
                parameters.Add("@SqlPenaltyType", sqlPenaltyType, DbType.String, size: -1);

                var rows = await connection.QueryAsync<LoanProvisionRowDto>(
                    "sp_7_16_LoanProvisionReport",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var resultList = rows.AsList();

                string branchName = "All";
                if (branchIds != "-1")
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
                }


                string? memberGroupName = null;
                if (!string.IsNullOrWhiteSpace(request.MemberGroupId) &&
                    request.MemberGroupId != "-1" &&
                    request.MemberGroupId != "string" &&
                    long.TryParse(request.MemberGroupId, out var memberGroupIdForName))
                {
                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id",
                        new { Id = memberGroupIdForName });
                }

                return new LoanProvisionData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalAmount = resultList.Sum(r => r.Amount ?? 0),
                    TotalProvisionAmount = resultList.Sum(r => r.ProvisionAmount ?? 0),
                    TotalLoans = resultList.Sum(r => r.NoofLoan ?? 0),
                    TillDateBs = request.TillDateBs,
                    BranchName = branchName,
                    MemberGroupName = memberGroupName,
                    PenaltyType = request.PenaltyType,
                    PenaltyTypeName = GetPenaltyTypeName(request.PenaltyType)
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