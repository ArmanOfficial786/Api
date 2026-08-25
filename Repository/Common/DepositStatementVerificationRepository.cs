//// Repositories/Implementations/AccountOperation/DepositStatementVerifyRepository.cs
//using Dapper;
//using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore;
//using NexgenCosysReport.Dtos.RequestDtos.Common;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using System.Data;

//namespace NexgenCosysReport.Repository.Common
//{
//    public class DepositStatementVerifyRepository : IDepositStatementVerification
//    {
//        private readonly AppDbContext _context;
//        private readonly IDateConverterService _dateConverter;
//        private readonly ILogger<DepositStatementVerifyRepository> _logger;

//        public DepositStatementVerifyRepository(
//            AppDbContext context,
//            IDateConverterService dateConverter,
//            ILogger<DepositStatementVerifyRepository> logger)
//        {
//            _context = context;
//            _dateConverter = dateConverter;
//            _logger = logger;
//        }

//        private SqlConnection GetOpenConnection() =>
//            new SqlConnection(_context.Database.GetConnectionString());

//        public async Task<List<DepositStatementVerificationDto>> GetVerificationHistory(long mamAccountOpeningId)
//        {
//            using var connection = GetOpenConnection();
//            await connection.OpenAsync();

//            const string sql = @"
//                SELECT 
//                    v.MamDepositStatementVerificationId,
//                    v.VerifiedFromDateOnBs,
//                    v.VerifiedToDateOnBs,
//                    v.CreatedOn,
//                    v.VerifiedDateOnBs,
//                    u.FullName AS VerifiedBy
//                FROM MamDepositStatementVerification v
//                LEFT JOIN UsmUser u ON v.CreatedBy = u.UsmUserId
//                WHERE v.MamAccountOpeningId = @MamAccountOpeningId
//                ORDER BY v.CreatedOn DESC";

//            return (await connection.QueryAsync<DepositStatementVerificationDto>(
//                sql, new { MamAccountOpeningId = mamAccountOpeningId })).AsList();
//        }

//        public async Task<DateTime?> GetMaxTransactionDateForVerify(long mamAccountOpeningId)
//        {
//            using var connection = GetOpenConnection();
//            await connection.OpenAsync();

//            var parameters = new DynamicParameters();
//            parameters.Add("@MamAccountOpeningId", mamAccountOpeningId);

//            return await connection.QueryFirstOrDefaultAsync<DateTime?>(
//                "sp_5_43_GetMaxDepositStatementDateForVerify",
//                parameters,
//                commandType: CommandType.StoredProcedure);
//        }

//        public async Task<(bool Success, string Message)> CreateVerification(
//            DepositStatementVerifyRequestDto request,
//            long userId)
//        {
//            var verifiedTillEnglish = await _dateConverter.NepaliToEnglishAsync(request.VerifiedToDateOnBs);

//            if (verifiedTillEnglish > DateTime.Now)
//            {
//                return (false, "Statement verification date cannot be greater than current date.");
//            }

//            using var connection = GetOpenConnection();
//            await connection.OpenAsync();

//            // Check if already verified
//            var isVerified = await IsAlreadyVerified(request.MamAccountOpeningId, request.VerifiedToDateOnBs);
//            if (isVerified)
//            {
//                return (false, "Statement verification is already up to date.");
//            }

//            // Check verification history - matches WebForm logic
//            var history = await GetVerificationHistory(request.MamAccountOpeningId);
//            if (history.Any())
//            {
//                var lastToBs = history.OrderByDescending(h => h.CreatedOn).First().VerifiedToDateOnBs;
//                var lastToEnglish = await _dateConverter.NepaliToEnglishAsync(lastToBs ?? string.Empty);

//                if (verifiedTillEnglish < lastToEnglish)
//                {
//                    return (false, "Statement verification date is less than last verification date.");
//                }

//                // Check if there are new transactions to verify - matches WebForm's maxdatfromaco check
//                var maxTransactionDate = await GetMaxTransactionDateForVerify(request.MamAccountOpeningId);
//                var lastCreatedOn = history.Max(h => h.CreatedOn);
//                if (maxTransactionDate.HasValue && maxTransactionDate.Value <= lastCreatedOn)
//                {
//                    return (false, "Statement verification is up to date.");
//                }
//            }

//            var verifiedDateOnBs = await _dateConverter.EnglishToNepaliAsync(DateTime.Now);

