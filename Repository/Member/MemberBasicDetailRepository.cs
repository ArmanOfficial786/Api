// Repository/Member/MemberBasicDetailsRepository.cs
using Dapper;
using NexgenCosysReport.DbContext;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Dtos.RequestDtos.Member;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Member;
using System.Data;

namespace NexgenCosysReport.Repository.Member
{
    public class MemberBasicDetailsRepository : IMemberBasicDetail
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<MemberBasicDetailsRepository> _logger;

        public MemberBasicDetailsRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<MemberBasicDetailsRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<List<MemberBasicDetailsSpDto>> GetMemberBasicDetails(MemberBasicDetailsRequest request)
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Build SQL filter string (exactly as original)
            var sqlFilterExp = BuildSqlFilter(request);
            var sqlFilterOrderby = BuildOrderBy(request.OrderBy);

            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExp", sqlFilterExp);
            parameters.Add("@SqlFilterOrderby", sqlFilterOrderby);

            var results = await connection.QueryAsync<MemberBasicDetailsSpDto>(
                "sp_4_11_GetMemberDetailsWithBirthday", // note invisible char after 'Birthday'
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return results.AsList();
        }

        private string BuildSqlFilter(MemberBasicDetailsRequest request)
        {
            var sqlFilterExp = string.Empty;

            // If a specific member is selected, ignore date range and branch filter?
            // Original: if (memmemberRegistrationId > 0) then add filter and skip others?
            // Actually original does: if memberId >0, use that filter only (still adds branch filter below? it doesn't skip others, but the else if means date filter only when memberId <=0)
            // We'll replicate exactly:
            if (request.MemberRegistrationId > 0)
            {
                sqlFilterExp += $" And MR.MemmemberRegistrationId = {request.MemberRegistrationId}";
            }
            else if (!string.IsNullOrEmpty(request.FromDate) && !string.IsNullOrEmpty(request.ToDate))
            {
                // Convert Nepali to English (AD) as original does
                string fromDateAd = _dateConverter.BsToAdStringAsync(request.FromDate).GetAwaiter().GetResult();
                string toDateAd = _dateConverter.BsToAdStringAsync(request.ToDate).GetAwaiter().GetResult();
                if (!string.IsNullOrEmpty(fromDateAd) && !string.IsNullOrEmpty(toDateAd))
                {
                    sqlFilterExp += $" And MR.RegistrationOn between '{fromDateAd}' And '{toDateAd}'";
                }
            }

            // Branch filter
            if (!string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
            {
                // If BranchIds contains commas, use IN clause; otherwise single value
                if (request.BranchIds.Contains(','))
                {
                    sqlFilterExp += $" And MR.UsmOfficeId in({request.BranchIds})";
                }
                else
                {
                    sqlFilterExp += $" And MR.UsmOfficeId = {request.BranchIds}";
                }
            }

            // Add any extra filters from original: order by is separate

            return sqlFilterExp;
        }

        private string BuildOrderBy(string? orderBy)
        {
            if (string.IsNullOrEmpty(orderBy) || orderBy == "-1")
                return string.Empty;

            // Special case for MemberId sorting
            if (orderBy == "MemberId")
            {
                return " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId";
            }

            // For others, just "order by " + column name
            return $" order by {orderBy}";
        }
    }
}
