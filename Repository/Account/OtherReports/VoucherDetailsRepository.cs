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

        // Legacy webform sentinel for "no voucher selected" is -1; some JSON clients
        // (Swagger's default nullable-numeric autofill, some frontends) send 0 instead
        // of omitting the field / sending null. Treat both as "no filter" so date-only
        // queries aren't silently narrowed to AcoVoucherId = 0 (which never exists).
        private static bool IsVoucherIdSpecified(long? voucherId) =>
            voucherId.HasValue && voucherId.Value > 0;

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
                    // Upper bound made exclusive-of-next-day so same-day transactions
                    // with a time component aren't excluded by a bare date BETWEEN.
                    filter += $" AND v.VoucherOn >= '{fromDateAd}' AND v.VoucherOn < DATEADD(day, 1, '{toDateAd}')";
                }
            }

            if (!string.IsNullOrEmpty(request.BranchIds) &&
                request.BranchIds != "-1" &&
                request.BranchIds != "string")
            {
                filter += $" AND v.UsmOfficeId IN ({request.BranchIds})";
            }

            if (IsVoucherIdSpecified(request.VoucherId))
            {
                filter += $" AND v.AcoVoucherId = {request.VoucherId!.Value}";
            }

            return filter;
        }

        private string BuildSqlOrderBy(VoucherDetailsRequestDto request)
        {
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
            parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);
            parameters.Add("@SqlFilterExpOrderBy", sqlOrderBy, DbType.String, size: -1);

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

            // Get voucher number only if a real voucher was actually filtered on
            if (IsVoucherIdSpecified(request.VoucherId))
            {
                var voucherNo = await connection.QueryFirstOrDefaultAsync<string>(
                    "SELECT VoucherNo FROM AcoVoucher WHERE AcoVoucherId = @VoucherId",
                    new { VoucherId = request.VoucherId!.Value });
                data.VoucherNo = voucherNo;
            }

            return data;
        }
    }
}