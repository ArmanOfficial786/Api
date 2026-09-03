// Repository/MemberAccount/OthersReport/SavingTransferRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport;
using System.Data;

namespace NexgenCosysReport.Repository.MemberAccount.OthersReport
{
    public class SavingTransferRepository : ISavingTransferRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly IBranchNameResolverService _branchNameResolver;
        private readonly ILogger<SavingTransferRepository> _logger;

        public SavingTransferRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            IBranchNameResolverService branchNameResolver,
            ILogger<SavingTransferRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _branchNameResolver = branchNameResolver;
            _logger = logger;
        }

        public async Task<SavingTransferData> GetReportDataAsync(SavingTransferRequestDto request)
        {
            try
            {
                // Convert Nepali dates to English
                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                // Build branch filter string (BranchIds is a nullable comma-separated string, e.g. "1,2,3" or "-1")
                string branchIdsStr = "-1";
                if (!string.IsNullOrEmpty(request.BranchIds) &&
                    request.BranchIds != "-1" &&
                    request.BranchIds != "string")
                {
                    branchIdsStr = request.BranchIds;
                }

                // Get branch names for header via shared resolver
                var branchNameDisplay = await _branchNameResolver.GetBranchNamesAsync(branchIdsStr);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();


                // Prepare parameters for the stored procedure
                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", $" And t.TransactionOn between '{fromDateStr}' And '{toDateStr}' AND t.UsmOfficeId in ({branchIdsStr})", DbType.String, size: -1);

                // Build Order By clause
                var orderByClause = BuildOrderByClause(request.OrderBy);
                parameters.Add("@SqlFilterExpOrder", orderByClause, DbType.String, size: -1);

                // Explicit, generous CommandTimeout as a safety net (default is 30s).
                // Backstop only - snapshot isolation above is the actual fix.
                var command = new CommandDefinition(
                    "sp_5_43_GetSavingTransfer",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120);

                var rows = await connection.QueryAsync<SavingTransferRowDto>(command);

                var resultList = rows.AsList();

                return new SavingTransferData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalAmount = resultList.Sum(r => r.Amount ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchNames = branchNameDisplay,
                    BranchIds = request.BranchIds,
                    OrderBy = request.OrderBy
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync");
                throw;
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
                "Amount" => " order by Amount DESC",
                "Date" => " order by Date",
                "Operator" => " order by Operator",
                _ => " order by MemberId"
            };
        }
    }
}