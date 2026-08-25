using Dapper;
using NexgenCosysReport.DbContext;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount;
using System.Data;

namespace NexgenCosysReport.Repository.MemberAccount
{
    public class SavingAcWiseBalanceRepository : ISavingAcWiseBalance
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;

        public SavingAcWiseBalanceRepository(
            AppDbContext context,
            IDateConverterService dateConverter)
        {
            _context = context;
            _dateConverter = dateConverter;
        }

        // ================================================================
        //  Public entry point
        // ================================================================

        public async Task<List<SavingAcWiseBalanceResponse>> GetSavingAcWiseBalanceAsync(
            SavingAcWiseBalanceRequest request)
        {
            // Build the three SP filter strings (mirrors legacy BLL method)
            string sqlFilterExpa = await BuildSqlFilterExpa(request);
            string sqlFilterExpt = await BuildSqlFilterExpt(request);
            string sqlFilterExpOrderBy = BuildSqlFilterExpOrderBy(request);

            var connectionString = _context.Database.GetConnectionString();
            await using var connection = new SqlConnection(connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExpa", sqlFilterExpa);
            parameters.Add("@SqlFilterExpt", sqlFilterExpt);
            parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy);

            var result = await connection.QueryAsync<SavingAcWiseBalanceResponse>(
                "sp_5_43_GetSavingACWiseBalance",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120
            );

            return result.ToList();
        }

        // ================================================================
        //  @SqlFilterExpa  — applied to the no-transaction (account-only)
        //  arm of the SP's WHERE clause:
        //    OR (t.AcoTransactionId IS NULL <@SqlFilterExpa>)
        //
        //  Filters: DepositType, Collector, Office, MemberGroup, CollectionCenter
        //  NOTE: TillDate is NOT applied here (no transaction date to compare)
        // ================================================================
        private async Task<string> BuildSqlFilterExpa(SavingAcWiseBalanceRequest request)
        {
            var filter = string.Empty;

            // Deposit type
            if (request.DepositId != -1)
                filter += $" AND a.SycDepositTypeId = {request.DepositId}";

            // Collector
            if (request.CollectorId != -1)
                filter += $" AND a.HurCollectorId = {request.CollectorId}";

            // Branch / office  (comma-separated list)
            if (!string.IsNullOrWhiteSpace(request.BranchSelected) &&
                request.BranchSelected != "-1")
            {
                filter += $" AND a.UsmOfficeId IN ({request.BranchSelected})";
            }

            // Member group
            if (request.MemberGroupId != -1)
                filter += $" AND m.SycMemberGroupId = {request.MemberGroupId}";

            // Collection center  (comma-separated list)
            if (
                request.CollectionCenterId != -1)
            {
                filter += $" AND mg.SycCollectionCenterId IN ({request.CollectionCenterId})";
            }

            // TillDate is intentionally omitted for @SqlFilterExpa
            // (accounts with no transactions should still appear)
            await Task.CompletedTask; // keeps signature async for consistency
            return filter;
        }

        // ================================================================
        //  @SqlFilterExpt  — applied to the transaction arm of the WHERE:
        //    t.IsActive = 1 AND att.AcoTransactionTypeId IN (...) <@SqlFilterExpt>
        //
        //  Filters: DepositType, Collector, TillDate (<=), Office,
        //           MemberGroup, CollectionCenter
        // ================================================================
        private async Task<string> BuildSqlFilterExpt(SavingAcWiseBalanceRequest request)
        {
            var filter = string.Empty;

            // Deposit type
            if (request.DepositId != -1)
                filter += $" AND a.SycDepositTypeId = {request.DepositId}";

            // Collector
            if (request.CollectorId != -1)
                filter += $" AND a.HurCollectorId = {request.CollectorId}";

            // Till date — convert BS ? AD then apply as upper bound
            if (!string.IsNullOrWhiteSpace(request.TillDate) &&
                request.TillDate != "-1")
            {
                string tillDateAd = await _dateConverter.BsToAdStringAsync(request.TillDate);
                if (!string.IsNullOrEmpty(tillDateAd))
                    filter += $" AND t.TransactionOn <= '{tillDateAd}'";
            }

            // Branch / office
            if (!string.IsNullOrWhiteSpace(request.BranchSelected) &&
                request.BranchSelected != "-1")
            {
                filter += $" AND a.UsmOfficeId IN ({request.BranchSelected})";
            }

            // Member group
            if (request.MemberGroupId != -1)
                filter += $" AND m.SycMemberGroupId = {request.MemberGroupId}";

            // Collection center
            if (
                request.CollectionCenterId != -1)
            {
                filter += $" AND mg.SycCollectionCenterId IN ({request.CollectionCenterId})";
            }

            return filter;
        }

        // ================================================================
        //  @SqlFilterExpOrderBy — injected AFTER the GROUP BY in #FINALTEMP.
        //  Handles the status WHERE clause AND the ORDER BY together,
        //  exactly as the legacy BLL does.
        //
        //  Status mapping (MamAccountStatusId):
        //    1 ? IN (1,4,5)   [Opened + Suspended + Disabled]
        //    2 ? IN (2)       [Closed]
        //    3 ? Balance > 0  [With Balance]
        //    4 ? IN (4)       [Suspended]
        //    5 ? IN (5)       [Disabled]
        //   -1 ? IN (1,2,4,5) [All]
        //
        //  Order-by mapping (exact legacy UI label strings):
        //    "Member Name"   ? ORDER BY MemberName
        //    "Member Id"     ? ORDER BY substring(MemberId, …), MemberId
        //    "Account No"    ? ORDER BY substring(AccountNo, …), AccountNo
        //    "Interest Rate" ? ORDER BY InterestRate DESC
        //    "Deposit"       ? ORDER BY Deposit DESC
        //    "Withdrawl"     ? ORDER BY Withdraw DESC  (note legacy typo kept)
        //    "Balance"       ? ORDER BY Balance DESC
        //    default         ? ORDER BY MemberName
        // ================================================================
        private static string BuildSqlFilterExpOrderBy(SavingAcWiseBalanceRequest request)
        {
            // ---- Status WHERE ----
            string statusClause = request.Status switch
            {
                "1" => "WHERE MamAccountStatusId IN (1,4,5) ",
                "2" => "WHERE MamAccountStatusId IN (2) ",
                "3" => "WHERE Balance > 0 ",
                "4" => "WHERE MamAccountStatusId IN (4) ",
                "5" => "WHERE MamAccountStatusId IN (5) ",
                _ => "WHERE MamAccountStatusId IN (1,2,4,5) "   // -1 / all
            };

            // ---- ORDER BY ----
            string orderByClause = request.OrderBy switch
            {
                "Member Name" =>
                    "ORDER BY MemberName",

                "Member Id" =>
                    // Strips the branch-prefix before the last '-' then sorts numerically
                    "ORDER BY substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId",

                "Account No" =>
                    "ORDER BY substring(AccountNo, 1,(len(AccountNo)-charindex('-', AccountNo))-1), AccountNo",

                "Interest Rate" =>
                    "ORDER BY InterestRate DESC",

                "Deposit" =>
                    "ORDER BY Deposit DESC",

                "Withdrawl" =>          // legacy label kept (note the typo)
                    "ORDER BY Withdraw DESC",

                "Balance" =>
                    "ORDER BY Balance DESC",

                _ =>
                    "ORDER BY MemberName"
            };

            return statusClause + orderByClause;
        }
    }

}