//            const string insertSql = @"
//                INSERT INTO MamDepositStatementVerification
//                    (MamAccountOpeningId, VerifiedFromDateOnBs, VerifiedToDateOnBs,
//                     VerifiedDateOn, VerifiedDateOnBs, CreatedBy, CreatedOn)
//                VALUES
//                    (@MamAccountOpeningId, @VerifiedFromDateOnBs, @VerifiedToDateOnBs,
//                     @VerifiedDateOn, @VerifiedDateOnBs, @CreatedBy, @CreatedOn)";

//            await connection.ExecuteAsync(insertSql, new
//            {
//                request.MamAccountOpeningId,
//                request.VerifiedFromDateOnBs,
//                request.VerifiedToDateOnBs,
//                VerifiedDateOn = DateTime.Now.Date,
//                VerifiedDateOnBs = verifiedDateOnBs,
//                CreatedBy = userId,
//                CreatedOn = DateTime.Now
//            });

//            return (true, "Deposit Statement Verification Updated Successfully");
//        }

//        public async Task<VerificationStatusDto> GetVerificationStatus(long mamAccountOpeningId)
//        {
//            var history = await GetVerificationHistory(mamAccountOpeningId);
//            if (!history.Any())
//            {
//                return new VerificationStatusDto
//                {
//                    HasVerification = false,
//                    Message = "No Verification Till Date"
//                };
//            }

//            var latest = history.OrderByDescending(h => h.CreatedOn).First();

//            return new VerificationStatusDto
//            {
//                HasVerification = true,
//                VerifiedTillBs = latest.VerifiedToDateOnBs,
//                VerifiedDateBs = latest.VerifiedDateBs,
//                VerifiedBy = latest.VerifiedBy,
//                Message = $"Passbook Verified Till : {latest.VerifiedToDateOnBs}"
//            };
//        }

//        public async Task<bool> IsAlreadyVerified(long mamAccountOpeningId, string verifiedTillDateBs)
//        {
//            using var connection = GetOpenConnection();
//            await connection.OpenAsync();

//            const string sql = @"
//                SELECT COUNT(1)
//                FROM MamDepositStatementVerification
//                WHERE MamAccountOpeningId = @MamAccountOpeningId
//                AND VerifiedToDateOnBs >= @VerifiedTillDateBs";

//            var count = await connection.ExecuteScalarAsync<int>(
//                sql,
//                new { MamAccountOpeningId = mamAccountOpeningId, VerifiedTillDateBs = verifiedTillDateBs });

//            return count > 0;
//        }

//        public async Task<long> GetAccountIdByAccountNo(string accountNo)
//        {
//            using var connection = GetOpenConnection();
//            await connection.OpenAsync();

//            const string sql = @"
//                SELECT TOP 1 MamAccountOpeningId
//                FROM MamAccountOpening
//                WHERE AccountNo = @AccountNo AND IsDeleted = 0
//                ORDER BY MamAccountOpeningId DESC";

//            return await connection.ExecuteScalarAsync<long>(
//                sql,
//                new { AccountNo = accountNo });
//        }

//        public async Task<decimal?> GetInterestPayable(long accountId)
//        {
//            using var connection = GetOpenConnection();
//            await connection.OpenAsync();

//            const string sql = @"
//                SELECT InterestPayable
//                FROM MamAccountOpening
//                WHERE MamAccountOpeningId = @AccountId";

//            return await connection.ExecuteScalarAsync<decimal?>(
//                sql,
//                new { AccountId = accountId });
//        }

//        public async Task<dynamic?> GetAccountInfo(string accountNo)
//        {
//            using var connection = GetOpenConnection();
//            await connection.OpenAsync();

//            const string sql = @"
//                SELECT 
//                    MamAccountOpeningId,
//                    AccountNo,
//                    UsmOfficeId,
//                    MamAccountStatusId,
//                    InterestPayable
//                FROM MamAccountOpening
//                WHERE AccountNo = @AccountNo AND IsDeleted = 0";

//            return await connection.QueryFirstOrDefaultAsync(
//                sql,
//                new { AccountNo = accountNo });
//        }
//    }
//}






// Repositories/Implementations/AccountOperation/DepositStatementVerifyRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;

namespace NexgenCosysReport.Repository.Common
{
    public class DepositStatementVerifyRepository : IDepositStatementVerification
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<DepositStatementVerifyRepository> _logger;

