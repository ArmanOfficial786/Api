using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Remit;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Remit;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.Remit
{
    public class RemitReceiptPrintRepository : IRemitReceiptPrintRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<RemitReceiptPrintRepository> _logger;

        public RemitReceiptPrintRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<RemitReceiptPrintRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        private static string SanitizeBranchIds(string? branchIds)
        {
            if (string.IsNullOrWhiteSpace(branchIds) || branchIds == "-1" || branchIds == "string")
                return string.Empty;

            var validIds = branchIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => long.TryParse(id, out _));

            return string.Join(",", validIds);
        }

        private static string ConvertToWords(decimal amount)
        {
            var units = new[] { "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
                "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen",
                "Eighteen", "Nineteen" };
            var tens = new[] { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

            string Convert(long n)
            {
                if (n < 20) return units[n];
                if (n < 100) return tens[n / 10] + (n % 10 > 0 ? " " + units[n % 10] : "");
                if (n < 1000) return units[n / 100] + " Hundred" + (n % 100 > 0 ? " " + Convert(n % 100) : "");
                if (n < 100000) return Convert(n / 1000) + " Thousand" + (n % 1000 > 0 ? " " + Convert(n % 1000) : "");
                if (n < 10000000) return Convert(n / 100000) + " Lakh" + (n % 100000 > 0 ? " " + Convert(n % 100000) : "");
                return Convert(n / 10000000) + " Crore" + (n % 10000000 > 0 ? " " + Convert(n % 10000000) : "");
            }

            var whole = (long)Math.Floor(amount);
            var paisa = (long)Math.Round((amount - whole) * 100);

            var result = Convert(whole);
            if (paisa > 0)
                result += " and " + Convert(paisa) + " Paisa";

            return result;
        }

        public async Task<RemitReceiptPrintData> GetReportDataAsync(RemitReceiptPrintRequestDto request)
        {
            try
            {
                var branchIds = SanitizeBranchIds(request.BranchIds);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sqlFilterExp = new StringBuilder();

                if (!string.IsNullOrEmpty(request.FromDateBs) && !string.IsNullOrEmpty(request.ToDateBs))
                {
                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                    sqlFilterExp.Append(" And TransactionOn between '")
                                .Append(fromDateAd.ToString("yyyy-MM-dd"))
                                .Append("' And '")
                                .Append(toDateAd.ToString("yyyy-MM-dd"))
                                .Append("' ");
                }

                if (!string.IsNullOrEmpty(branchIds))
                {
                    sqlFilterExp.Append(" And RD.UsmOfficeId in ( ").Append(branchIds).Append(" )");
                }

                if (!string.IsNullOrEmpty(request.IsReceived))
                {
                    sqlFilterExp.Append(" And IsReceived = ").Append(request.IsReceived);
                }

                if (request.RemittanceDetailId != -1)
                {
                    sqlFilterExp.Append(" And RemRemittanceDetailsId = ").Append(request.RemittanceDetailId);
                }

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);

                var rows = await connection.QueryAsync<RemitReceiptPrintRowDto>(
                    "sp_9_67_GetRemittanceReceiptReport",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var resultList = rows.AsList();
                var transactionAmount = resultList.FirstOrDefault()?.TransactionAmount ?? 0;

                string branchName = "All";
                if (!string.IsNullOrEmpty(branchIds))
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
                }

                return new RemitReceiptPrintData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TransactionAmount = transactionAmount,
                    AmountInWords = $"NRs. {ConvertToWords(transactionAmount)} Rupees Only.",
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchName = branchName,
                    IsReceived = request.IsReceived
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync");
                throw;
            }
        }
    }
}