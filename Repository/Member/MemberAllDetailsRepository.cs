using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Dtos.RequestDtos.Member;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Member;
using System.Data;

namespace NexgenCosysReport.Repository.Member
{
    public class MemberAllDetailsRepository : IMemberAllDetails
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<MemberAllDetailsRepository> _logger;

        public MemberAllDetailsRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<MemberAllDetailsRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<List<MemberAllDetailSpResponse>> GetMemberAllDetailsAsync(MemberAllDetailRequst request)
        {
            var sqlFilterExp = await BuildSqlFilter(request);
            var sqlOrderBy = BuildOrderBy(request.orderby);

            var fullFilterExp = sqlFilterExp + sqlOrderBy;

            var connectionString = _context.Database.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", fullFilterExp, DbType.String, size: -1);

                var results = await connection.QueryAsync<MemberAllDetailSpResponse>(
                    "sp_4_11_GetMemberAllDetailsReport",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return results.ToList();
            }
        }

        private async Task<string> BuildSqlFilter(MemberAllDetailRequst request)
        {
            var sqlFilterExp = string.Empty;

            if (request.branchId != -1 && request.branchId != 0)
            {
                sqlFilterExp += $" AND MR.UsmOfficeId = {request.branchId}";
            }

            if (request.memberGroupId != -1 && request.memberGroupId != 0)
            {
                sqlFilterExp += $" AND MR.SycMemberGroupId = {request.memberGroupId}";
            }

            if (!string.IsNullOrWhiteSpace(request.fromDate) && request.fromDate.Trim() != "string"
                && !string.IsNullOrWhiteSpace(request.toDate) && request.toDate.Trim() != "string")
            {
                string fromDateAd = await _dateConverter.BsToAdStringAsync(request.fromDate);
                string toDateAd = await _dateConverter.BsToAdStringAsync(request.toDate);

                if (!string.IsNullOrEmpty(fromDateAd) && !string.IsNullOrEmpty(toDateAd))
                    sqlFilterExp += $" AND MR.RegistrationOn BETWEEN '{fromDateAd}' AND '{toDateAd}'";
            }

            return sqlFilterExp;
        }

        // orderBy arrives from the UI as a ready-made SQL clause, e.g. " order by FullName".
        private static string BuildOrderBy(string? orderBy)
        {
            if (string.IsNullOrWhiteSpace(orderBy) || orderBy == "-1")
                return " ORDER BY MR.RegistrationOn ASC";
            switch (orderBy.Trim().ToLower())
            {
                case "membername":
                    return " ORDER BY Name ASC";
                case "memberid":
                    return " ORDER BY MR.MemberId ASC";

                default:
                    return " ORDER BY MR.RegistrationOn ASC";

            }

            return orderBy;
        }
    }
}