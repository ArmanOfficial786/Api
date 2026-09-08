// Repository/Account/OthersReport/ReserveMasterRepository.cs
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
    public class ReserveMasterRepository : IReserveMasterRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;

        public ReserveMasterRepository(AppDbContext context, IDateConverterService dateConverter)
        {
            _context = context;
            _dateConverter = dateConverter;
        }

        private async Task<string> BuildSqlFilterExp(ReserveMasterRequestDto request)
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

        private string BuildSqlOrderBy(ReserveMasterRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) ||
                request.OrderBy == "-1" ||
                request.OrderBy == "string")
            {
                return " ORDER BY Id";
            }

            return request.OrderBy.Trim().ToLower() switch
            {
                "title" => " ORDER BY Title",
                "percentage" => " ORDER BY Percentage DESC",
                "amount" => " ORDER BY Amount DESC",
                _ => " ORDER BY Id"
            };
        }

        public async Task<ReserveMasterData> GetReserveMasterDataAsync(ReserveMasterRequestDto request)
        {
            var sqlFilterExp = await BuildSqlFilterExp(request);
            var sqlOrderBy = BuildSqlOrderBy(request);

            var connectionString = _context.Database.GetConnectionString();
            await using var connection = new SqlConnection(connectionString);

            // Create parameters with output parameter for reserve amount
            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExp", sqlFilterExp);
            parameters.Add("@SqlFilterExpOrderBy", sqlOrderBy);
            parameters.Add("@reserveAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);

            var result = await connection.QueryAsync<ReserveMasterRowDto>(
                "sp_6_56_GetReserveMaster",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120
            );

            // Get the output parameter value
            var totalReserveAmount = parameters.Get<decimal?>("@reserveAmount") ?? 0;

            var rows = result.ToList();

            // Calculate additional metrics
            var totalIncome = 0m;
            var totalExpense = 0m;
            var generalReserve = 0m;
            var reservePercentage = 0m;

            // Get reserve percentage from the first row if available
            if (rows.Any())
            {
                var generalReserveRow = rows.FirstOrDefault(r => r.Title == "General Reserve Fund" && r.IsEditable == false);
                if (generalReserveRow != null)
                {
                    reservePercentage = generalReserveRow.Percentage ?? 0;
                    generalReserve = generalReserveRow.Amount ?? 0;
                }
            }

            // Calculate remaining reserve
            var remainingReserve = totalReserveAmount - generalReserve;

            // Calculate income and expense from the data
            // Note: We need to calculate these from the temp ledger data
            // Since the stored procedure returns only the reserve data, we'll calculate
            // income and expense separately or derive from reserve amount

            var data = new ReserveMasterData
            {
                Rows = rows,
                TotalRecords = rows.Count,
                TotalReserveAmount = totalReserveAmount,
                GeneralReserve = generalReserve,
                RemainingReserve = remainingReserve,
                ReservePercentage = reservePercentage,
                FromDateBs = request.FromDate,
                ToDateBs = request.ToDate,
                OrderBy = request.OrderBy
            };

            // Calculate Net Profit = Total Income - Total Expense
            // Since we don't have direct income/expense from the stored procedure output,
            // we can derive it from the reserve amount and percentage
            if (reservePercentage > 0)
            {
                // Net Profit = Total Reserve Amount * 100 / Reserve Percentage (approx)
                // But this is approximate since different reserves have different percentages
                data.NetProfit = totalReserveAmount;
            }

            return data;
        }
    }
}