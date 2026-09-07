// Repository/Account/OthersReport/DailyExpenseRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports;
using NexgenCosysReport.Inteface.ServiceInterface.Account.OtherReports;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;

namespace NexgenCosysReport.Repository.Account.OthersReport
{
    public class DailyExpenseRepository : IDailyExpenseRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;

        public DailyExpenseRepository(AppDbContext context, IDateConverterService dateConverter)
        {
            _context = context;
            _dateConverter = dateConverter;
        }

        private async Task<string> BuildSqlFilterExp(DailyExpenseRequestDto request)
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

            return filter;
        }

        private string BuildSqlOrderBy(DailyExpenseRequestDto request)
        {
            // LedgerHead then MainLedger always lead the sort (matches the webform's
            // SortGroupHeader3 — three grouping levels: LedgerHead -> MainLedger ->
            // SubLedger) so rows for the same group arrive contiguous. The view's
            // GroupBy preserves first-seen order, it does not sort, so this ordering
            // is required for correct nested grouping.
            if (string.IsNullOrEmpty(request.OrderBy) ||
                request.OrderBy == "-1" ||
                request.OrderBy == "string")
            {
                return " ORDER BY LedgerHead, MainLedger, SubLedger";
            }

            return request.OrderBy.Trim().ToLower() switch
            {
                "main ledger" => " ORDER BY LedgerHead, MainLedger",
                "sub ledger" => " ORDER BY LedgerHead, MainLedger, SubLedger",
                "debit amount" => " ORDER BY LedgerHead, MainLedger, DebitAmount DESC",
                "credit amount" => " ORDER BY LedgerHead, MainLedger, CreditAmount DESC",
                "balance" => " ORDER BY LedgerHead, MainLedger, Balance DESC",
                _ => " ORDER BY LedgerHead, MainLedger, SubLedger"
            };
        }

        public async Task<DailyExpenseData> GetDailyExpenseDataAsync(DailyExpenseRequestDto request)
        {
            var sqlFilterExp = await BuildSqlFilterExp(request);
            var sqlOrderBy = BuildSqlOrderBy(request);

            var connectionString = _context.Database.GetConnectionString();
            await using var connection = new SqlConnection(connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExp", sqlFilterExp);
            parameters.Add("@SqlFilterExpOrderBy", sqlOrderBy);

            var result = await connection.QueryAsync<DailyExpenseRowDto>(
                "sp_6_56_GetDailyExpense",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120
            );

            var rows = result.ToList();

            var data = new DailyExpenseData
            {
                Rows = rows,
                TotalRecords = rows.Count,
                TotalDebitAmount = rows.Sum(r => r.DebitAmount ?? 0m),
                TotalCreditAmount = rows.Sum(r => r.CreditAmount ?? 0m),
                TotalBalance = rows.Sum(r => r.Balance ?? 0m),
                FromDateBs = request.FromDate,
                ToDateBs = request.ToDate,
                OrderBy = request.OrderBy
            };

            // STRING_AGG needs compat level 140 (SQL Server 2017+), unavailable on this
            // database — same fix as AccountYearClosingRepository: split the CSV in C#
            // and let Dapper parameterize the IN clause.
            if (!string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
            {
                var branchIdList = request.BranchIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(id => long.TryParse(id, out var parsed) ? parsed : (long?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .ToList();

                if (branchIdList.Any())
                {
                    const string sql = @"
                        SELECT OfficeName
                        FROM UsmOffice
                        WHERE UsmOfficeId IN @Ids
                        ORDER BY OfficeName";

                    var names = (await connection.QueryAsync<string>(
                        sql, new { Ids = branchIdList })).ToList();

                    data.BranchNames = names.Any() ? string.Join(", ", names) : "All";
                }
                else
                {
                    data.BranchNames = "All";
                }
            }
            else
            {
                data.BranchNames = "All";
            }

            return data;
        }
    }
}