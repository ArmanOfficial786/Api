//// Repository/MemberLookUpRepository.cs
//using Dapper;
//using NexgenCosysReport.DbContext;
//using NexgenCosysReport.Dtos.RequestDtos.Common;
//using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore;
//using System.Data;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;

//namespace NexgenCosysReport.Repository.Common
//{
//    public class MemberLookUpRepository : IMemberLookUp
//    {
//        private readonly AppDbContext _context;
//        private const int FIXED_PAGE_SIZE = 10;

//        public MemberLookUpRepository(AppDbContext context)
//        {
//            _context = context;
//        }

//        // -- 1. Paginated + filtered list for the grid -------------------------
//        public async Task<Pagination<MemberLookUpDtos>> GetMemberListAsync(
//            MemberLookUpRequest request,
//            long userId)
//        {
//            var connectionString = _context.Database.GetConnectionString();
//            using var connection = new SqlConnection(connectionString);

//            var parameters = new DynamicParameters();
//            parameters.Add("@SqlUserId", userId);
//            parameters.Add("@PageNumber", request.Page);
//            parameters.Add("@PageSize", FIXED_PAGE_SIZE);
//            parameters.Add("@MemberId", NullIfEmpty(request.MemberId));
//            parameters.Add("@MemberName", NullIfEmpty(request.MemberName));
//            parameters.Add("@GroupName", NullIfEmpty(request.GroupName));
//            parameters.Add("@GroupCode", NullIfEmpty(request.GroupCode));
//            parameters.Add("@CenterName", NullIfEmpty(request.CenterName));
//            parameters.Add("@CenterCode", NullIfEmpty(request.CenterCode));
//            parameters.Add("@Gender", NullIfEmpty(request.Gender));
//            parameters.Add("@MobileNo", NullIfEmpty(request.MobileNo));
//            parameters.Add("@OfficeName", NullIfEmpty(request.OfficeName));
//            parameters.Add("@SortColumn", request.SortColumn);
//            parameters.Add("@SortDirection", request.SortDirection);

//            var rawItems = (await connection.QueryAsync<MemberLookUpDtos>(
//                "sp_4_11_GetMemberRegistrationDetailByRoleForUC",
//                parameters,
//                commandType: CommandType.StoredProcedure
//            )).ToList();

//            if (!rawItems.Any())
//            {
//                return new Pagination<MemberLookUpDtos>
//                {
//                    Items = new List<MemberLookUpDtos>(),
//                    totalRecord = 0,
//                    currentPage = request.Page,
//                    pageSize = FIXED_PAGE_SIZE,
//                    totalPages = 0
//                };
//            }

//            var first = rawItems.First();

//            return new Pagination<MemberLookUpDtos>
//            {
//                Items = rawItems,
//                totalRecord = first.TotalCount,
//                currentPage = first.CurrentPage > 0 ? first.CurrentPage : request.Page,
//                pageSize = FIXED_PAGE_SIZE,
//                totalPages = first.TotalPages
//            };
//        }

//        // -- 2. Single member selected by user clicking "Sel" ------------------
//        public async Task<MemberSelectedDto?> GetSelectedMemberAsync(
//            long memMemberRegistrationId,
//            long userId)
//        {
//            var connectionString = _context.Database.GetConnectionString();
//            using var connection = new SqlConnection(connectionString);

//            // Re-use the same SP but filter down to one record by ID
//            var parameters = new DynamicParameters();
//            parameters.Add("@SqlUserId", userId);
//            parameters.Add("@PageNumber", 1);
//            parameters.Add("@PageSize", 1);
//            parameters.Add("@MemMemberRegistrationId", memMemberRegistrationId);
//            parameters.Add("@MemberId", DBNull.Value);
//            parameters.Add("@MemberName", DBNull.Value);
//            parameters.Add("@GroupName", DBNull.Value);
//            parameters.Add("@CenterName", DBNull.Value);
//            parameters.Add("@Gender", DBNull.Value);
//            parameters.Add("@MobileNo", DBNull.Value);
//            parameters.Add("@OfficeName", DBNull.Value);
//            parameters.Add("@SortColumn", "MemberId");
//            parameters.Add("@SortDirection", "ASC");

//            var result = await connection.QueryFirstOrDefaultAsync<MemberSelectedDto>(
//                "sp_4_11_GetMemberRegistrationDetailByRoleForUC",
//                parameters,
//                commandType: CommandType.StoredProcedure
//            );

//            return result;
//        }

//        // -- Helper ------------------------------------------------------------
//        private static string? NullIfEmpty(string? value) =>
//            string.IsNullOrWhiteSpace(value) || value.Trim() == "string"
//                ? null
//                : value.Trim();
//    }
//}







using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;

namespace NexgenCosysReport.Repository.Common
{
    public class MemberLookUpRepository : IMemberLookUp
    {
        private readonly AppDbContext _context;
        private const int FIXED_PAGE_SIZE = 10;
        private const string SpName = "sp_4_11_GetMemberRegistrationDetailByRoleForUC";

