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

        private static string SanitizeIdList(string? ids)
        {
            if (string.IsNullOrWhiteSpace(ids) || ids == "-1" || ids == "string")
                return "-1";

            var validIds = ids
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => long.TryParse(id, out _));

            var joined = string.Join(",", validIds);
            return string.IsNullOrEmpty(joined) ? "-1" : joined;
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

                var branchId = "-1";
                if (!string.IsNullOrEmpty(request.BranchId) &&
                    request.BranchId != "-1" &&
                    long.TryParse(request.BranchId, out var bId))
                {
                    branchId = bId.ToString();
                }

                var collectionCenterId = "-1";
                if (!string.IsNullOrEmpty(request.CollectionCenterId) &&
                    request.CollectionCenterId != "-1" &&
                    long.TryParse(request.CollectionCenterId, out var cId))
                {
                    collectionCenterId = cId.ToString();
                }

                var meetingType = request.MeetingType?.Trim();
                if (meetingType != "rbNext" && meetingType != "rbCenterWise" && meetingType != "rbDetail")
                    meetingType = "rbNext";

                if (meetingType == "rbCenterWise" && collectionCenterId == "-1")
                    throw new ArgumentException("Please select a collection center.");

                if (meetingType == "rbDetail" && branchId == "-1")
                    throw new ArgumentException("Please select an office.");

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                var spName = meetingType == "rbDetail"
                    ? "sp_GetCenterFieldVisitScheduleReport"
                    : "sp_GetCenterMeetingDetailReport";

                var parameters = new DynamicParameters();
                parameters.Add("@BranchId", branchId == "-1" ? (object)DBNull.Value : long.Parse(branchId), DbType.Int64);
                parameters.Add("@CenterId", collectionCenterId == "-1" ? (object)DBNull.Value : long.Parse(collectionCenterId), DbType.Int64);
                parameters.Add("@FromDate", fromDateAd.Date, DbType.Date);
                parameters.Add("@ToDate", toDateAd.Date, DbType.Date);
                parameters.Add("@IsNextMeeting", meetingType == "rbNext" ? 1 : 0, DbType.Int32);

                var rows = (await connection.QueryAsync<CenterMeetingDetailRowDto>(
                    spName,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 600
                )).AsList();

                string branchName = "All";
                if (branchId != "-1")
                {
                    branchName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId = @Id",
                        new { Id = long.Parse(branchId) }) ?? "All";
                }

                string? collectionCenterName = null;
                if (collectionCenterId != "-1")
                {
                    collectionCenterName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT CollectionCenterName FROM SycCollectionCenter WHERE SycCollectionCenterId = @Id",
                        new { Id = long.Parse(collectionCenterId) });
                }

                return new CenterMeetingDetailData
                {
                    Rows = rows,
                    TotalRecords = rows.Count,
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    FromDateAd = fromDateAd.ToString("yyyy-MM-dd"),
                    ToDateAd = toDateAd.ToString("yyyy-MM-dd"),
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