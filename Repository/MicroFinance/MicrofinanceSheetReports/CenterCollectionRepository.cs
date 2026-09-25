// Repository/Microfinance/MicrofinanceSheetReports/CenterCollectionRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Microfinance.MicrofinanceSheetReports;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Microfinance.MicrofinanceSheetReports;
using System.Data;

namespace NexgenCosysReport.Repository.Microfinance.MicrofinanceSheetReports
{
    public class CenterCollectionRepository : ICenterCollectionRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<CenterCollectionRepository> _logger;

        public CenterCollectionRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<CenterCollectionRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        private static string SanitizeId(string? id)
        {
            if (string.IsNullOrWhiteSpace(id) || id == "-1" || id == "string")
                return "0";

            return long.TryParse(id, out var v) ? v.ToString() : "0";
        }

        public async Task<CenterCollectionData> GetReportDataAsync(CenterCollectionRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.TillDateBs) || request.TillDateBs == "-1")
                    throw new ArgumentException("Collection Date is required.");

                var collectionCenterId = SanitizeId(request.CollectionCenterId);
                if (collectionCenterId == "0")
                    throw new ArgumentException("Please select a Collection Center.");

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDateBs);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlTillDate", tillDateAd.ToString("MM-dd-yyyy"), DbType.String, size: -1);
                parameters.Add("@SqlCollectionCenterId", collectionCenterId, DbType.String, size: -1);

                using var multi = await connection.QueryMultipleAsync(
                    "sp_5_43_GetCenterCollectionReport",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 600);

                var rows = (await multi.ReadAsync<CenterCollectionRowDto>()).AsList();
                var savingSummary = (await multi.ReadAsync<CenterCollectionSavingSummaryDto>()).AsList();
                var loanSummary = (await multi.ReadAsync<CenterCollectionLoanSummaryDto>()).AsList();

                // Header info
                string? collectionCenterName = null;
                string? collectionCenterAddress = null;
                string? previousMeetingDate = null;

                var centerInfo = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    @"SELECT CollectionCenterName, Address 
                      FROM SycCollectionCenter 
                      WHERE SycCollectionCenterId = @Id",
                    new { Id = long.Parse(collectionCenterId) });

                if (centerInfo != null)
                {
                    collectionCenterName = (string?)centerInfo.CollectionCenterName;
                    collectionCenterAddress = (string?)centerInfo.Address;
                }

                previousMeetingDate = await connection.QueryFirstOrDefaultAsync<string>(
                    @"SELECT TOP 1 
                             ISNULL(ChangeMeetingDateOnBs, MeetingDateOnBs) 
                      FROM   SycCollectionCenterMeetingDate 
                      WHERE  SycCollectionCenterId = @Id 
                      AND    IsActive = 1 
                      AND    (MeetingDateOn < GETDATE() OR ChangeMeetingDateOn < GETDATE())
                      ORDER BY ISNULL(ChangeMeetingDateOn, MeetingDateOn) DESC",
                    new { Id = long.Parse(collectionCenterId) });

                return new CenterCollectionData
                {
                    Rows = rows,
                    SavingSummary = savingSummary,
                    LoanSummary = loanSummary,

                    TotalRecords = rows.Count,
                    TotalLedgerBalance = rows.Sum(r => r.LedgerBalance ?? 0),
                    TotalNetBalance = rows.Sum(r => r.NetBalance ?? 0),
                    TotalSavingDue = rows.Sum(r => r.DueAmount ?? 0),
                    TotalLoanIssueAmount = rows.Sum(r => r.LoanIssueAmount ?? 0),
                    TotalRemainingPrinciple = rows.Sum(r => r.RemainingPrinciple ?? 0),
                    TotalDuePrinciple = rows.Sum(r => r.DuePrinciple ?? 0),
                    TotalDueInterest = rows.Sum(r => r.DueInterest ?? 0),
                    TotalDuePenalty = rows.Sum(r => r.DuePenalty ?? 0),
                    TotalInstallment = rows.Sum(r => r.TotalInstallment ?? 0),
                    TotalCollection = rows.Sum(r => (r.DueAmount ?? 0) + (r.TotalInstallment ?? 0)),

                    TillDateBs = request.TillDateBs,
                    TillDateAd = tillDateAd.ToString("yyyy-MM-dd"),
                    CollectionCenterName = collectionCenterName,
                    CollectionCenterAddress = collectionCenterAddress,
                    PreviousMeetingDate = previousMeetingDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync (CenterCollection)");
                throw;
            }
        }
    }
}