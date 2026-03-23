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

        public MemberIdCardRepository(AppDbContext context)
        {
            _context = context;
        }

        public List<MemberIdCardResponseModel> GetMemberIdCardData(MemberIdCardRequest request)
        {
            var sqlFilterExp = BuildSqlFilter(request);
            var sqlOrderBy = " ORDER BY RegistrationOn ASC";

            var connectionString = _context.Database.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new DynamicParameters();

                parameters.Add("@SqlFilterExp", sqlFilterExp);
                parameters.Add("@SqlOrderBy", sqlOrderBy);

                var results = connection.Query<MemberIdCardResponseModel>(
                    "sp_4_11_GetMemberIDCard",
                    parameters,
                    commandType: CommandType.StoredProcedure
                ).ToList();

                return results;
            }
        }

        private string BuildSqlFilter(MemberIdCardRequest request)
        {
            var sqlFilterExp = string.Empty;

            if (!string.IsNullOrEmpty(request.memberId))
            {
                sqlFilterExp += $" And MR.MemberId = '{request.memberId}'";
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
                sqlFilterExp += $" And MR.RegistrationOn between '{request.fromDate}' And '{request.toDate}'";
            }

            return sqlFilterExp;
        }
    }
}