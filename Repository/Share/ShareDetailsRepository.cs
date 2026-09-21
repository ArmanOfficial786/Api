using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Share;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Share;
using System.Data;
using System.Globalization;
using System.Text;

namespace NexgenCosysReport.Repository.Share
{
    public class ShareDetailsRepository : IShareDetails
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<ShareDetailsRepository> _logger;

        public ShareDetailsRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<ShareDetailsRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        private static string BuildSqlOrderBy(ShareDetailsRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
                return string.Empty;

            return request.OrderBy.Trim() switch
            {
                "MemberId" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId ",
                "MemberName" => " order by MemberName ",
                "RegistrationDate" => " order by RegistrationDate ",
                "ShareNo" => " order by TotalShare DESC",
                "Amount" => " order by TotalShareAmount DESC",
                _ => string.Empty
            };
        }

        private static string SanitizeOfficeIds(string? officeIds)
        {
            if (string.IsNullOrWhiteSpace(officeIds) || officeIds == "-1" || officeIds == "string")
                return string.Empty;

            var validIds = officeIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => long.TryParse(id, out _));

            return string.Join(",", validIds);
        }

        public async Task<ShareDetailsData> GetReportDataAsync(ShareDetailsRequestDto request)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDateBs);
                var tillDateAdStr = tillDateAd.ToString("yyyy-MM-dd");

                var officeIds = SanitizeOfficeIds(request.OfficeIds);

                var sqlFilterExp = new StringBuilder();
                string? branchName = null;
                string? shareTypeName = null;
                string? memberTypeName = null;
                string? collectionCenterName = null;
                string? memberGroupName = null;

                if (!string.IsNullOrEmpty(officeIds))
                {
                    sqlFilterExp.Append(" And S.UsmOfficeId in ( ").Append(officeIds).Append(" )");

                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({officeIds})");
                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : null;
                }

                if (request.ShareTypeId != -1)
                {
                    sqlFilterExp.Append(" And S.ShmShareTypeId = ").Append(request.ShareTypeId);
                    shareTypeName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT ShareTypeName FROM ShmShareType WHERE ShmShareTypeId = @Id",
                        new { Id = request.ShareTypeId });
                }

                if (request.MemberTypeId != -1)
                {
                    sqlFilterExp.Append(" AND M.SycMemberTypeId = ").Append(request.MemberTypeId);
                    memberTypeName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT MemberTypeName FROM SycMemberType WHERE SycMemberTypeId = @Id",
                        new { Id = request.MemberTypeId });
                }

                if (request.CollectionCenterId != -1)
                {
                    collectionCenterName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT CollectionCenterName FROM SycCollectionCenter WHERE SycCollectionCenterId = @Id",
                        new { Id = request.CollectionCenterId });
                }

                if (request.MemberGroupId != -1)
                {
                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id",
                        new { Id = request.MemberGroupId });
                }

                var sqlFilterExpGreaterOrLessThan = request.IsGreaterThan
                    ? " and TotalShareAmount > " + request.TotalShareAmount.ToString(CultureInfo.InvariantCulture)
                    : " and TotalShareAmount < " + request.TotalShareAmount.ToString(CultureInfo.InvariantCulture);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExpDate", tillDateAdStr, DbType.String, size: -1);
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrder", BuildSqlOrderBy(request), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpGreaterOrLessThanTotalAmt", sqlFilterExpGreaterOrLessThan, DbType.String, size: -1);
                parameters.Add("@collectorId", request.CollectionCenterId, DbType.Int64);
                parameters.Add("@groupId", request.MemberGroupId, DbType.Int64);

                var rows = await connection.QueryAsync<ShareDetailsRowDto>(
                    "sp_8_14_GetShareDetails",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 180
                );

                var resultList = rows.AsList();

                return new ShareDetailsData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalMembers = resultList
                        .Select(r => r.MemberId)
                        .Distinct()
                        .Count(),
                    TotalShare = resultList.Sum(r => r.TotalShare ?? 0),
                    TotalShareAmount = resultList.Sum(r => r.TotalShareAmount ?? 0),
                    TillDateBs = request.TillDateBs,
                    BranchName = branchName ?? "All",
                    ShareTypeName = shareTypeName ?? "All",
                    MemberTypeName = memberTypeName ?? "All",
                    CollectionCenterName = collectionCenterName ?? "All",
                    MemberGroupName = memberGroupName ?? "All",
                    OrderBy = request.OrderBy,
                    ReportType = request.ReportType?.ToUpper() ?? "ENGLISH"
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