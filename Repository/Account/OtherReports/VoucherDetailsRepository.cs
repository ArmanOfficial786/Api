// Repository/Account/OthersReport/VoucherDetailsRepository.cs
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
    public class VoucherDetailsRepository : IVoucherDetailsRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;

        public VoucherDetailsRepository(AppDbContext context, IDateConverterService dateConverter)
        {
            _context = context;
            _dateConverter = dateConverter;
        }

        private async Task<string> BuildSqlFilterExp(VoucherDetailsRequestDto request)
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

            if (request.VoucherId.HasValue && request.VoucherId.Value != -1)
            {
                filter += $" AND v.AcoVoucherId = {request.VoucherId.Value}";
            }

            return filter;
        }

        private string BuildSqlOrderBy(VoucherDetailsRequestDto request)
        {
            // VoucherNo always leads so rows for the same voucher arrive contiguous —
            // required for the view's GroupBy (which preserves first-seen order, does
            // not sort) to group correctly into the per-voucher blocks shown in the image.
            // The previous default ("ORDER BY MainLedger" with no VoucherNo at all) would
            // scatter a single voucher's debit/credit legs across the whole report.
            if (string.IsNullOrEmpty(request.OrderBy) ||
                request.OrderBy == "-1" ||
                request.OrderBy == "string")
            {
                return " ORDER BY VoucherNo, MainLedger";
            }

            return request.OrderBy.Trim().ToLower() switch
            {
                "voucher no" => " ORDER BY VoucherNo",
                "voucher date" => " ORDER BY VoucherNo, VoucherOnBs",
                "main ledger" => " ORDER BY VoucherNo, MainLedger",
                "sub ledger" => " ORDER BY VoucherNo, SubLedger1",
                "debit amount" => " ORDER BY VoucherNo, DebitAmount DESC",
                "credit amount" => " ORDER BY VoucherNo, CreditAmount DESC",
                _ => " ORDER BY VoucherNo, MainLedger"
            };
        }

        public async Task<VoucherDetailsData> GetVoucherDetailsDataAsync(VoucherDetailsRequestDto request)
        {
            var sqlFilterExp = await BuildSqlFilterExp(request);
            var sqlOrderBy = BuildSqlOrderBy(request);

            var connectionString = _context.Database.GetConnectionString();
            await using var connection = new SqlConnection(connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExp", sqlFilterExp);
            parameters.Add("@SqlFilterExpOrderBy", sqlOrderBy);

            var result = await connection.QueryAsync<VoucherDetailsRowDto>(
                "sp_6_56_GetVoucherDetails",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120
            );

            var rows = result.ToList();

            var data = new VoucherDetailsData
            {
                Rows = rows,
                TotalRecords = rows.Count,
                TotalDebitAmount = rows.Sum(r => r.DebitAmount ?? 0m),
                TotalCreditAmount = rows.Sum(r => r.CreditAmount ?? 0m),
                FromDateBs = request.FromDate,
                ToDateBs = request.ToDate,
                OrderBy = request.OrderBy,
                ViewType = request.ViewType,
                VoucherId = request.VoucherId
            };

            // Get voucher number if voucher ID is provided
            if (request.VoucherId.HasValue && request.VoucherId.Value != -1)
            {
                var voucherNo = await connection.QueryFirstOrDefaultAsync<string>(
                    "SELECT VoucherNo FROM AcoVoucher WHERE AcoVoucherId = @VoucherId",
                    new { VoucherId = request.VoucherId.Value });
                data.VoucherNo = voucherNo;
            }

            // Root-cause fix: STRING_AGG needs compat level 140 (SQL Server 2017+), which
            // this database doesn't have (same fix as DailyExpenseRepository, DailyIncomeRepository,
            // DayBookLedgerWiseRepository, PEARLSAnalysisRepository). Split the CSV in C# and let
            // Dapper parameterize the IN clause instead — works on any SQL Server version.
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

                    data.BranchNames = names.Any() ? string.Join(", ", names) : "All Branches";
                }
                else
                {
                    data.BranchNames = "All Branches";
                }
            }
            else
            {
                data.BranchNames = "All Branches";
            }

            return data;
        }
    }
}