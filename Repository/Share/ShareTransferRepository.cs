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

                if (!string.IsNullOrEmpty(request.FromDateBs) && !string.IsNullOrEmpty(request.ToDateBs))
                {
                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                    sqlFilterExp.Append(" And a.TransactionOn between '")
                                .Append(fromDateAd.ToString("yyyy-MM-dd"))
                                .Append("' And '")
                                .Append(toDateAd.ToString("yyyy-MM-dd"))
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

                if (request.MemberGroupId != -1)
                {
                    sqlFilterExp.Append(" AND m.SycMemberGroupId = ").Append(request.MemberGroupId);
                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id",
                        new { Id = request.MemberGroupId });
                }

                sqlFilterExp.Append(BuildSqlOrderBy(request));

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