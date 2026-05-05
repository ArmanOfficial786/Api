// Repository/MemberLookUpRepository.cs
using Dapper;
using JsSampleReport.Dtos.RequestDtos.Common;
using JsSampleReport.Inteface.ServiceInterface;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace JsSampleReport.Repository
{
    public class MemberLookUpRepository : IMemberLookUp
    {
        private readonly AppDbContext _context;
        private const int FIXED_PAGE_SIZE = 10;

        public MemberLookUpRepository(AppDbContext context)
        {
            _context = context;
        }

        // ── 1. Paginated + filtered list for the grid ─────────────────────────
        public async Task<PagedResult<MemberLookUpDtos>> GetMemberListAsync(
            MemberLookUpRequest request,
            long userId)
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@SqlUserId", userId);
            parameters.Add("@PageNumber", request.Page);
            parameters.Add("@PageSize", FIXED_PAGE_SIZE);
            parameters.Add("@MemberId", NullIfEmpty(request.MemberId));
            parameters.Add("@MemberName", NullIfEmpty(request.MemberName));
            parameters.Add("@GroupName", NullIfEmpty(request.GroupName));
            parameters.Add("@GroupCode", NullIfEmpty(request.GroupCode));
            parameters.Add("@CenterName", NullIfEmpty(request.CenterName));
            parameters.Add("@CenterCode", NullIfEmpty(request.CenterCode));
            parameters.Add("@Gender", NullIfEmpty(request.Gender));
            parameters.Add("@MobileNo", NullIfEmpty(request.MobileNo));
            parameters.Add("@OfficeName", NullIfEmpty(request.OfficeName));
            parameters.Add("@SortColumn", request.SortColumn);
            parameters.Add("@SortDirection", request.SortDirection);

            var rawItems = (await connection.QueryAsync<MemberLookUpDtos>(
                "sp_4_11_GetMemberRegistrationDetailByRoleForUC",
                parameters,
                commandType: CommandType.StoredProcedure
            )).ToList();

            if (!rawItems.Any())
            {
                return new PagedResult<MemberLookUpDtos>
                {
                    Items = new List<MemberLookUpDtos>(),
                    TotalCount = 0,
                    CurrentPage = request.Page,
                    PageSize = FIXED_PAGE_SIZE,
                    TotalPages = 0
                };
            }

            var first = rawItems.First();

            return new PagedResult<MemberLookUpDtos>
            {
                Items = rawItems,
                TotalCount = first.TotalCount,
                CurrentPage = first.CurrentPage > 0 ? first.CurrentPage : request.Page,
                PageSize = FIXED_PAGE_SIZE,
                TotalPages = first.TotalPages
            };
        }

        // ── 2. Single member selected by user clicking "Sel" ──────────────────
        public async Task<MemberSelectedDto?> GetSelectedMemberAsync(
            long memMemberRegistrationId,
            long userId)
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);

            // Re-use the same SP but filter down to one record by ID
            var parameters = new DynamicParameters();
            parameters.Add("@SqlUserId", userId);
            parameters.Add("@PageNumber", 1);
            parameters.Add("@PageSize", 1);
            parameters.Add("@MemMemberRegistrationId", memMemberRegistrationId);
            parameters.Add("@MemberId", DBNull.Value);
            parameters.Add("@MemberName", DBNull.Value);
            parameters.Add("@GroupName", DBNull.Value);
            parameters.Add("@CenterName", DBNull.Value);
            parameters.Add("@Gender", DBNull.Value);
            parameters.Add("@MobileNo", DBNull.Value);
            parameters.Add("@OfficeName", DBNull.Value);
            parameters.Add("@SortColumn", "MemberId");
            parameters.Add("@SortDirection", "ASC");

            var result = await connection.QueryFirstOrDefaultAsync<MemberSelectedDto>(
                "sp_4_11_GetMemberRegistrationDetailByRoleForUC",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result;
        }

        // ── Helper ────────────────────────────────────────────────────────────
        private static string? NullIfEmpty(string? value) =>
            string.IsNullOrWhiteSpace(value) || value.Trim() == "string"
                ? null
                : value.Trim();
    }
}