// Repository/Common/LoanAccountLookUpRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;

namespace NexgenCosysReport.Repository.Common
{
    public class LoanAccountLookUpRepository : ILoanAccountLookUp
    {
        private readonly AppDbContext _context;
        private const int FIXED_PAGE_SIZE = 10;

        // Whitelist maps to the CTE aliases (final SELECT reads from the CTE)
        private static readonly Dictionary<string, string> AllowedSortColumns =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "AccountNo",   "AccountNo" },
                { "MemberId",    "MemberId" },
                { "MemberName",  "MemberName" },
                { "LoanStatus",  "LoanStatus" },
                { "LoanIssueId", "LoanIssueId" }
            };

        public LoanAccountLookUpRepository(AppDbContext context)
        {
            _context = context;
        }

        // -- 1. Paginated + filtered list for the grid -------------------------
        public async Task<Pagination<LoanAccountLookUpDtos>> GetLoanAccountListAsync(
            LoanAccountLookUpRequest request,
            long userId)
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);

            var page = Math.Max(1, request.Page);

            var sortColumn = AllowedSortColumns.TryGetValue(request.SortColumn ?? "", out var col)
                                ? col
                                : "AccountNo";
            var sortDirection = string.Equals(request.SortDirection, "ASC", StringComparison.OrdinalIgnoreCase)
                                ? "ASC"
                                : "DESC";

            // MemberName built without CONCAT_WS (works on all SQL Server versions).
            // OFFSET/FETCH requires SQL Server 2012+.
            var sql = $@"
;WITH BaseData AS
(
    SELECT
        li.LmtLoanIssueId   AS LoanIssueId,
        li.LoanAccountNo    AS AccountNo,
        mr.MemberId         AS MemberId,
        LTRIM(RTRIM(REPLACE(
            ISNULL(mr.FirstName,'')  + ' ' +
            ISNULL(mr.MiddleName,'') + ' ' +
            ISNULL(mr.LastName,''),
            '  ', ' ')))    AS MemberName,
        ls.LoanStatus       AS LoanStatus
    FROM LmtLoanIssue li
    INNER JOIN MemMemberRegistration mr
        ON li.MemMemberRegistrationId = mr.MemMemberRegistrationId
    LEFT JOIN LmtLoanStatus ls
        ON li.LmtLoanStatusId = ls.LmtLoanStatusId
    WHERE li.IsActive        = 1
      AND li.IsVerified      = 1
      AND li.LoanODorNormal  = 'O'
      AND li.LmtLoanStatusId <> 2
      AND li.TransStatus     <> 'U'
      AND EXISTS
          (
              SELECT 1
              FROM UsmRelationUserToOffice us
              WHERE us.UsmOfficeId = li.UsmOfficeId
                AND us.UsmUserId   = @SqlUserId
          )
),
Filtered AS
(
    SELECT *
    FROM BaseData
    WHERE (@LoanIssueId IS NULL OR LoanIssueId = @LoanIssueId)
      AND (@AccountNo   IS NULL OR AccountNo  LIKE '%' + @AccountNo  + '%')
      AND (@MemberId    IS NULL OR MemberId   LIKE '%' + @MemberId   + '%')
      AND (@MemberName  IS NULL OR MemberName LIKE '%' + @MemberName + '%')
      AND (@LoanStatus  IS NULL OR LoanStatus LIKE '%' + @LoanStatus + '%')
)
SELECT
    LoanIssueId,
    AccountNo,
    MemberId,
    MemberName,
    LoanStatus,
    COUNT(*) OVER() AS TotalCount,
    @PageNumber     AS CurrentPage,
    CASE WHEN COUNT(*) OVER() = 0 THEN 0
         ELSE CEILING(CAST(COUNT(*) OVER() AS DECIMAL(18,2)) / @PageSize)
    END             AS TotalPages
FROM Filtered
ORDER BY {sortColumn} {sortDirection}, LoanIssueId DESC
OFFSET (@PageNumber - 1) * @PageSize ROWS
FETCH NEXT @PageSize ROWS ONLY;";

            var parameters = new DynamicParameters();
            parameters.Add("@SqlUserId", userId, DbType.Int64);
            parameters.Add("@PageNumber", page, DbType.Int32);
            parameters.Add("@PageSize", FIXED_PAGE_SIZE, DbType.Int32);
            parameters.Add("@LoanIssueId", request.LoanIssueId, DbType.Int64);
            parameters.Add("@AccountNo", NullIfEmpty(request.AccountNo), DbType.String);
            parameters.Add("@MemberId", NullIfEmpty(request.MemberId), DbType.String);
            parameters.Add("@MemberName", NullIfEmpty(request.MemberName), DbType.String);
            parameters.Add("@LoanStatus", NullIfEmpty(request.LoanStatus), DbType.String);

            var rawItems = (await connection.QueryAsync<LoanAccountLookUpDtos>(sql, parameters)).ToList();

            if (rawItems.Count == 0)
            {
                return new Pagination<LoanAccountLookUpDtos>
                {
                    Items = new List<LoanAccountLookUpDtos>(),
                    totalRecord = 0,
                    currentPage = page,
                    pageSize = FIXED_PAGE_SIZE,
                    totalPages = 0
                };
            }

            var first = rawItems[0];

            return new Pagination<LoanAccountLookUpDtos>
            {
                Items = rawItems,
                totalRecord = first.TotalCount,
                currentPage = first.CurrentPage > 0 ? first.CurrentPage : page,
                pageSize = FIXED_PAGE_SIZE,
                totalPages = first.TotalPages
            };
        }

        // -- 2. Single account selected by user clicking "Sel" -----------------
        public async Task<LoanAccountSelectedDto?> GetSelectedLoanAccountAsync(
            long loanIssueId,
            long userId)
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);

            const string sql = @"
SELECT TOP 1
    li.LmtLoanIssueId   AS LoanIssueId,
    li.LoanAccountNo    AS AccountNo,
    mr.MemberId         AS MemberId,
    LTRIM(RTRIM(REPLACE(
        ISNULL(mr.FirstName,'')  + ' ' +
        ISNULL(mr.MiddleName,'') + ' ' +
        ISNULL(mr.LastName,''),
        '  ', ' ')))    AS MemberName,
    ls.LoanStatus       AS LoanStatus
FROM LmtLoanIssue li
INNER JOIN MemMemberRegistration mr
    ON li.MemMemberRegistrationId = mr.MemMemberRegistrationId
LEFT JOIN LmtLoanStatus ls
    ON li.LmtLoanStatusId = ls.LmtLoanStatusId
WHERE li.IsActive        = 1
  AND li.IsVerified      = 1
  AND li.LoanODorNormal  = 'O'
  AND li.LmtLoanStatusId <> 2
  AND li.TransStatus     <> 'U'
  AND li.LmtLoanIssueId  = @LoanIssueId
  AND EXISTS
      (
          SELECT 1
          FROM UsmRelationUserToOffice us
          WHERE us.UsmOfficeId = li.UsmOfficeId
            AND us.UsmUserId   = @SqlUserId
      );";

            var parameters = new DynamicParameters();
            parameters.Add("@SqlUserId", userId, DbType.Int64);
            parameters.Add("@LoanIssueId", loanIssueId, DbType.Int64);

            return await connection.QueryFirstOrDefaultAsync<LoanAccountSelectedDto>(sql, parameters);
        }

        // -- Helper ------------------------------------------------------------
        private static string? NullIfEmpty(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}