// Repositories/Implementations/Account/IBTReports/IBTTransactionRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Account.IBTReports;
using NexgenCosysReport.Inteface.ServiceInterface.Account.IBTReports;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;

namespace NexgenCosysReport.Repository.Account.IBTReports
{
    public class IBTTransactionRepository : IIBTTransactionRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<IBTTransactionRepository> _logger;

        public IBTTransactionRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<IBTTransactionRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        // --------------------------------------------------------------
        // @orderBy — legacy passes " Order by " + <raw value> straight
        // into the SP, which concatenates it into dynamic SQL. Restrict
        // to the two known-safe column names from the dropdown rather
        // than forwarding arbitrary input.
        // --------------------------------------------------------------
        private static string BuildOrderBy(IBTTransactionRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1" || request.OrderBy == "string")
                return string.Empty;

            return request.OrderBy.Trim() switch
            {
                "FullName" => " Order by FullName",
                "MemMemberRegistrationOfficeName" => " Order by MemMemberRegistrationOfficeName",
                _ => string.Empty
            };
        }

        // --------------------------------------------------------------
        // @branch — legacy builds " and UO.OfficeName = '<branchName>'"
        // by string concatenation with the branch's DISPLAY TEXT, not an
        // ID. Since that flows directly into dynamic SQL executed by the
        // SP, resolve the office name from the DB by validated numeric
        // BranchId first — never forward a raw client-supplied string
        // into this fragment.
        //
        // Returns a tuple instead of using an `out` parameter, since
        // async methods cannot declare ref/in/out parameters in C#.
        // --------------------------------------------------------------
        private async Task<(string FilterClause, string BranchName)> BuildBranchFilterAsync(
            SqlConnection connection, IBTTransactionRequestDto request)
        {
            if (string.IsNullOrEmpty(request.BranchId) ||
                request.BranchId == "-1" ||
                request.BranchId == "string" ||
                !long.TryParse(request.BranchId, out var branchId))
            {
                return (string.Empty, "All");
            }

            var officeName = await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId = @BranchId",
                new { BranchId = branchId });

            if (string.IsNullOrEmpty(officeName))
                return (string.Empty, "All");

            // OfficeName is now a trusted DB value (not raw user input), safe to
            // embed via a parameterized value rather than string concatenation.
            return (" and UO.OfficeName = @OfficeNameFilter", officeName);
        }

        public async Task<IBTTransactionData> GetReportDataAsync(IBTTransactionRequestDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.FromDateBs) || string.IsNullOrEmpty(request.ToDateBs)
                    || request.FromDateBs == "-1" || request.ToDateBs == "-1")
                {
                    throw new ArgumentException("FromDateBs and ToDateBs are required.");
                }

                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                // ISO format avoids SQL Server regional/language ambiguity for string->date literals
                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var (branchFilterClause, branchNameForDisplay) = await BuildBranchFilterAsync(connection, request);
                var orderByClause = BuildOrderBy(request);

                // ---- SP declares 4 parameters: @FromDate, @ToDate, @branch, @orderBy ----
                // @branch stays a dynamic-SQL fragment for legacy compatibility, but the
                // office name embedded in it now comes from a resolved DB lookup instead
                // of unescaped user input; @OfficeNameFilter carries the actual value.
                var parameters = new DynamicParameters();
                parameters.Add("@FromDate", fromDateStr);
                parameters.Add("@ToDate", toDateStr);
                parameters.Add("@branch", branchFilterClause, DbType.String, size: -1);
                parameters.Add("@orderBy", orderByClause, DbType.String, size: -1);

                if (!string.IsNullOrEmpty(branchFilterClause))
                {
                    parameters.Add("@OfficeNameFilter", branchNameForDisplay);
                }

                var rows = await connection.QueryAsync<IBTTransactionRowDto>(
                    "sp_5_43_GetSavingIBTTransaction",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var resultList = rows.AsList();

                return new IBTTransactionData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalAmount = resultList.Sum(r => r.Amount ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchName = branchNameForDisplay,
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