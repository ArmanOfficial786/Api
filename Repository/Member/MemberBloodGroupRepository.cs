using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Dtos.RequestDtos.Member;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Member;
using System.Data;

namespace NexgenCosysReport.Repository.Member
{
    public class MemberBloodGroupReportRepository : IMemberBloodGroup
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<MemberBloodGroupReportRepository> _logger;

        public MemberBloodGroupReportRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<MemberBloodGroupReportRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<List<MemberBloodGroupSpDto>> GetMemberBloodGroupReport(MemberBloodGroupReportRequest request)
        {
            var sqlFilterExp = BuildSqlFilter(request);
            var connectionString = _context.Database.GetConnectionString();

            using var connection = new SqlConnection(connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExp", sqlFilterExp);

            var results = await connection.QueryAsync<MemberBloodGroupSpDto>(
                "sp_4_11_GetMemberBloodGroupDetailsReport",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return results.AsList();
        }

        private string BuildSqlFilter(MemberBloodGroupReportRequest request)
        {
            var sqlFilterExp = string.Empty;

            // Branch filter
            if (request.BranchId != -1 && request.BranchId != 0)
            {
                sqlFilterExp += $" And U.UsmOfficeId = {request.BranchId}";
            }

            // Blood Group option
            if (request.BloodGroupOption == 2) // Available
            {
                sqlFilterExp += " AND MR.BloodGroup IS NOT NULL ";
            }
            else if (request.BloodGroupOption == 3) // Unavailable
            {
                sqlFilterExp += " AND MR.BloodGroup IS NULL ";
            }
            // option 1 = All => no filter

            // Member Group
            if (request.MemberGroupId != -1 && request.MemberGroupId != 0)
            {
                sqlFilterExp += $" AND MR.SycMemberGroupId = {request.MemberGroupId}";
            }

            // Date range - convert Nepali to English AD
            if (!string.IsNullOrEmpty(request.FromDate) && !string.IsNullOrEmpty(request.ToDate))
            {
                string fromDateAd = _dateConverter.BsToAdStringAsync(request.FromDate).GetAwaiter().GetResult();
                string toDateAd = _dateConverter.BsToAdStringAsync(request.ToDate).GetAwaiter().GetResult();

                if (!string.IsNullOrEmpty(fromDateAd) && !string.IsNullOrEmpty(toDateAd))
                {
                    sqlFilterExp += $" And MR.RegistrationOn BETWEEN '{fromDateAd}' AND '{toDateAd}'";
                }
            }

            // Order by (default RegistrationOn ASC)
            sqlFilterExp += " ORDER BY RegistrationOn ASC";

            return sqlFilterExp;
        }
    }
}