using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Dtos.RequestDtos.Member;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Member;
using System.Data;

namespace NexgenCosysReport.Repository.Member
{
    public class MemberRegistrationDetailHandler : IMemberDetail
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<MemberRegistrationDetailHandler> _logger;

        public MemberRegistrationDetailHandler(
            AppDbContext context,
            ILogger<MemberRegistrationDetailHandler> logger,
            IDateConverterService dateConverter)
        {
            _context = context;
            _logger = logger;
            _dateConverter = dateConverter;
        }

        public async Task<List<MemberRegistrationDetail>> GetMemberRegistrationDetail(MemberDetailRequest request)
        {
            var sqlFilterExp = await BuildSqlFilter(request);
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

        //public async Task<List<CommonHeader>> GetCommonHeaders()
        //{
        //    var connectionString = _context.Database.GetConnectionString();
        //    using (var connection = new SqlConnection(connectionString))
        //    {
        //        var parameters = new DynamicParameters();
        //        parameters.Add("@SqlFilterExp", string.Empty, DbType.String);

        //        var results = connection.Query<CommonHeader>(
        //            "sp_2_1_GetCompanyProfile",
        //            parameters,
        //            commandType: CommandType.StoredProcedure
        //        ).ToList();

        //        return results;
        //    }
        //}

        private async Task<string> BuildSqlFilter(MemberDetailRequest request)
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
                //sqlFilterExp += $" And MR.RegistrationOn between '{request.fromDate}' And '{request.toDate}'";
                string fromDateAd = await _dateConverter.BsToAdStringAsync(request.fromDate);
                string toDateAd = await _dateConverter.BsToAdStringAsync(request.toDate);

                if (!string.IsNullOrEmpty(fromDateAd) && !string.IsNullOrEmpty(toDateAd))
                    sqlFilterExp += $" And MR.RegistrationOn BETWEEN '{fromDateAd}' AND '{toDateAd}'";
            }

            if (!string.IsNullOrEmpty(sqlFilterExp))
            {
                sqlFilterExp += " ORDER BY RegistrationOn Asc";
            }

            return sqlFilterExp;
        }
    }
}
