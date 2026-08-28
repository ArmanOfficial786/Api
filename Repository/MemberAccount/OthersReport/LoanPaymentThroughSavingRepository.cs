
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport;
using System.Data;

namespace NexgenCosysReport.Repository.MemberAccount.OthersReport
{
    public class LoanPaymentThroughSavingRepository : ILoanPaymentThroughSavingRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanPaymentThroughSavingRepository> _logger;

        public LoanPaymentThroughSavingRepository(AppDbContext context, IDateConverterService dateConverter, ILogger<LoanPaymentThroughSavingRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<LoanPaymentThroughSavingData> GetReportDataAsync(LoanPaymentThroughSavingRequestDto request)
        {
            try
            {
                // Convert Nepali dates to English
                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                // Build branch filter string
                string branchIdsStr = "-1";
                if (request.BranchIds.Count > 0)
                {
                    branchIdsStr = string.Join(",", request.BranchIds);
                }

                // Get branch names for header
                var branchNames = await GetBranchNamesListAsync(request.BranchIds);
                string branchNameDisplay = branchNames.Count > 0
                    ? (branchNames.Count == 1 ? branchNames[0] : string.Join(", ", branchNames))
                    : "All Branches";

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Determine stored procedure based on report view
                string spName = request.ReportView == "Horizontal"
                    ? "sp_5_43_GetLoanPaymentThroughSavingHorizontal"
                    : "sp_5_43_GetLoanPaymentThroughSaving";

                // Build filter expression
                var sqlFilterExp = $" And t.TransactionOn between '{fromDateStr}' And '{toDateStr}' AND t.UsmOfficeId in ({branchIdsStr})";

                // Build Order By clause
                var orderByClause = BuildOrderByClause(request.OrderBy);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrder", orderByClause, DbType.String, size: -1);

                var rows = await connection.QueryAsync<LoanPaymentThroughSavingRowDto>(
                    spName,
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var resultList = rows.AsList();

                return new LoanPaymentThroughSavingData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalNetAmount = resultList.Sum(r => r.NetAmount ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchNames = branchNameDisplay,
                    BranchIds = request.BranchIds,
                    OrderBy = request.OrderBy,
                    ReportView = request.ReportView
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync");
                throw;
            }
        }

        public async Task<List<string>> GetBranchNamesListAsync(List<long> branchIds)
        {
            try
            {
                if (branchIds.Count == 0)
                    return ["All Branches"];

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var ids = string.Join(",", branchIds);
                var sql = $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({ids}) ORDER BY OfficeName";

                var names = await connection.QueryAsync<string>(sql);
                return names.AsList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetBranchNamesListAsync");
                return ["All Branches"];
            }
        }

        private static string BuildOrderByClause(string orderBy)
        {
            return orderBy switch
            {
                "Member Id" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId",
                "Member Name" => " order by MemberName",
                "Account No" => " order by substring(AccountNo, 1,(len(AccountNo)-charindex('-', AccountNo))-1), AccountNo",
                "Type" => " order by Type",
                "Amount" => " order by NetAmount DESC",
                "Date" => " order by Date",
                "Operator" => " order by Operator",
                _ => " order by MemberId"
            };
        }
    }
}