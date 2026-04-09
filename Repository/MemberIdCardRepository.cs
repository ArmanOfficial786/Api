using Dapper;
using JsSampleReport.Dtos.ReportDtos;
using JsSampleReport.Dtos.RequestDtos;
using JsSampleReport.Inteface.ServiceInterface;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace JsSampleReport.Repository
{
    public class MemberIdCardRepository : IMemberIdCard
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;

        public MemberIdCardRepository(AppDbContext context, IDateConverterService dateConverter)
        {
            _context = context;
            _dateConverter = dateConverter;
        }

        public async Task<List<MemberIdCardModel>> GetMemberIdCardData(MemberIdCardRequest request)
        {
            var sqlFilterExp = await BuildSqlFilter(request);
            var sqlOrderBy = BuildOrderBy(request.orderby);
            //var sqlOrderBy = request.orderby;

            var connectionString = _context.Database.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new DynamicParameters();

                parameters.Add("@SqlFilterExp", sqlFilterExp);
                parameters.Add("@SqlOrderBy", sqlOrderBy);

                var results = connection.Query<MemberIdCardModel>(
                    "sp_4_11_GetMemberIDCard",
                    parameters,
                    commandType: CommandType.StoredProcedure
                ).ToList();

                return results;
            }
        }

        private async Task<string> BuildSqlFilter(MemberIdCardRequest request)
        {
            var sqlFilterExp = string.Empty;

            if (!string.IsNullOrWhiteSpace(request.memberId)
                && request.memberId.Trim() != "string")
            {
                sqlFilterExp += $" AND MR.MemberId = '{request.memberId.Trim()}'";
            }

            if (request.branchId != -1 && request.branchId != 0)
            {
                sqlFilterExp += $" And U.UsmOfficeId = {request.branchId}";
            }

            if (request.memberGroupId != -1 && request.memberGroupId != 0)
            {
                sqlFilterExp += $" And MR.SycMemberGroupId = {request.memberGroupId}";
            }

            if (!string.IsNullOrEmpty(request.fromDate) && !string.IsNullOrEmpty(request.toDate))
            {
                //sqlFilterExp += $" And MR.RegistrationOn between '{request.fromDate}' And '{request.toDate}'";
                string fromDateAd = await _dateConverter.BsToAdStringAsync(request.fromDate);
                string toDateAd = await _dateConverter.BsToAdStringAsync(request.toDate);

                if (!string.IsNullOrEmpty(fromDateAd) && !string.IsNullOrEmpty(toDateAd))
                    sqlFilterExp += $" And MR.RegistrationOn BETWEEN '{fromDateAd}' AND '{toDateAd}'";
            }

            return sqlFilterExp;
        }
        // ================= ORDER BY BUILDER =================
        private static string BuildOrderBy(string? orderBy)
        {
            if (string.IsNullOrWhiteSpace(orderBy) || orderBy == "-1")
                return " ORDER BY MR.RegistrationOn ASC";

            switch (orderBy.Trim().ToLower())
            {
                case "1":
                case "name":
                case "membername":
                    return " ORDER BY Name ASC";

                case "2":
                case "sex":
                    return " ORDER BY Sex ASC"; //alias of Geneder
                case "3":
                case "memberid":
                    return " ORDER BY MR.MemberId ASC";
                case "4":
                case "birthonbs":
                    return " ORDER BY MR.BirthOnBS ASC";

                case "5":
                case "registrationon":
                    return " ORDER BY MR.RegistrationOn ASC";

                default:
                    return " ORDER BY MR.RegistrationOn ASC";
            }

        }
    }
}