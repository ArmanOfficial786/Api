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
    public class CopomisRepository : ICopomis
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<CopomisRepository> _logger;

        public CopomisRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            IWebHostEnvironment webHostEnvironment,
            ILogger<CopomisRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        private static string BuildSqlOrderBy(CopomisRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
                return string.Empty;

            return request.OrderBy.Trim() switch
            {
                "MemberId" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId ",
                "MemberName" => " order by MemberName ",
                "RegistrationDate" => " order by RegistrationOn ",
                "ShareNo" => " order by shareNo DESC",
                "Amount" => " order by TotalShareAmt DESC",
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

        // --------------------------------------------------------------
        // Real ids (member type, collection center) start at 1. Some
        // callers send 0 as their "nothing selected" sentinel instead of
        // -1 (same pattern seen elsewhere in this codebase, e.g.
        // ShareTypeId). Both this repository's own filter and the SP's
        // internal @collectorId check only ever test for -1, so a bare 0
        // was silently filtering on a nonexistent id and zeroing out the
        // whole result set. Normalize any id <= 0 to -1 here so both
        // sides agree on what "no filter" means.
        // --------------------------------------------------------------
        private static long NormalizeSentinel(long id) => id <= 0 ? -1 : id;

        public async Task<CopomisData> GetReportDataAsync(CopomisRequestDto request)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDateBs);
                var tillDateAdStr = tillDateAd.ToString("yyyy-MM-dd");

                var officeIds = SanitizeOfficeIds(request.OfficeIds);
                var memberTypeId = NormalizeSentinel(request.MemberTypeId);
                var collectionCenterId = NormalizeSentinel(request.CollectionCenterId);

                var sqlFilterExp = new StringBuilder();
                string? branchName = null;
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

                if (memberTypeId != -1)
                {
                    sqlFilterExp.Append(" AND M.SycMemberTypeId = ").Append(memberTypeId);
                    memberTypeName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT MemberTypeName FROM SycMemberType WHERE SycMemberTypeId = @Id",
                        new { Id = memberTypeId });
                }

                if (collectionCenterId != -1)
                {
                    collectionCenterName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT CollectionCenterName FROM SycCollectionCenter WHERE SycCollectionCenterId = @Id",
                        new { Id = collectionCenterId });
                }

                if (request.MemberGroupId != -1)
                {
                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id",
                        new { Id = request.MemberGroupId });
                }

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExpDate", tillDateAdStr, DbType.String, size: -1);
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrder", BuildSqlOrderBy(request), DbType.String, size: -1);
                parameters.Add("@collectorId", collectionCenterId, DbType.Int64);
                parameters.Add("@groupId", request.MemberGroupId, DbType.Int64);

                var rows = await connection.QueryAsync<CopomisRowDto>(
                    "sp_8_14_GetCopomisReport",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 180
                );

                var resultList = rows.AsList();

                if (request.ShowMemberPhoto)
                {
                    foreach (var row in resultList)
                    {
                        if (!string.IsNullOrEmpty(row.MemberId))
                        {
                            row.MemberImage = await GetMemberPhotoBase64Async(row.MemberId);
                        }
                    }
                }

                return new CopomisData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalMembers = resultList
                        .Select(r => r.MemberId)
                        .Distinct()
                        .Count(),
                    TotalShareNo = resultList.Sum(r => r.ShareNo ?? 0),
                    TotalShareAmount = resultList.Sum(r => r.TotalShareAmt ?? 0),
                    TillDateBs = request.TillDateBs,
                    BranchName = branchName ?? "All",
                    CollectionCenterName = collectionCenterName ?? "All",
                    MemberGroupName = memberGroupName ?? "All",
                    MemberTypeName = memberTypeName ?? "All",
                    OrderBy = request.OrderBy
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync");
                throw;
            }
        }

        private async Task<string?> GetMemberPhotoBase64Async(string memberId)
        {
            try
            {
                var webRoot = _webHostEnvironment.WebRootPath ?? _webHostEnvironment.ContentRootPath;
                var path = Path.Combine(webRoot, "UploadedDocuments", "MemMemberManagement", "MemMemberPhotoAndSignature");

                if (!Directory.Exists(path))
                    return null;

                var photoFile = Path.Combine(path, $"{memberId}_MemberPhoto.jpg");
                if (!File.Exists(photoFile))
                {
                    var fallback = Path.Combine(path, "PhotoNotAvailable.jpg");
                    if (!File.Exists(fallback))
                        return null;
                    photoFile = fallback;
                }

                var bytes = await File.ReadAllBytesAsync(photoFile);
                return Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load member photo for {MemberId}", memberId);
                return null;
            }
        }
    }
}