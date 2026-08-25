// Repository/AccountOperation/MemberSummaryRepository.cs
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
    public class MemberSummaryRepository : IMemberSummary
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<MemberSummaryRepository> _logger;

        public MemberSummaryRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<MemberSummaryRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<MemberSummaryData> GetMemberSummaryReport(MemberSummaryRequest request)
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // --- Convert Nepali date to English ---
            string tillDateStr = string.Empty;
            if (!string.IsNullOrEmpty(request.TillDate) && request.TillDate != "-1")
            {
                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDate);
                tillDateStr = tillDateAd.ToString("MM/dd/yyyy");
            }

            // --- Branch filter ---
            string branchFilter = request.BranchIds;
            if (request.SameCompanyName)
            {
                branchFilter = "-1";
            }
            else if (string.IsNullOrEmpty(request.BranchIds) || request.BranchIds == "-1")
            {
                branchFilter = "-1";
            }

            // --- Collection Center filter ---
            string collectionCenterFilter = request.CollectionCenterId ?? "-1";
            if (string.IsNullOrEmpty(collectionCenterFilter) || collectionCenterFilter == "-1")
            {
                collectionCenterFilter = "-1";
            }

            // --- Member Group filter ---
            string memberGroupFilter = request.MemberGroupId ?? "-1";
            if (string.IsNullOrEmpty(memberGroupFilter) || memberGroupFilter == "-1")
            {
                memberGroupFilter = "-1";
            }

            // --- Order By ---
            string orderByClause = MapOrderBy(request.OrderBy);

            _logger.LogInformation(
                "MemberSummary SP params -> TillDate: {TillDate}, BranchFilter: {BranchFilter}, " +
                "CollectionCenterFilter: {CollectionCenterFilter}, MemberGroupFilter: {MemberGroupFilter}, " +
                "OrderBy: {OrderBy}",
                tillDateStr, branchFilter, collectionCenterFilter, memberGroupFilter, orderByClause);

            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExpTillDate", tillDateStr);
            parameters.Add("@SqlFilterExpOfficeId", branchFilter);
            parameters.Add("@SqlFilterExpCenterId", collectionCenterFilter);
            parameters.Add("@SqlFilterExpGroupId", memberGroupFilter);
            parameters.Add("@SqlFilterExpOrderBy", orderByClause);

            var rawRows = await connection.QueryAsync(
                "sp_5_43_GetMemberSummaryReport",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var list = new List<MemberSummaryRowDto>();
            var savingTotals = new Dictionary<string, decimal>
            {
                { "NormalSaving", 0m },
                { "RecurringSaving", 0m },
                { "FixedSaving", 0m },
                { "TermSaving", 0m },
                { "DoubleDeposit", 0m },
                { "RegularSaving", 0m }
            };

            decimal totalShare = 0m;
            decimal totalSaving = 0m;
            decimal totalLoan = 0m;

            foreach (var r in rawRows)
            {
                var dict = (IDictionary<string, object>)r;
                var row = new MemberSummaryRowDto
                {
                    MemberId = GetString(dict, "MemberId"),
                    MemberName = GetString(dict, "MemberName"),
                    Address = GetString(dict, "Address"),
                    PhoneNumber = GetString(dict, "PhoneNumber"),
                    CollectionCenter = GetString(dict, "CollectionCenter"),
                    MemberGroup = GetString(dict, "MemberGroup"),
                    ShareAmount = GetDecimal(dict, "ShareAmount"),
                    NormalSaving = GetDecimal(dict, "NormalSaving"),
                    RecurringSaving = GetDecimal(dict, "RecurringSaving"),
                    FixedSaving = GetDecimal(dict, "FixedSaving"),
                    TermSaving = GetDecimal(dict, "TermSaving"),
                    DoubleDeposit = GetDecimal(dict, "DoubleDeposit"),
                    RegularSaving = GetDecimal(dict, "RegularSaving"),
                    TotalSaving = GetDecimal(dict, "TotalSaving"),
                    LoanAmount = GetDecimal(dict, "LoanAmount"),
                    TotalBalance = GetDecimal(dict, "TotalBalance")
                };
                list.Add(row);

                totalShare += row.ShareAmount;
                totalSaving += row.TotalSaving;
                totalLoan += row.LoanAmount;

                savingTotals["NormalSaving"] += row.NormalSaving;
                savingTotals["RecurringSaving"] += row.RecurringSaving;
                savingTotals["FixedSaving"] += row.FixedSaving;
                savingTotals["TermSaving"] += row.TermSaving;
                savingTotals["DoubleDeposit"] += row.DoubleDeposit;
                savingTotals["RegularSaving"] += row.RegularSaving;
            }

            _logger.LogInformation("MemberSummaryReport returned {Count} rows", list.Count);

            return new MemberSummaryData
            {
                Rows = list,
                TotalRecords = list.Count,
                TotalShareAmount = totalShare,
                TotalSaving = totalSaving,
                TotalLoan = totalLoan,
                GrandTotal = totalShare + totalSaving + totalLoan,
                SavingTypeTotals = savingTotals
            };
        }

        private string MapOrderBy(string orderBy)
        {
            return orderBy switch
            {
                "Member Id" => "MemberId",
                "Member Name" => "MemberName",
                "Address" => "Address",
                "Share Amount" => "ShareAmount DESC",
                "Normal Saving" => "NormalSaving DESC",
                "Recurring Saving" => "RecurringSaving DESC",
                "Fixed Saving" => "FixedSaving DESC",
                "Term Saving" => "TermSaving DESC",
                "Double Deposit" => "DoubleDeposit DESC",
                "Regular Saving" => "RegularSaving DESC",
                "Total Saving" => "TotalSaving DESC",
                "Loan Amount" => "LoanAmount DESC",
                _ => "MemberId"
            };
        }

        private static string? GetString(IDictionary<string, object> dict, string key) =>
            dict.TryGetValue(key, out var val) && val != DBNull.Value ? val?.ToString() : null;

        private static decimal GetDecimal(IDictionary<string, object> dict, string key) =>
            dict.TryGetValue(key, out var val) && val != DBNull.Value ? Convert.ToDecimal(val) : 0m;
    }
}
