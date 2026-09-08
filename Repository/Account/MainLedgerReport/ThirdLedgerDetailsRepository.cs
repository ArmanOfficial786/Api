// Repository/Account/ThirdLedgerDetailsReport/ThirdLedgerDetailsRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Account.ThirdLedgerDetailsReport;
using NexgenCosysReport.Inteface.ServiceInterface.Account.ThirdLedgerDetailsReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;

namespace NexgenCosysReport.Repository.Account.ThirdLedgerDetailsReport
{
    public class ThirdLedgerDetailsRepository : IThirdLedgerDetailsRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<ThirdLedgerDetailsRepository> _logger;

        public ThirdLedgerDetailsRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<ThirdLedgerDetailsRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        private async Task<string> BuildSqlFilterExp(ThirdLedgerDetailsRequestDto request)
        {
            var filter = string.Empty;

            if (!string.IsNullOrEmpty(request.FromDate) && !string.IsNullOrEmpty(request.ToDate)
                && request.FromDate != "-1" && request.ToDate != "-1")
            {
                string fromDateAd = await _dateConverter.BsToAdStringAsync(request.FromDate);
                string toDateAd = await _dateConverter.BsToAdStringAsync(request.ToDate);

                if (!string.IsNullOrEmpty(fromDateAd) && !string.IsNullOrEmpty(toDateAd))
                {
                    filter += $" AND v.VoucherOn BETWEEN '{fromDateAd}' AND '{toDateAd}'";
                }
            }

            if (!string.IsNullOrEmpty(request.BranchIds) &&
                request.BranchIds != "-1" &&
                request.BranchIds != "string")
            {
                filter += $" AND v.UsmOfficeId IN ({request.BranchIds})";
            }

            // Voucher type filter
            if (!string.IsNullOrEmpty(request.VoucherType) && request.VoucherType != "All")
            {
                bool isAuto = request.VoucherType == "Auto";
                filter += $" AND vp.IsAutomatic = {(isAuto ? "1" : "0")}";
            }

            // Ledger head filter
            if (request.LedgerHeadId > 0)
            {
                filter += $" AND l.AcoAccountTypeId = {request.LedgerHeadId}";
            }

            // Ledger name filter
            if (!string.IsNullOrEmpty(request.LedgerName) && request.LedgerName != "-1")
            {
                filter += $" AND l.LedgerHead = '{request.LedgerName}'";
            }

            // Sub ledger filter
            if (!string.IsNullOrEmpty(request.SubLedgerName) && request.SubLedgerName != "-1")
            {
                filter += $" AND vpd.LedgerHead = '{request.SubLedgerName}'";
            }

            // 2nd Sub ledger filter
            if (!string.IsNullOrEmpty(request.SecondSubLedgerName) && request.SecondSubLedgerName != "-1")
            {
                filter += $" AND vpd.SubLedger2 = '{request.SecondSubLedgerName}'";
            }

            // 3rd Sub ledger filter
            if (!string.IsNullOrEmpty(request.ThirdSubLedgerName) && request.ThirdSubLedgerName != "-1")
            {
                filter += $" AND vpd.SubLedger3 = '{request.ThirdSubLedgerName}'";
            }

            return filter;
        }

        private string BuildSqlOrderBy(ThirdLedgerDetailsRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
            {
                return " ORDER BY v.VoucherOn";
            }

            return request.OrderBy.ToLower() switch
            {
                "voucher no" => " ORDER BY v.VoucherNo",
                "voucher date" => " ORDER BY v.VoucherOn",
                "main ledger" => " ORDER BY MainLedger",
                "sub ledger" => " ORDER BY SubLedger1",
                "debit amount" => " ORDER BY DebitAmount DESC",
                "credit amount" => " ORDER BY CreditAmount DESC",
                "balance" => " ORDER BY Balance DESC",
                _ => " ORDER BY v.VoucherOn"
            };
        }

        public async Task<ThirdLedgerDetailsData> GetThirdLedgerDetailsDataAsync(ThirdLedgerDetailsRequestDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.FromDate) || string.IsNullOrEmpty(request.ToDate)
                    || request.FromDate == "-1" || request.ToDate == "-1")
                {
                    throw new ArgumentException("FromDate and ToDate are required.");
                }

                var sqlFilterExp = await BuildSqlFilterExp(request);
                var sqlOrderBy = BuildSqlOrderBy(request);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp);
                parameters.Add("@SqlFilterExpOrderBy", sqlOrderBy);

                // Get account type name
                string? accountTypeName = null;
                if (request.LedgerHeadId > 0)
                {
                    accountTypeName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT AccountType FROM AcoAccountType WHERE AcoAccountTypeId = @LedgerHeadId",
                        new { LedgerHeadId = request.LedgerHeadId });
                }

                // Determine which stored procedure to call based on report type
                var isSummary = request.ReportType == "Summary";
                var spName = isSummary ? "sp_6_56_GetLedgerDetailsSummary" : "sp_6_56_GetLedgerDetails";

                var result = await connection.QueryAsync<ThirdLedgerDetailsRowDto>(
                    spName,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var rows = result.ToList();

                // Calculate totals
                var totalDebit = rows.Sum(r => r.DebitAmount ?? 0);
                var totalCredit = rows.Sum(r => r.CreditAmount ?? 0);
                var totalBalance = isSummary ? totalDebit - totalCredit : totalCredit - totalDebit;

                var data = new ThirdLedgerDetailsData
                {
                    Rows = rows,
                    TotalRecords = rows.Count,
                    TotalDebitAmount = totalDebit,
                    TotalCreditAmount = totalCredit,
                    TotalBalance = totalBalance,
                    FromDateBs = request.FromDate,
                    ToDateBs = request.ToDate,
                    LedgerName = request.LedgerName,
                    SubLedgerName = request.SubLedgerName,
                    SecondSubLedgerName = request.SecondSubLedgerName,
                    ThirdSubLedgerName = request.ThirdSubLedgerName,
                    VoucherType = request.VoucherType,
                    ReportType = request.ReportType,
                    ShowOpeningBalance = request.ShowOpeningBalance,
                    OrderBy = request.OrderBy,
                    LedgerHeadName = accountTypeName
                };

                // Get branch names
                if (!string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
                {
                    var branchNames = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT STRING_AGG(OfficeName, ', ') FROM UsmOffice WHERE UsmOfficeId IN (" + request.BranchIds + ")");
                    data.BranchNames = branchNames ?? "All Branches";
                }
                else
                {
                    data.BranchNames = "All Branches";
                }

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetThirdLedgerDetailsDataAsync");
                throw;
            }
        }
    }
}