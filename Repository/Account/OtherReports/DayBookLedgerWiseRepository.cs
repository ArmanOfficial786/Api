// Repository/AccountOperation/OthersReport/DayBookLedgerWiseRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports;
using NexgenCosysReport.Inteface.ServiceInterface.Account.OtherReports;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;

namespace NexgenCosysReport.Repository.AccountOperation.OthersReport
{
    public class DayBookLedgerWiseRepository : IDayBookLedgerWiseRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;

        public DayBookLedgerWiseRepository(AppDbContext context, IDateConverterService dateConverter)
        {
            _context = context;
            _dateConverter = dateConverter;
        }

        private async Task<string> BuildSqlFilterExp(DayBookLedgerWiseRequestDto request)
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

        private string BuildSqlOrderBy(DayBookLedgerWiseRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) ||
                request.OrderBy == "-1" ||
                request.OrderBy == "string")
            {
                return " ORDER BY SubLedger1";
            }

            return request.OrderBy.Trim().ToLower() switch
            {
                "main ledger" => " ORDER BY MainLedger",
                "sub ledger" => " ORDER BY SubLedger1",
                "sub ledger1" => " ORDER BY SubLedger2",
                "sub ledger2" => " ORDER BY SubLedger3",
                "opening balance" => " ORDER BY OpeningBalance DESC",
                "balance" => " ORDER BY TodayBalance DESC",
                "closing balance" => " ORDER BY ClosingBalance DESC",
                _ => " ORDER BY SubLedger1"
            };
        }

        public async Task<DayBookLedgerWiseData> GetDayBookLedgerWiseDataAsync(DayBookLedgerWiseRequestDto request)
        {
            var sqlFilterExp = await BuildSqlFilterExp(request);
            var sqlOrderBy = BuildSqlOrderBy(request);

            var connectionString = _context.Database.GetConnectionString();
            await using var connection = new SqlConnection(connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExp", sqlFilterExp);
            parameters.Add("@SqlFilterExpOrderBy", sqlOrderBy);

            var result = await connection.QueryAsync<DayBookLedgerWiseRowDto>(
                "sp_6_56_GetDayBookLedgerWise",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120
            );

            var rows = result.ToList();

            var data = new DayBookLedgerWiseData
            {
                Rows = rows,
                TotalRecords = rows.Count,
                TotalDebitAmount = rows.Sum(r => r.DebitAmount ?? 0m),
                TotalCreditAmount = rows.Sum(r => r.CreditAmount ?? 0m),
                TotalBalance = rows.Sum(r => r.TodayBalance ?? 0m),
                FromDateBs = request.FromDate,
                ToDateBs = request.ToDate,
                OrderBy = request.OrderBy
            };

            // Get branch names if applicable
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
    }
}