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
    public class TellerToTellerCashTransferRepository : ITellerToTellerCashTransfer
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;

        public TellerToTellerCashTransferRepository(AppDbContext context, IDateConverterService dateConverter)
        {
            _context = context;
            _dateConverter = dateConverter;
        }

        // --------------------------------------------------------------
        // @SqlFilterExp
        // Appended inside SP WHERE t.IsActive=1 + @SqlFilterExp
        // Uses t.TransectionOn (NOT t.TransectionOnBs) and t.UsmOfficeId
        // Also carries the ORDER BY clause — legacy SP concatenates both
        // into the same @SqlFilterExp parameter (single-param SP, unlike
        // AccountStatement's split Exp/OrderBy/Type params).
        // --------------------------------------------------------------
        private async Task<string> BuildSqlFilterExp(TellerToTellerCashTransferRequestDto request)
        {
            var filter = string.Empty;

            if (!string.IsNullOrEmpty(request.FromDate) && !string.IsNullOrEmpty(request.ToDate)
                && request.FromDate != "-1" && request.ToDate != "-1")
            {
                string fromDateAd = await _dateConverter.BsToAdStringAsync(request.FromDate);
                string toDateAd = await _dateConverter.BsToAdStringAsync(request.ToDate);

                if (!string.IsNullOrEmpty(fromDateAd) && !string.IsNullOrEmpty(toDateAd))
                {
                    filter += $" AND t.TransectionOn BETWEEN '{fromDateAd}' AND '{toDateAd}'";
                }
            }

            if (!string.IsNullOrEmpty(request.BranchIds) &&
                request.BranchIds != "-1" &&
                request.BranchIds != "string")
            {
                filter += $" AND t.UsmOfficeId IN ({request.BranchIds})";
            }

            filter += BuildSqlOrderBy(request);

            return filter;
        }

        // --------------------------------------------------------------
        // ORDER BY clause — column names must match the SP final SELECT
        // aliases (TellerFrom, TellerTo, Date, Amount, IssuedBy)
        // --------------------------------------------------------------
        private string BuildSqlOrderBy(TellerToTellerCashTransferRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) ||
                request.OrderBy == "-1" ||
                request.OrderBy == "string")
            {
                return " order by Date"; // default — matches legacy BLL
            }

            return request.OrderBy.Trim().ToLower() switch
            {
                "tellerfrom" => " order by TellerFrom",
                "tellerto" => " order by TellerTo",
                "date" => " order by Date",
                "amount" => " order by Amount",
                "issued by" => " order by IssuedBy",
                _ => " order by Date"
            };
        }

        // -- Main teller-to-teller cash transfer report ----------------
        // Keeps a compatibility method that returns raw rows and
        // implements the interface method expected by services.
        public async Task<List<TellerToTellerCashTransferRowDto>>
            GetTellerToTellerCashTransferAsync(TellerToTellerCashTransferRequestDto request)
        {
            var sqlFilterExp = await BuildSqlFilterExp(request);

            var connectionString = _context.Database.GetConnectionString();
            await using var connection = new SqlConnection(connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExp", sqlFilterExp);

            var result = await connection.QueryAsync<TellerToTellerCashTransferRowDto>(
                "sp_6_56_GetTellerToTellerCashTransfer",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120
            );

            return result.ToList();
        }

        // Interface implementation expected by application layer
        public async Task<TellerToTellerCashTransferData>
            GetReportDataAsync(TellerToTellerCashTransferRequestDto request)
        {
            var rows = await GetTellerToTellerCashTransferAsync(request);

            var data = new TellerToTellerCashTransferData
            {
                Rows = rows,
                TotalRecords = rows?.Count ?? 0,
                TotalAmount = rows?.Sum(r => r.Amount ?? 0m) ?? 0m,
                FromDateBs = request.FromDate,
                ToDateBs = request.ToDate,
                OrderBy = request.OrderBy
            };

            return data;
        }
    }
}