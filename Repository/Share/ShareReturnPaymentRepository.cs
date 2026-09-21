using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Share;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Share;
using System.Data;

namespace NexgenCosysReport.Repository.Share
{
    public class ShareReturnPaymentRepository : IShareReturnPayment
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<ShareReturnPaymentRepository> _logger;

        public ShareReturnPaymentRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<ShareReturnPaymentRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        private static string SanitizeOfficeIds(string? officeIds)
        {
            if (string.IsNullOrWhiteSpace(officeIds) || officeIds == "-1" || officeIds == "string")
                return "-1";

            var validIds = officeIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => long.TryParse(id, out _));

            var joined = string.Join(",", validIds);
            return string.IsNullOrEmpty(joined) ? "-1" : joined;
        }

        public async Task<ShareReturnPaymentData> GetReportDataAsync(ShareReturnPaymentRequestDto request)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                var officeIds = SanitizeOfficeIds(request.OfficeIds);

                string branchName = "All";
                if (officeIds != "-1")
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({officeIds})");
                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
                }

                var parameters = new DynamicParameters();
                parameters.Add("@FromDate", fromDateAd.ToString("yyyy-MM-dd"), DbType.String, size: 225);
                parameters.Add("@ToDate", toDateAd.ToString("yyyy-MM-dd"), DbType.String, size: 225);
                parameters.Add("@UsmOfficeId", officeIds, DbType.String, size: 225);

                var rows = await connection.QueryAsync<ShareReturnPaymentRowDto>(
                    "sp_8_14_GetShareReturnPaymentReport",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var resultList = rows.AsList();

                return new ShareReturnPaymentData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalMembers = resultList
                        .Select(r => r.MemberId)
                        .Distinct()
                        .Count(),
                    TotalCashAmount = resultList.Sum(r => r.CashAmount ?? 0),
                    TotalTaxAmount = resultList.Sum(r => r.TaxAmount ?? 0),
                    TotalBonusShareAmount = resultList.Sum(r => r.BonusShareAmount ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchName = branchName
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