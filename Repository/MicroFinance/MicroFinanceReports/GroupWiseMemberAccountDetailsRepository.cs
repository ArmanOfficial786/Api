// Repository/Microfinance/MicrofinanceReport/GroupWiseMemberAccountDetailsRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.MicroFinance.MicroFinanceReports;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Microfinance.MicrofinanceReport;
using System.Data;

namespace NexgenCosysReport.Repository.Microfinance.MicrofinanceReport
{
    public class GroupWiseMemberAccountDetailsRepository : IGroupWiseMemberAccountDetailsRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<GroupWiseMemberAccountDetailsRepository> _logger;

        public GroupWiseMemberAccountDetailsRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<GroupWiseMemberAccountDetailsRepository> logger)
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

        private static string BuildSqlOrderBy(string? orderBy)
        {
            if (string.IsNullOrEmpty(orderBy) || orderBy == "-1")
                return string.Empty;

            return orderBy.Trim() switch
            {
                "MemberId" => " order by MemberId",
                "MemberName" => " order by MemberName",
                "AccountNo" => " order by AccountNo",
                _ => string.Empty
            };
        }

        public async Task<GroupWiseMemberAccountDetailsData> GetReportDataAsync(GroupWiseMemberAccountDetailsRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.FromDateBs) || request.FromDateBs == "-1")
                    throw new ArgumentException("From Date is required.");

                if (string.IsNullOrWhiteSpace(request.ToDateBs) || request.ToDateBs == "-1")
                    throw new ArgumentException("To Date is required.");

                var branchIds = SanitizeIdList(request.BranchIds);
                if (branchIds == "-1")
                    throw new ArgumentException("Please select Branch Name.");

                var collectionCenterIds = SanitizeIdList(request.CollectionCenterIds);

                long? memberGroupId = null;
                if (!string.IsNullOrEmpty(request.MemberGroupId) &&
                    request.MemberGroupId != "-1" &&
                    long.TryParse(request.MemberGroupId, out var mg))
                {
                    memberGroupId = mg;
                }

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                var sqlOrderExpFrom = $"'{fromDateAd:MM/dd/yyyy}'";
                var sqlOrderExpTill = $"'{toDateAd:MM/dd/yyyy}'";

                var sqlFilterExp = $" And LS.UsmOfficeId in({branchIds})";
                if (memberGroupId.HasValue)
                    sqlFilterExp += $" AND MR.SycMemberGroupId = {memberGroupId.Value}";
                if (collectionCenterIds != "-1")
                    sqlFilterExp += $" And Mg.SycCollectionCenterId in({collectionCenterIds})";

                var sqlOrderExp = BuildSqlOrderBy(request.OrderBy);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);
                parameters.Add("@SqlOrderExpFrom", sqlOrderExpFrom, DbType.String, size: -1);
                parameters.Add("@SqlOrderExpTill", sqlOrderExpTill, DbType.String, size: -1);
                parameters.Add("@SqlOrderExp", sqlOrderExp, DbType.String, size: -1);

                var rows = (await connection.QueryAsync<GroupWiseMemberAccountDetailsRowDto>(
                    "sp_7_16_GroupWiseMemberAccountDetailsReport",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 600
                )).AsList();

                string branchName = "All";
                if (branchIds != "-1")
                {
                    var names = await connection.QueryAsync<string>(
                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
                    var list = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    branchName = list.Count > 0 ? string.Join(", ", list) : "All";
                }

                string? collectionCenterName = null;
                if (collectionCenterIds != "-1")
                {
                    var ccNames = await connection.QueryAsync<string>(
                        $"SELECT CollectionCenterName FROM SycCollectionCenter WHERE SycCollectionCenterId IN ({collectionCenterIds})");
                    var list = ccNames.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                    collectionCenterName = list.Count > 0 ? string.Join(", ", list) : null;
                }

                string? memberGroupName = null;
                if (memberGroupId.HasValue)
                {
                    memberGroupName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT Name FROM SycMemberGroup WHERE SycMemberGroupId = @Id",
                        new { Id = memberGroupId.Value });
                }

                return new GroupWiseMemberAccountDetailsData
                {
                    Rows = rows,
                    TotalRecords = rows.Count,
                    TotalBalance = rows.Sum(r => r.Balance ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    FromDateAd = fromDateAd.ToString("yyyy-MM-dd"),
                    ToDateAd = toDateAd.ToString("yyyy-MM-dd"),
                    BranchName = branchName,
                    CollectionCenterName = collectionCenterName,
                    MemberGroupName = memberGroupName,
                    OrderBy = request.OrderBy,
                    GroupByCollectionCenter = request.GroupByCollectionCenter,
                    GroupByMemberGroup = request.GroupByMemberGroup,
                    GroupByBranch = request.GroupByBranch
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync (GroupWiseMemberAccountDetails)");
                throw;
            }
        }
    }
}