// Repository/Account/OthersReport/AccountYearClosingRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports;
using NexgenCosysReport.Inteface.ServiceInterface.Account.OtherReports;
using System.Data;

namespace NexgenCosysReport.Repository.Account.OthersReport
{
    public class AccountYearClosingRepository : IAccountYearClosingRepository
    {
        private readonly AppDbContext _context;

        public AccountYearClosingRepository(AppDbContext context)
        {
            _context = context;
        }

        private string BuildSqlFilterExp(AccountYearClosingRequestDto request)
        {
            var filter = string.Empty;

            if (!string.IsNullOrEmpty(request.BranchId) &&
                request.BranchId != "-1" &&
                request.BranchId != "string")
            {
                filter += $" AND a.UsmOfficeId IN ({request.BranchId})";
            }

            // Branch Name always leads the ORDER BY so rows from the same branch arrive
            // contiguous — required for the view's GroupBy (which preserves first-seen
            // order, does not sort) to group correctly, matching the report's visual grouping.
            if (string.IsNullOrEmpty(request.OrderBy) ||
                request.OrderBy == "-1" ||
                request.OrderBy == "string")
            {
                filter += " ORDER BY BranchName";
            }
            else
            {
                filter += request.OrderBy.Trim().ToLower() switch
                {
                    "branch name" => " ORDER BY BranchName",
                    "account year" => " ORDER BY BranchName, AccountYear",
                    "closed date" => " ORDER BY BranchName, ClosedOnBs",
                    "voucher no" => " ORDER BY BranchName, VoucherNo",
                    "status" => " ORDER BY BranchName, Status",
                    "closed by" => " ORDER BY BranchName, ClosedBy",
                    _ => " ORDER BY BranchName"
                };
            }

            return filter;
        }

        public async Task<AccountYearClosingData> GetAccountYearClosingDataAsync(AccountYearClosingRequestDto request)
        {
            var sqlFilterExp = BuildSqlFilterExp(request);

            var connectionString = _context.Database.GetConnectionString();
            await using var connection = new SqlConnection(connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExp", sqlFilterExp);

            var result = await connection.QueryAsync<AccountYearClosingRowDto>(
                "sp_6_56_GetAccountYearClosing",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120
            );

            var rows = result.ToList();

            var data = new AccountYearClosingData
            {
                Rows = rows,
                TotalRecords = rows.Count,
                OrderBy = request.OrderBy
            };

            return data;
        }
    }
}