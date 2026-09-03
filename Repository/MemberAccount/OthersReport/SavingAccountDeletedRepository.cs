// Repository/MemberAccount/OthersReport/SavingAccountDeletedRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.MemberAccount.OthersReport
{
    public class SavingAccountDeletedRepository : ISavingAccountDeletedRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<SavingAccountDeletedRepository> _logger;

        public SavingAccountDeletedRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<SavingAccountDeletedRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<SavingAccountDeletedData> GetReportDataAsync(SavingAccountDeletedRequestDto request)
        {
            try
            {
                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);
                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                var sqlFilterExp = new StringBuilder();

                // Date filter - Account opened date range (matches the SP's WHERE clause,
                // which filters on a.AccountOpenOn; the SP itself always adds
                // "WHERE a.IsDeleted='True'" so deleted-date range filtering is not
                // separately supported here).
                sqlFilterExp.Append($" AND a.AccountOpenOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");

                if (!string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
                {
                    sqlFilterExp.Append($" AND a.UsmOfficeId IN ({request.BranchIds})");
                }

                // DepositTypeName is always the leading sort key so rows arrive grouped
                // by deposit type first, matching the report's visual grouping.
                var orderByClause = BuildOrderByClause(request.OrderBy);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString() + orderByClause, DbType.String, size: -1);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var rawRows = (await connection.QueryAsync<SpRawRow>(
                    "sp_5_43_GetSavingAccountDeleted",
                    parameters,
                    commandType: CommandType.StoredProcedure
                )).ToList();

                var resultList = new List<SavingAccountDeletedRowDto>();
                foreach (var raw in rawRows)
                {
                    string? deletedDateBs = null;
                    if (raw.AccountDeletedDate.HasValue)
                    {
                        try
                        {
                            var deletedDateBsResult = await _dateConverter.EnglishToNepaliAsync(raw.AccountDeletedDate.Value);
                            deletedDateBs = deletedDateBsResult.ToString();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error converting AccountDeletedDate to BS for AccountNo {AccountNo}", raw.AccountNo);
                            deletedDateBs = raw.AccountDeletedDate.Value.ToString("yyyy-MM-dd");
                        }
                    }

                    resultList.Add(new SavingAccountDeletedRowDto
                    {
                        MemberId = raw.MemberId,
                        MemberName = raw.Name,
                        AccountNo = raw.AccountNo,
                        AccountOpenOnBs = raw.AccountOpenOnBs,
                        AccountOpenDate = null,
                        DepositTypeName = raw.DepositTypeName,
                        InterestRate = null,
                        AccountBalance = null,
                        DeletedDate = deletedDateBs,
                        DeletedBy = null,
                        Reason = null
                    });
                }

                // Root-cause fix: resolve the actual branch names from BranchIds instead
                // of echoing back request.BranchName ("All Branches" default).
                var branchNames = await GetBranchNamesByIdsAsync(request.BranchIds);

                return new SavingAccountDeletedData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalBalance = resultList.Sum(r => r.AccountBalance ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchNames = branchNames,
                    OrderBy = request.OrderBy,
                    TotalDeletedAccounts = resultList.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync for Saving Account Deleted Report");
                throw;
            }
        }

        /// <summary>
        /// Resolves a comma-separated list of UsmOfficeId values into a comma-separated
        /// list of office/branch names. "-1" or empty means unfiltered -> "All".
        /// Falls back to the raw id list if the lookup fails or an id has no match.
        /// </summary>
        private async Task<string> GetBranchNamesByIdsAsync(string? branchIdsCsv)
        {
            if (string.IsNullOrWhiteSpace(branchIdsCsv) || branchIdsCsv == "-1")
            {
                return "All";
            }

            try
            {
                var ids = branchIdsCsv
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(id => long.TryParse(id, out var parsed) ? parsed : (long?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .ToList();

                if (!ids.Any())
                {
                    return "All";
                }

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                const string sql = "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN @Ids";
                var names = (await connection.QueryAsync<string>(sql, new { Ids = ids })).ToList();

                return names.Any() ? string.Join(", ", names) : branchIdsCsv;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetBranchNamesByIdsAsync");
                return branchIdsCsv;
            }
        }

        private static string BuildOrderByClause(string orderBy)
        {
            return orderBy switch
            {
                "Member Name" => " ORDER BY d.DepositTypeName, Name",
                "Account No" => " ORDER BY d.DepositTypeName, substring(a.AccountNo, 1,(len(a.AccountNo)-charindex('-', a.AccountNo))-1), a.AccountNo",
                "A‎/‎C Open Date" => " ORDER BY d.DepositTypeName, a.AccountOpenOnBs",
                "Member Id" => " ORDER BY d.DepositTypeName, substring(m.MemberId, 1,(len(m.MemberId)-charindex('-', m.MemberId))-1), m.MemberId",
                _ => " ORDER BY d.DepositTypeName, substring(m.MemberId, 1,(len(m.MemberId)-charindex('-', m.MemberId))-1), m.MemberId"
            };
        }

        private class SpRawRow
        {
            public string? DepositTypeName { get; set; }
            public string? AccountNo { get; set; }
            public string? AccountOpenOnBs { get; set; }
            public DateTime? AccountDeletedDate { get; set; }
            public string? MemberId { get; set; }
            public string? Name { get; set; }
        }
    }
}