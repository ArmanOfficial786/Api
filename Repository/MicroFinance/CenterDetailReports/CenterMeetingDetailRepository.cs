// Repository/Microfinance/CenterDetailReports/CenterMeetingDetailRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Microfinance.CenterDetailReports;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Microfinance.CenterDetailReports;
using System.Data;

namespace NexgenCosysReport.Repository.Microfinance
{
    public class CenterMeetingDetailRepository : ICenterMeetingDetailRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<CenterMeetingDetailRepository> _logger;

        public CenterMeetingDetailRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<CenterMeetingDetailRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        private static string GetMeetingTypeName(string meetingType) =>
            meetingType?.Trim() switch
            {
                "rbNext" => "Next Meeting",
                "rbCenterWise" => "Center Wise",
                "rbDetail" => "Detail",
                _ => "Next Meeting"
            };

        public async Task<CenterMeetingDetailData> GetReportDataAsync(CenterMeetingDetailRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.FromDateBs) || request.FromDateBs == "-1")
                    throw new ArgumentException("From Date is required.");

                if (string.IsNullOrWhiteSpace(request.ToDateBs) || request.ToDateBs == "-1")
                    throw new ArgumentException("To Date is required.");

                // SP checks "<> -1" for both office and center, so -1 IS the sentinel —
                // never send DBNull here, since NULL <> -1 evaluates to UNKNOWN and
                // silently (and confusingly) behaves as "no filter" for the wrong reason.
                var officeId = -1;
                if (!string.IsNullOrEmpty(request.BranchId) &&
                    request.BranchId != "-1" &&
                    int.TryParse(request.BranchId, out var oId))
                {
                    officeId = oId;
                }

                var centerId = -1;
                if (!string.IsNullOrEmpty(request.CollectionCenterId) &&
                    request.CollectionCenterId != "-1" &&
                    int.TryParse(request.CollectionCenterId, out var cId))
                {
                    centerId = cId;
                }

                var meetingType = request.MeetingType?.Trim();
                if (meetingType != "rbNext" && meetingType != "rbCenterWise" && meetingType != "rbDetail")
                    meetingType = "rbNext";

                if (meetingType == "rbCenterWise" && centerId == -1)
                    throw new ArgumentException("Please select a collection center.");

                if (meetingType == "rbDetail" && officeId == -1)
                    throw new ArgumentException("Please select an office.");

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                // SP parameters are nvarchar(100), compared against date columns via
                // dynamic SQL string concatenation — 'yyyy-MM-dd' is the one unambiguous
                // format regardless of server locale/date-format settings.
                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                List<CenterMeetingDetailRowDto> rows;

                if (meetingType == "rbDetail")
                {
                    // Field visit schedule — different SP, different (pivoted) shape.
                    // Not wired into this DTO/view; needs its own model once the
                    // expected report layout for it is confirmed.
                    throw new NotSupportedException(
                        "Field visit schedule report uses a different data shape and is not yet implemented against this endpoint.");
                }

                // sp_4_113_GetCenterMeetingDetailReport — exact parameter names as declared
                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExpOfficeId", officeId, DbType.Int32);
                parameters.Add("@SqlFilterExpCenterId", centerId, DbType.Int32);
                parameters.Add("@SqlFilterExpFromDate", fromDateStr, DbType.String, size: 100);
                parameters.Add("@SqlFilterExpToDate", toDateStr, DbType.String, size: 100);
                parameters.Add("@SqlFilterExpIsNextMeeting", meetingType == "rbNext" ? 1 : 0, DbType.Int32);

                rows = (await connection.QueryAsync<CenterMeetingDetailRowDto>(
                    "sp_4_113_GetCenterMeetingDetailReport",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 600
                )).AsList();

                string branchName = "All";
                if (officeId != -1)
                {
                    branchName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId = @Id",
                        new { Id = officeId }) ?? "All";
                }

                string? collectionCenterName = null;
                if (centerId != -1)
                {
                    collectionCenterName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT CollectionCenterName FROM SycCollectionCenter WHERE SycCollectionCenterId = @Id",
                        new { Id = centerId });
                }

                return new CenterMeetingDetailData
                {
                    Rows = rows,
                    TotalRecords = rows.Count,
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    FromDateAd = fromDateStr,
                    ToDateAd = toDateStr,
                    BranchName = branchName,
                    CollectionCenterName = collectionCenterName,
                    MeetingType = meetingType,
                    MeetingTypeName = GetMeetingTypeName(meetingType)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync (CenterMeetingDetail)");
                throw;
            }
        }
    }
}