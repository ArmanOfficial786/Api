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
            if (string.IsNullOrEmpty(request.OrderBy) ||
                request.OrderBy == "-1" ||
                request.OrderBy == "string")
            {
                return " ORDER BY MainLedger";
            }

            return request.OrderBy.Trim().ToLower() switch
            {
                "voucher no" => " ORDER BY VoucherNo",
                "voucher date" => " ORDER BY VoucherOnBs",
                "main ledger" => " ORDER BY MainLedger",
                "sub ledger" => " ORDER BY SubLedger1",
                "debit amount" => " ORDER BY DebitAmount DESC",
                "credit amount" => " ORDER BY CreditAmount DESC",
                _ => " ORDER BY MainLedger"
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