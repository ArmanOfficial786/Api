// Repository/Microfinance/MicrofinanceSheetReports/CenterCollectionSheetSingleRepository.cs
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
    public class CenterCollectionSheetSingleRepository : ICenterCollectionSheetSingleRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<CenterCollectionSheetSingleRepository> _logger;

        public CenterCollectionSheetSingleRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<CenterCollectionSheetSingleRepository> logger)
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

        private static string GetViewTypeName(string viewType) =>
            viewType?.Trim().ToUpper() switch
            {
                "A" => "Show Account No",
                "T" => "Show Account Type",
                _ => "Show Account Type"
            };

        public async Task<CenterCollectionSheetSingleData> GetReportDataAsync(CenterCollectionSheetSingleRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.TillDateBs) || request.TillDateBs == "-1")
                    throw new ArgumentException("Collection Date is required.");

                var collectionCenterId = SanitizeId(request.CollectionCenterId);
                if (collectionCenterId == "0")
                    throw new ArgumentException("Please select a Collection Center.");

                var viewType = request.ViewType?.Trim().ToUpper();
                if (viewType != "A" && viewType != "T")
                    viewType = "T";

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDateBs);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlTillDate", tillDateAd.ToString("MM-dd-yyyy"), DbType.String, size: -1);
                parameters.Add("@SqlCollectionCenterId", collectionCenterId, DbType.String, size: -1);
                parameters.Add("@SqlViewType", viewType, DbType.String, size: -1);

                using var multi = await connection.QueryMultipleAsync(
                    "sp_5_43_GetCenterCollectionSheetReportSingle",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 600);

                // Result set 1: main combined sheet
                var rows = (await multi.ReadAsync<CenterCollectionSheetSingleRowDto>()).AsList();

                // Result set 2: loan summary
                var loanSummary = (await multi.ReadAsync<CenterCollectionSheetSingleLoanSummaryDto>()).AsList();

                // Result set 3: saving summary
                var savingSummary = (await multi.ReadAsync<CenterCollectionSheetSingleSavingSummaryDto>()).AsList();

                // Result set 4: evaluation (single row)
                var evaluation = await multi.ReadFirstOrDefaultAsync<CenterCollectionSheetSingleEvaluationDto>();

                // Header info
                string? collectionCenterName = null;
                string? collectionCenterAddress = null;
                string? nextMeetingDate = null;

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

                nextMeetingDate = await connection.QueryFirstOrDefaultAsync<string>(
                    @"SELECT TOP 1 
                             ISNULL(ChangeMeetingDateOnBs, MeetingDateOnBs) 
                      FROM   SycCollectionCenterMeetingDate 
                      WHERE  SycCollectionCenterId = @Id 
                      AND    IsActive = 1 
                      AND    (MeetingDateOn > GETDATE() OR ChangeMeetingDateOn > GETDATE())
                      ORDER BY ISNULL(ChangeMeetingDateOn, MeetingDateOn)",
                    new { Id = long.Parse(collectionCenterId) });

                return new CenterCollectionSheetSingleData
                {
                    Rows = rows,
                    LoanSummary = loanSummary,
                    SavingSummary = savingSummary,
                    Evaluation = evaluation,

                    TotalRecords = rows.Count,
                    TotalSLedgerBalance = rows.Sum(r => r.SLedgerBalance ?? 0),
                    TotalSNetBalance = rows.Sum(r => r.SNetBalance ?? 0),
                    TotalSDueAmount = rows.Sum(r => r.SDueAmount ?? 0),
                    TotalSGrandTotal = rows.Sum(r => r.SGrandTotal ?? 0),
                    TotalLoanIssueAmount = rows.Sum(r => r.LoanIssueAmount ?? 0),
                    TotalLRemainingPrinciple = rows.Sum(r => r.LRemainingPrinciple ?? 0),
                    TotalLDuePrinciple = rows.Sum(r => r.LDuePrinciple ?? 0),
                    TotalLDueInterest = rows.Sum(r => r.LDueInterest ?? 0),
                    TotalLDuePenalty = rows.Sum(r => r.LDuePenalty ?? 0),
                    TotalLTotalInstallment = rows.Sum(r => r.LTotalInstallment ?? 0),
                    TotalOtherAmount = rows.Sum(r => r.OtherAmount ?? 0),

                    TillDateBs = request.TillDateBs,
                    TillDateAd = tillDateAd.ToString("yyyy-MM-dd"),
                    CollectionCenterName = collectionCenterName,
                    CollectionCenterAddress = collectionCenterAddress,
                    NextMeetingDate = nextMeetingDate,
                    ViewType = viewType,
                    ViewTypeName = GetViewTypeName(viewType)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync (CenterCollectionSheetSingle)");
                throw;
            }
        }
    }
}