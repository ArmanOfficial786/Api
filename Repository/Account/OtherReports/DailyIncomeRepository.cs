// Repository/Account/OthersReport/DailyIncomeRepository.cs
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
    public class DailyIncomeRepository : IDailyIncomeRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;

        public DailyIncomeRepository(AppDbContext context, IDateConverterService dateConverter)
        {
            _context = context;
            _dateConverter = dateConverter;
        }

        private async Task<string> BuildSqlFilterExp(DailyIncomeRequestDto request)
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

            if (!string.IsNullOrEmpty(request.BranchId) &&
                request.BranchId != "-1" &&
                request.BranchId != "string")
            {
                filter += $" AND v.UsmOfficeId IN ({request.BranchId})";
            }

            return filter;
        }

        private string BuildSqlOrderBy(DailyIncomeRequestDto request)
        {
            // MainLedger always leads so rows for the same main ledger arrive contiguous
            // — required for the view's GroupBy (which preserves first-seen order, does
            // not sort) to group correctly, matching the report's visual grouping.
            if (string.IsNullOrEmpty(request.OrderBy) ||
                request.OrderBy == "-1" ||
                request.OrderBy == "string")
            {
                return " ORDER BY MainLedger, SubLedger";
            }

            return request.OrderBy.Trim().ToLower() switch
            {
                "main ledger" => " ORDER BY MainLedger",
                "sub ledger" => " ORDER BY MainLedger, SubLedger",
                "debit amount" => " ORDER BY MainLedger, DebitAmount DESC",
                "credit amount" => " ORDER BY MainLedger, CreditAmount DESC",
                "balance" => " ORDER BY MainLedger, Balance DESC",
                _ => " ORDER BY MainLedger, SubLedger"
            };
        }

        public async Task<DailyIncomeData> GetDailyIncomeDataAsync(DailyIncomeRequestDto request)
        {
            var sqlFilterExp = await BuildSqlFilterExp(request);
            var sqlOrderBy = BuildSqlOrderBy(request);

            var connectionString = _context.Database.GetConnectionString();
            await using var connection = new SqlConnection(connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExp", sqlFilterExp);
            parameters.Add("@SqlFilterExpOrderBy", sqlOrderBy);

            var result = await connection.QueryAsync<DailyIncomeRowDto>(
                "sp_6_56_GetDailyIncome",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120
            );

            var rows = result.ToList();

            var data = new DailyIncomeData
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

            return data;
        }
    }
}