        public DepositStatementVerifyRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<DepositStatementVerifyRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        private SqlConnection GetOpenConnection() =>
            new SqlConnection(_context.Database.GetConnectionString());

        // ---- Reads ----

        public async Task<List<DepositStatementVerificationDto>> GetVerificationHistory(long mamAccountOpeningId)
        {
            using var connection = GetOpenConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT 
                    v.MamDepositStatementVerificationId,
                    v.VerifiedFromDateOnBs,
                    v.VerifiedToDateOnBs,
                    v.CreatedOn,
                    v.VerifiedDateOnBs AS VerifiedDateBs,
                    u.FullName AS VerifiedBy
                FROM MamDepositStatementVerification v
                LEFT JOIN UsmUser u ON v.CreatedBy = u.UsmUserId
                WHERE v.MamAccountOpeningId = @MamAccountOpeningId
                ORDER BY v.CreatedOn DESC";

            return (await connection.QueryAsync<DepositStatementVerificationDto>(
                sql, new { MamAccountOpeningId = mamAccountOpeningId })).AsList();
        }

        /// <summary>
        /// Mirrors CMamDepositStatementVerification.GetByAccountNo — joins on AccountNo
        /// rather than filtering by MamAccountOpeningId. This is what the WebForm's
        /// verify flow actually queries, and what GetVerificationStatus uses internally.
        /// </summary>
        private async Task<List<DepositStatementVerificationDto>> GetVerificationHistoryByAccountNo(string accountNo)
        {
            using var connection = GetOpenConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT 
                    v.MamDepositStatementVerificationId,
                    v.VerifiedFromDateOnBs,
                    v.VerifiedToDateOnBs,
                    v.CreatedOn,
                    v.VerifiedDateOnBs AS VerifiedDateBs,
                    u.FullName AS VerifiedBy
                FROM MamDepositStatementVerification v
                INNER JOIN MamAccountOpening a ON a.MamAccountOpeningId = v.MamAccountOpeningId
                LEFT JOIN UsmUser u ON v.CreatedBy = u.UsmUserId
                WHERE a.AccountNo = @AccountNo
                ORDER BY v.CreatedOn DESC";

            return (await connection.QueryAsync<DepositStatementVerificationDto>(
                sql, new { AccountNo = accountNo })).AsList();
        }

