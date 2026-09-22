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
    public class ShareTransferRepository : IShareTransfer
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<ShareTransferRepository> _logger;

        public ShareTransferRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<ShareTransferRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        private static string BuildSqlOrderBy(ShareTransferRequestDto request)
        {
            if (string.IsNullOrEmpty(request.OrderBy) || request.OrderBy == "-1")
                return string.Empty;

            return request.OrderBy.Trim() switch
            {
                "MemberId" => " order by substring(m.MemberId, 1,(len(m.MemberId)-charindex('-', m.MemberId))-1), m.MemberId ",
                "Name" => " order by Name ",
                "PreviousMemberId" => " order by substring(pm.MemberId, 1,(len(pm.MemberId)-charindex('-',pm.MemberId))-1), pm.MemberId ",
                "PreviousMemberName" => " order by PreviousMemberName ",
                "TotalNoShare" => " order by TotalNoShare DESC",
                "Date" => " order by Date",
                "Amount" => " order by Amount DESC",
                _ => string.Empty
            };
        }

        // --------------------------------------------------------------
        // The rest of the app's BS dates are slash-separated (e.g.
        // "2079/05/01"), but requests to this endpoint have arrived with
        // dashes ("2079-06-06"). If the date converter expects the
        // slash format, a dash-separated string can silently parse to
        // the wrong AD date instead of throwing - which would explain
        // why the exact same filter syntax works in SSMS with literal
        // AD dates but returns nothing when the BS conversion is in the
        // loop. Normalizing here removes that ambiguity.
        // --------------------------------------------------------------
        private static string NormalizeBsDate(string bsDate) => bsDate.Trim().Replace("-", "/");

        public async Task<ShareTransferData> GetReportDataAsync(ShareTransferRequestDto request)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sqlFilterExp = new StringBuilder();
                string? officeName = null;
                string? shareTypeName = null;
                string? memberGroupName = null;

                if (!string.IsNullOrEmpty(request.FromDateBs) && !string.IsNullOrEmpty(request.ToDateBs)
                    && request.FromDateBs != "-1" && request.ToDateBs != "-1")
                {
                    var fromDateBsNormalized = NormalizeBsDate(request.FromDateBs);
                    var toDateBsNormalized = NormalizeBsDate(request.ToDateBs);

                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(fromDateBsNormalized);
                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(toDateBsNormalized);

                    var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                    var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                    // Log the actual computed AD range so a "no data" report
                    // can be checked against what was really queried, instead
                    // of only seeing the original BS input.
                    _logger.LogInformation(
                        "ShareTransferReport date filter: BS {FromBs}..{ToBs} -> AD {FromAd}..{ToAd}",
                        fromDateBsNormalized, toDateBsNormalized, fromDateStr, toDateStr);

                    sqlFilterExp.Append(" And a.TransactionOn >= '").Append(fromDateStr).Append("'");
                    sqlFilterExp.Append(" And a.TransactionOn <= '").Append(toDateStr).Append("'");
                }

                if (request.OfficeId != -1)
                {
                    sqlFilterExp.Append(" And o.UsmOfficeId = ").Append(request.OfficeId);
                    officeName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId = @Id",
                        new { Id = request.OfficeId });
                }

                // ShareTypeId's "no filter" sentinel is -1 (matches the form
                // default and every other sentinel in this repository).
                if (request.ShareTypeId != -1)
                {
                    sqlFilterExp.Append(" And s.ShmShareTypeId = ").Append(request.ShareTypeId);
                    shareTypeName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT ShareTypeName FROM ShmShareType WHERE ShmShareTypeId = @Id",
                        new { Id = request.ShareTypeId });
                }

                if (request.MemberGroupId != -1)
                {
                    sqlFilterExp.Append(" AND m.SycMemberGroupId = ").Append(request.MemberGroupId);
                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id",
                        new { Id = request.MemberGroupId });
                }

                sqlFilterExp.Append(BuildSqlOrderBy(request));

                _logger.LogInformation("ShareTransferReport @SqlFilterExp: {Filter}", sqlFilterExp.ToString());

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);

                var rows = await connection.QueryAsync<ShareTransferRowDto>(
                    "sp_8_14_GetShareTransfered",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var resultList = rows.AsList();

                return new ShareTransferData
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