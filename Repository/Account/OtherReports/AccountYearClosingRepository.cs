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

            // Build ORDER BY clause
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
                    "account year" => " ORDER BY AccountYear",
                    "closed date" => " ORDER BY ClosedOnBs",
                    "voucher no" => " ORDER BY VoucherNo",
                    "status" => " ORDER BY Status",
                    "closed by" => " ORDER BY ClosedBy",
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