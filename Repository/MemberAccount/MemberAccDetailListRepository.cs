// Repository/AccountOperation/MemberAccountDetailNoRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount;
using System.Data;

namespace NexgenCosysReport.Repository.MemberAccount
{
    public class MemberAccountDetailNoRepository : IMemberAccDetailList
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<MemberAccountDetailNoRepository> _logger;

        public MemberAccountDetailNoRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<MemberAccountDetailNoRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<MemberAccountDetailNoData> GetMemberAccountDetailNo(MemberAccountDetailNoRequest request)
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // ---- Replicates CMemberAccountManagementReports.GetMemberAccountDetailNoReport exactly ----
            // The SP accepts exactly 6 params: @SqlFilterExp, @SqlFilterExpTill, @SqlFilterExpOrderBy,
            // @SqlSavingTypeId, @SqlLoanTypeId, @SqlShareTypeId

            var sqlFilterExp = string.Empty;
            var sqlFilterExpTill = string.Empty;
            var sqlFilterExpOrderBy = " WHERE 1=1 ";

            var savingTypeId = ParseIdOrDefault(request.SavingTypeId);
            var loanTypeId = ParseIdOrDefault(request.LoanTypeId);
            var shareTypeId = ParseIdOrDefault(request.ShareTypeId);

            // --- Branch/office filter --- ("-1" or SameCompanyName => no filter, matches legacy "")
            var usmOfficeId = (!request.SameCompanyName && !string.IsNullOrEmpty(request.BranchIds)
                                && request.BranchIds != "-1")
                ? request.BranchIds
                : string.Empty;

            if (usmOfficeId != "")
            {
                sqlFilterExp += " And M.UsmOfficeId in (" + usmOfficeId + ")";
            }

            // --- Till date (Nepali -> English) ---
            if (!string.IsNullOrEmpty(request.TillDate) && request.TillDate != "-1")
            {
                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDate);
                sqlFilterExpTill = tillDateAd.ToString("MM/dd/yyyy");
            }

            // --- Member type (0=All, 1=Active, 2=Inactive) ---
            if (request.MemberType == 1)
            {
                if (request.IncludeSaving) sqlFilterExpOrderBy += " AND Saving > 0 ";
                if (request.IncludeShare) sqlFilterExpOrderBy += " AND Share > 0 ";
                if (request.IncludeLoan) sqlFilterExpOrderBy += " AND Loan > 0 ";
            }
            else if (request.MemberType == 2)
            {
                if (request.IncludeSaving) sqlFilterExpOrderBy += " AND Saving = 0 ";
                if (request.IncludeShare) sqlFilterExpOrderBy += " AND Share = 0 ";
                if (request.IncludeLoan) sqlFilterExpOrderBy += " AND Loan = 0 ";
            }

            // --- Order by (matches legacy dropdown text exactly) ---
            sqlFilterExpOrderBy += request.OrderBy switch
            {
                "Member Id" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId",
                "Account No" => " order by substring(AccountNo, 1,(len(AccountNo)-charindex('-', AccountNo))-1), AccountNo",
                "Saving" => " order by Saving desc",
                "Share" => " order by Share desc",
                "Loan" => " order by Loan desc",
                _ => " order by MemberName"
            };

            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExp", sqlFilterExp);
            parameters.Add("@SqlFilterExpTill", sqlFilterExpTill);
            parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy);
            parameters.Add("@SqlSavingTypeId", savingTypeId.ToString());
            parameters.Add("@SqlLoanTypeId", loanTypeId.ToString());
            parameters.Add("@SqlShareTypeId", shareTypeId.ToString());

            // The SP returns one row per member with columns:
            // MemberId, MemberName, AccountNo, Address, ContactNo/Mobile, Saving, Share, Loan
            // Map raw column names -> our Dto via a lightweight anonymous read.
            var rawRows = await connection.QueryAsync(
                "sp_5_43_GetMemberAccountDetailNoReport",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var list = new List<MemberAccountDetailNoRowDto>();
            foreach (var r in rawRows)
            {
                var dict = (IDictionary<string, object>)r;
                list.Add(new MemberAccountDetailNoRowDto
                {
                    MemberId = GetString(dict, "MemberId"),
                    Name = GetString(dict, "MemberName") ?? GetString(dict, "Name"),
                    Address = GetString(dict, "Address"),
                    ContactNo = GetString(dict, "ContactNo") ?? GetString(dict, "Mobile") ?? GetString(dict, "MobileNo"),
                    Saving = GetDecimal(dict, "Saving"),
                    Share = GetDecimal(dict, "Share"),
                    Loan = GetDecimal(dict, "Loan")
                });
            }

            return new MemberAccountDetailNoData
            {
                Rows = list,
                TotalRecords = list.Count,
                TotalSaving = list.Sum(x => x.Saving),
                TotalShare = list.Sum(x => x.Share),
                TotalLoan = list.Sum(x => x.Loan)
            };
        }

        private static string? GetString(IDictionary<string, object> dict, string key) =>
            dict.TryGetValue(key, out var val) && val != DBNull.Value ? val?.ToString() : null;

        private static decimal GetDecimal(IDictionary<string, object> dict, string key) =>
            dict.TryGetValue(key, out var val) && val != DBNull.Value ? Convert.ToDecimal(val) : 0m;

        private static long ParseIdOrDefault(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return -1;
            return long.TryParse(value, out var id) ? id : -1;
        }
    }
}