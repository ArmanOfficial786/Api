using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Share;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Share;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.Share
{
    public class ShareHoldingRepository : IShareHolding
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<ShareHoldingRepository> _logger;

        public ShareHoldingRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<ShareHoldingRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        private static string BuildSqlOrderBy(ShareHoldingRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
                return string.Empty;

            return request.OrderBy.Trim() switch
            {
                "MemberId" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId ",
                "MemberName" => " order by MemberName ",
                "TotalNoShare" => " order by TotalNoShare DESC",
                "HoldingPeriodFromOnBs" => " order by HoldingPeriodFromBs ",
                "ShareType" => " order by ShareType",
                "Amount" => " order by Amount DESC",
                _ => string.Empty
            };
        }

        public async Task<ShareHoldingData> GetReportDataAsync(ShareHoldingRequestDto request)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sqlFilterExp = new StringBuilder();
                string? officeName = null;
                string? shareTypeName = null;
                string? memberTypeName = null;
                string? memberGroupName = null;

                if (request.MemberId != -1)
                {
                    sqlFilterExp.Append(" And m.MemMemberRegistrationId = ").Append(request.MemberId);
                }

                string fromDateAd = string.Empty;
                string toDateAd = string.Empty;

                if (!string.IsNullOrEmpty(request.FromDateBs) && !string.IsNullOrEmpty(request.ToDateBs))
                {
                    var fromDate = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                    var toDate = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                    fromDateAd = fromDate.ToString("yyyy-MM-dd");
                    toDateAd = toDate.ToString("yyyy-MM-dd");

                    sqlFilterExp.Append(" And s.ShareHoldingPeriodFromOn between '")
                                .Append(fromDateAd)
                                .Append("' And '")
                                .Append(toDateAd)
                                .Append("'");
                }

                if (request.OfficeId != -1)
                {
                    sqlFilterExp.Append(" And o.UsmOfficeId = ").Append(request.OfficeId);
                    officeName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId = @Id",
                        new { Id = request.OfficeId });
                }

                if (request.ShareTypeId != -1)
                {
                    sqlFilterExp.Append(" And s.ShmShareTypeId = ").Append(request.ShareTypeId);
                    shareTypeName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT ShareTypeName FROM ShmShareType WHERE ShmShareTypeId = @Id",
                        new { Id = request.ShareTypeId });
                }

                if (request.MemberTypeId != -1)
                {
                    sqlFilterExp.Append(" And syc.SycMemberTypeId = ").Append(request.MemberTypeId);
                    memberTypeName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT MemberTypeName FROM SycMemberType WHERE SycMemberTypeId = @Id",
                        new { Id = request.MemberTypeId });
                }

                if (request.MemberGroupId != -1)
                {
                    sqlFilterExp.Append(" AND m.SycMemberGroupId = ").Append(request.MemberGroupId);
                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id",
                        new { Id = request.MemberGroupId });
                }

                var orderByClause = BuildSqlOrderBy(request);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrderby", orderByClause, DbType.String, size: -1);
                parameters.Add("@Fromdate", fromDateAd, DbType.String, size: 10);
                parameters.Add("@ToDate", toDateAd, DbType.String, size: 10);
                parameters.Add("@FromdateBs", request.FromDateBs ?? "", DbType.String, size: 10);
                parameters.Add("@ToDateBs", request.ToDateBs ?? "", DbType.String, size: 10);

                var rows = await connection.QueryAsync<ShareHoldingRowDto>(
                    "sp_8_14_GetShareHolding",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var resultList = rows.AsList();

                return new ShareHoldingData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalMembers = resultList
                        .Select(r => r.MemberId)
                        .Distinct()
                        .Count(),
                    TotalNoShare = resultList.Sum(r => r.TotalNoShare ?? 0),
                    TotalAmount = resultList.Sum(r => r.Amount ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    OfficeName = officeName ?? "All",
                    ShareTypeName = shareTypeName ?? "All",
                    MemberTypeName = memberTypeName ?? "All",
                    MemberGroupName = memberGroupName,
                    OrderBy = request.OrderBy
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