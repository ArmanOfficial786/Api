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
    public class ShareDividendPatronizeTransferredRepository : IShareDividendPatronizeTransferred
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<ShareDividendPatronizeTransferredRepository> _logger;

        public ShareDividendPatronizeTransferredRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<ShareDividendPatronizeTransferredRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<ShareDividendPatronizeTransferredData> GetReportDataAsync(ShareDividendPatronizeTransferredRequestDto request)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var normalizedFromDate = (request.FromDateBs ?? string.Empty).Replace("/", "-");
                var normalizedToDate = (request.ToDateBs ?? string.Empty).Replace("/", "-");

                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(normalizedFromDate);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(normalizedToDate);

                var reportType = string.IsNullOrWhiteSpace(request.ReportType)
                    ? "SHARE DIVIDEND"
                    : request.ReportType.ToUpper();

                var spName = reportType == "PATRONIZE"
                    ? "sp_8_14_GetPatronizedTransferredReport"
                    : "sp_8_14_GetShareDividendTransferredReport";


                var parameters = new DynamicParameters();
                parameters.Add("@fromDate", fromDateAd.Date, DbType.Date);
                parameters.Add("@toDate", toDateAd.Date, DbType.Date);

                var rows = await connection.QueryAsync<ShareDividendPatronizeTransferredRowDto>(
                    spName,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var resultList = rows.AsList();

                return new ShareDividendPatronizeTransferredData
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
                    ReportType = reportType
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