        public async Task<DateTime?> GetMaxTransactionDateForVerify(long mamAccountOpeningId)
        {
            using var connection = GetOpenConnection();
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@MamAccountOpeningId", mamAccountOpeningId);

            return await connection.QueryFirstOrDefaultAsync<DateTime?>(
                "sp_5_43_GetMaxDepositStatementDateForVerify",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<dynamic?> GetAccountInfo(string accountNo)
        {
            using var connection = GetOpenConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT 
                    MamAccountOpeningId,
                    AccountNo,
                    UsmOfficeId,
                    MamAccountStatusId,
                    InterestPayable
                FROM MamAccountOpening
                WHERE AccountNo = @AccountNo AND IsDeleted = 0";

            return await connection.QueryFirstOrDefaultAsync(
                sql, new { AccountNo = accountNo });
        }

        public async Task<dynamic?> GetAccountInfoById(long mamAccountOpeningId)
        {
            using var connection = GetOpenConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT 
                    MamAccountOpeningId,
                    AccountNo,
                    UsmOfficeId,
                    MamAccountStatusId,
                    InterestPayable
                FROM MamAccountOpening
                WHERE MamAccountOpeningId = @MamAccountOpeningId AND IsDeleted = 0";

            return await connection.QueryFirstOrDefaultAsync(
                sql, new { MamAccountOpeningId = mamAccountOpeningId });
        }

        public async Task<long> GetAccountIdByAccountNo(string accountNo)
        {
            using var connection = GetOpenConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT TOP 1 MamAccountOpeningId
                FROM MamAccountOpening
                WHERE AccountNo = @AccountNo AND IsDeleted = 0
                ORDER BY MamAccountOpeningId DESC";

            return await connection.ExecuteScalarAsync<long>(
                sql, new { AccountNo = accountNo });
        }

        public async Task<decimal?> GetInterestPayable(long accountId)
        {
            using var connection = GetOpenConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT InterestPayable
                FROM MamAccountOpening
                WHERE MamAccountOpeningId = @AccountId";

            return await connection.ExecuteScalarAsync<decimal?>(
                sql, new { AccountId = accountId });
        }

        // ---- Write ----

        /// <summary>
        /// Replicates btnStatementVerify_Click's validation order exactly:
        ///  1. Future-date check
        ///  2. verifiedTill &lt; Max(history.VerifiedToDateOnBs) -> "less than last verification date"
        ///  3. maxTransactionDate &lt;= Max(history.CreatedOn) -> "up to date"
        ///  4. Insert
        /// </summary>
        public async Task<(bool Success, string Message)> CreateVerification(
            DepositStatementVerifyRequestDto request,
            long userId)
        {
            var verifiedTillEnglish = await _dateConverter.NepaliToEnglishAsync(request.VerifiedToDateOnBs);

            if (verifiedTillEnglish > DateTime.Now)
            {
                return (false, "Statement verification date cannot be greater than current date.");
            }

            var history = await GetVerificationHistoryByAccountNo(request.AccountNo);

            if (history.Any())
            {
                var lastToBsRaw = history.Max(h => h.VerifiedToDateOnBs) ?? string.Empty;
                var lastToEnglish = await _dateConverter.NepaliToEnglishAsync(lastToBsRaw);

                if (verifiedTillEnglish < lastToEnglish)
                {
                    return (false, "Statement verification date is less than last verification date");
                }

                var maxTransactionDate = await GetMaxTransactionDateForVerify(request.MamAccountOpeningId);
                var lastCreatedOn = history.Max(h => h.CreatedOn);
                if (maxTransactionDate.HasValue && maxTransactionDate.Value <= lastCreatedOn)
                {
                    return (false, "Statement verification is upto dated.");
                }
            }

            var verifiedDateOnBs = await _dateConverter.EnglishToNepaliAsync(DateTime.Now);

            const string insertSql = @"
                INSERT INTO MamDepositStatementVerification
                    (MamAccountOpeningId, VerifiedFromDateOnBs, VerifiedToDateOnBs,
                     VerifiedDateOn, VerifiedDateOnBs, CreatedBy, CreatedOn)
                VALUES
                    (@MamAccountOpeningId, @VerifiedFromDateOnBs, @VerifiedToDateOnBs,
                     @VerifiedDateOn, @VerifiedDateOnBs, @CreatedBy, @CreatedOn)";

            using var connection = GetOpenConnection();
            await connection.OpenAsync();

            await connection.ExecuteAsync(insertSql, new
            {
                request.MamAccountOpeningId,
                request.VerifiedFromDateOnBs,
                request.VerifiedToDateOnBs,
                VerifiedDateOn = DateTime.Now.Date,
                VerifiedDateOnBs = verifiedDateOnBs,
                CreatedBy = userId,
                CreatedOn = DateTime.Now
            });

            return (true, "Deposit Statement Verification Updated Successfully");
        }

        /// <summary>
        /// Public contract is by mamAccountOpeningId (matches the generated client route
        /// Status/{mamAccountOpeningId}), but resolves AccountNo first and scopes the actual
        /// lookup by AccountNo — replicating the WebForm's GetByAccountNo-based refresh.
        /// Returns null if the account opening doesn't exist (controller returns 404).
        /// </summary>
        public async Task<VerificationStatusDto?> GetVerificationStatus(long mamAccountOpeningId)
        {
            var accountInfo = await GetAccountInfoById(mamAccountOpeningId);
            if (accountInfo == null)
            {
                return null;
            }

            string accountNo = accountInfo.AccountNo;
            var history = await GetVerificationHistoryByAccountNo(accountNo);

            if (!history.Any())
            {
                return new VerificationStatusDto
                {
                    HasVerification = false,
                    Message = "No Verification Till Date"
                };
            }

            // WebForm: depositStatementVerification.Max(p => p.VerifiedToDateOnBs) — raw string max.
            var maxVerifiedToBs = history.Max(h => h.VerifiedToDateOnBs);
            var latestRecord = history
                .Where(h => h.VerifiedToDateOnBs == maxVerifiedToBs)
                .OrderByDescending(h => h.CreatedOn)
                .First();

            return new VerificationStatusDto
            {
                HasVerification = true,
                VerifiedTillBs = maxVerifiedToBs,
                VerifiedDateBs = latestRecord.VerifiedDateBs,
                VerifiedBy = latestRecord.VerifiedBy,
                Message = $"Passbook Verified Till : {maxVerifiedToBs}"
            };
        }
    }
}