        public MemberLookUpRepository(AppDbContext context)
        {
            _context = context;
        }



        // 1. Member list with searching, sorting and pagination
        public async Task<Pagination<MemberLookUpDtos>> GetMemberListAsync(
            MemberLookUpRequest request,
            long userId)
        {
            var rows = await ExecuteSpAsync(userId);

            var items = rows.Select(x => new MemberLookUpDtos
            {
                MemMemberRegistrationId = x.MemMemberRegistrationId,
                CenterName = x.CenterName,
                CenterCode = x.CollectionCenterShortCode,
                GroupName = x.GroupName,
                GroupCode = x.GroupShortCode,
                OfficeName = x.OfficeName,
                MemberId = x.MemberId,
                MemberName = x.MemberName,
                Gender = x.Gender,
                TemporaryAddress = x.TemporaryAddress,
                MobileNo = x.MobileNo
            });

            items = items
                .Where(x => Matches(x.MemberId, request.MemberId))
                .Where(x => Matches(x.MemberName, request.MemberName))
                .Where(x => Matches(x.GroupName, request.GroupName))
                .Where(x => Matches(x.GroupCode, request.GroupCode))
                .Where(x => Matches(x.CenterName, request.CenterName))
                .Where(x => Matches(x.CenterCode, request.CenterCode))
                .Where(x => Matches(x.Gender, request.Gender))
                .Where(x => Matches(x.MobileNo, request.MobileNo))
                .Where(x => Matches(x.OfficeName, request.OfficeName));

            var filtered = items.ToList();
            var totalRecord = filtered.Count;

            var sorted = ApplySorting(filtered, request.SortColumn, request.SortDirection);

            var currentPage = Math.Max(1, request.Page);

            var pagedItems = sorted
                .Skip((currentPage - 1) * FIXED_PAGE_SIZE)
                .Take(FIXED_PAGE_SIZE)
                .ToList();

            var totalPages = (int)Math.Ceiling((double)totalRecord / FIXED_PAGE_SIZE);

            return new Pagination<MemberLookUpDtos>
            {
                Items = pagedItems,
                totalRecord = totalRecord,
                currentPage = currentPage,
                pageSize = FIXED_PAGE_SIZE,
                totalPages = totalPages
            };
        }

        // 2. Selected member by registration ID (within user's authorized records)
        public async Task<MemberSelectedDto?> GetSelectedMemberAsync(
            long memMemberRegistrationId,
            long userId)
        {
            var rows = await ExecuteSpAsync(userId);

            return rows
                .Where(x => x.MemMemberRegistrationId == memMemberRegistrationId)
                .Select(x => new MemberSelectedDto
                {
                    MemMemberRegistrationId = x.MemMemberRegistrationId,
                    MemberId = x.MemberId,
                    MemberName = x.MemberName
                })
                .FirstOrDefault();
        }

        // 3. SP execution
        private async Task<List<MemberSpRow>> ExecuteSpAsync(long userId)
        {
            var connectionString = _context.Database.GetConnectionString();

            using var connection = new SqlConnection(connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@SqlUserId", userId, DbType.Int64);

            var result = await connection.QueryAsync<MemberSpRow>(
                SpName,
                parameters,
                commandType: CommandType.StoredProcedure);

            return result.ToList();
        }

        // 4. Search helper
        private static bool Matches(string? source, string? search)
        {
            if (string.IsNullOrWhiteSpace(search) ||
                search.Trim().Equals("string", StringComparison.OrdinalIgnoreCase))
                return true;

            return source?.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase) == true;
        }

        // 5. Sorting helper
        private static IEnumerable<MemberLookUpDtos> ApplySorting(
            IEnumerable<MemberLookUpDtos> items,
            string? sortColumn,
            string? sortDirection)
        {
            bool descending = string.Equals(sortDirection, "DESC", StringComparison.OrdinalIgnoreCase);

            return (sortColumn ?? "MemberId").ToLowerInvariant() switch
            {
                "membername" => descending ? items.OrderByDescending(x => x.MemberName) : items.OrderBy(x => x.MemberName),
                "groupname" => descending ? items.OrderByDescending(x => x.GroupName) : items.OrderBy(x => x.GroupName),
                "groupcode" => descending ? items.OrderByDescending(x => x.GroupCode) : items.OrderBy(x => x.GroupCode),
                "centername" => descending ? items.OrderByDescending(x => x.CenterName) : items.OrderBy(x => x.CenterName),
                "centercode" => descending ? items.OrderByDescending(x => x.CenterCode) : items.OrderBy(x => x.CenterCode),
                "gender" => descending ? items.OrderByDescending(x => x.Gender) : items.OrderBy(x => x.Gender),
                "mobileno" => descending ? items.OrderByDescending(x => x.MobileNo) : items.OrderBy(x => x.MobileNo),
                "officename" => descending ? items.OrderByDescending(x => x.OfficeName) : items.OrderBy(x => x.OfficeName),
                _ => descending ? items.OrderByDescending(x => x.MemberId) : items.OrderBy(x => x.MemberId)
            };
        }
    }
}