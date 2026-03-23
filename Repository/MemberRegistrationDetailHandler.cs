using Dapper;
using JsSampleReport;
using JsSampleReport.Dtos.ReportDtos;
using JsSampleReport.Dtos.RequestDtos;
using JsSampleReport.Inteface.ServiceInterface;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace JsSampleProject.ServiceHandler
{
    public class MemberRegistrationDetailHandler : IMemberDetail
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MemberRegistrationDetailHandler> _logger;

        public MemberRegistrationDetailHandler(
            AppDbContext context,
            ILogger<MemberRegistrationDetailHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<MemberRegistrationDetail> GetMemberRegistrationDetail(MemberDetailRequest request)
        {
            var sqlFilterExp = BuildSqlFilter(request);
            var connectionString = _context.Database.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String);

                var results = connection.Query<MemberRegistrationDetail>(
                    "sp_4_11_GetMemberRegistrationDetail",
                    parameters,
                    commandType: CommandType.StoredProcedure
                ).ToList();
                return results;
            }
        }

        public List<CommonHeader> GetCommonHeaders()
        {
            var connectionString = _context.Database.GetConnectionString();
            using (var connection = new SqlConnection(connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", string.Empty, DbType.String);

                var results = connection.Query<CommonHeader>(
                    "sp_2_1_GetCompanyProfile",
                    parameters,
                    commandType: CommandType.StoredProcedure
                ).ToList();

                return results;
            }
        }

        private string BuildSqlFilter(MemberDetailRequest request)
        {
            var sqlFilterExp = string.Empty;

            if (request.branchId != -1 && request.branchId != 0)
            {
                sqlFilterExp += $" And U.UsmOfficeId={request.branchId}";
            }

            if (request.memberGroupId != -1 && request.memberGroupId != 0)
            {
                sqlFilterExp += $" AND MR.SycMemberGroupId = {request.memberGroupId}";
            }

            if (!string.IsNullOrEmpty(request.fromDate) && !string.IsNullOrEmpty(request.toDate))
            {
                sqlFilterExp += $" And MR.RegistrationOn between '{request.fromDate}' And '{request.toDate}'";
            }

            if (!string.IsNullOrEmpty(sqlFilterExp))
            {
                sqlFilterExp += " ORDER BY RegistrationOn Asc";
            }

            return sqlFilterExp;
        }
    }
}
