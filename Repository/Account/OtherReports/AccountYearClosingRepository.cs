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

            if (!string.IsNullOrEmpty(request.BranchIds) &&
                request.BranchIds != "-1" &&
                request.BranchIds != "string")
            {
                filter += $" AND a.UsmOfficeId IN ({request.BranchIds})";
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

            // Root-cause fix: neither STRING_AGG (needs compat level 140) nor STRING_SPLIT
            // (needs compat level 130) is available on this database. Split the CSV in C#
            // instead of SQL, and let Dapper parameterize the resulting list into an IN
            // clause — works on any SQL Server version with zero dependency on
            // compatibility level.
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