// Repository/MemberAccount/OthersReport/SavingDepositDateWiseRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.MemberAccount.OthersReport
{
    public class SavingDepositDateWiseRepository : ISavingDepositDateWiseRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<SavingDepositDateWiseRepository> _logger;

        private const int CommandTimeoutSeconds = 120;

        public SavingDepositDateWiseRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<SavingDepositDateWiseRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<SavingDepositDateWiseData> GetReportDataAsync(SavingDepositDateWiseRequestDto request)
        {
            try
            {
                var transactionType = string.IsNullOrWhiteSpace(request.TransactionType)
                    ? "Deposit"
                    : request.TransactionType;

                List<SavingDepositDateWiseRowDto> resultList;

                if (transactionType.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    // Not a mode the original WebForms UI exposed (its radio buttons only ever
                    // sent "Deposit" or "Withdrawl"), but the API needs it: run both stored
                    // procedures and merge, mirroring the "All" pattern used elsewhere in the
                    // same BLL (e.g. GetBranchToBranchCollection's "All" case).
                    var depositRows = await QueryTransactionTypeAsync(request, "Deposit");
                    var withdrawlRows = await QueryTransactionTypeAsync(request, "Withdrawl");

                    resultList = depositRows.Concat(withdrawlRows).ToList();
                    resultList = ApplyInMemoryOrderBy(resultList, request.OrderBy);
                }
                else
                {
                    resultList = await QueryTransactionTypeAsync(request, transactionType);
                }

                // Get member details if a specific member was selected
                string? selectedMemberId = null;
                string? selectedMemberName = null;
                if (request.MemberId.HasValue && request.MemberId.Value != -1)
                {
                    var member = await GetMemberDetailsAsync(request.MemberId.Value);
                    if (member != null)
                    {
                        selectedMemberId = member.MemberId;
                        selectedMemberName = member.MemberName;
                    }
                }

                // Get saving type name if selected
                string? savingTypeName = null;
                if (request.SavingTypeId.HasValue && request.SavingTypeId.Value != -1)
                {
                    savingTypeName = await GetSavingTypeNameAsync(request.SavingTypeId.Value);
                }

                // Totals
                var totalDeposit = resultList
                    .Where(r => r.TransactionType == "Deposit")
                    .Sum(r => r.Amount ?? 0);
                var totalWithdrawal = resultList
                    .Where(r => r.TransactionType == "Withdrawl")
                    .Sum(r => r.Amount ?? 0);

                var chequeDetailText = GetChequeDetailText(request.ChequeDetail);

                return new SavingDepositDateWiseData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalAmount = resultList.Sum(r => r.Amount ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchNames = request.BranchName,
                    OrderBy = request.OrderBy,
                    TransactionType = transactionType,
                    ReportMode = request.ReportMode,
                    SelectedMemberId = selectedMemberId,
                    SelectedMemberName = selectedMemberName,
                    SavingTypeName = savingTypeName,
                    TotalDepositAmount = totalDeposit,
                    TotalWithdrawalAmount = totalWithdrawal,
                    ChequeDetail = request.ChequeDetail,
                    ChequeDetailText = chequeDetailText
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync for Saving Deposit Date Wise Report");
                throw;
            }
        }

        /// <summary>
        /// Runs a single stored procedure for the given concrete transaction type
        /// ("Deposit" or "Withdrawl" only). Order-by is intentionally NOT appended
        /// here when called from the "All" merge path (see ApplyInMemoryOrderBy),
        /// because two separately-sorted SP results can't be interleaved correctly -
        /// the merge needs to sort the combined set as one list.
        /// </summary>
        private async Task<List<SavingDepositDateWiseRowDto>> QueryTransactionTypeAsync(
            SavingDepositDateWiseRequestDto request, string concreteType)
        {
            var sqlFilterExp = new StringBuilder();

            if (request.MemberId.HasValue && request.MemberId.Value != -1)
            {
                sqlFilterExp.Append($" And t.MemMemberRegistrationId = {request.MemberId.Value}");
            }

            if (request.ReportMode == "DateWise" &&
                !string.IsNullOrEmpty(request.FromDateBs) && request.FromDateBs != "-1" &&
                !string.IsNullOrEmpty(request.ToDateBs) && request.ToDateBs != "-1")
            {
                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);
                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");
                sqlFilterExp.Append($" And t.TransactionOn between '{fromDateStr}' And '{toDateStr}'");
            }

            if (request.ReportMode == "DateWise" &&
                !string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
            {
                sqlFilterExp.Append($" And t.UsmOfficeId in ({request.BranchIds})");
            }

            if (request.SavingTypeId.HasValue && request.SavingTypeId.Value != -1)
            {
                sqlFilterExp.Append($" And a.SycDepositTypeId = {request.SavingTypeId.Value}");
            }

            if (request.ChequeDetail == 1 && concreteType == "Withdrawl")
            {
                sqlFilterExp.Append(" And t.MamChequeWithdrawId is not null ");
            }

            // Only append ORDER BY for single-type calls (i.e. not part of an "All" merge).
            // The "All" path sorts the merged list in-memory instead - see ApplyInMemoryOrderBy.
            if (!request.TransactionType.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                sqlFilterExp.Append(BuildOrderByClause(request.OrderBy));
            }

            string spName = concreteType switch
            {
                "Deposit" => "sp_5_43_GetSavingDepositDateWise",
                "Withdrawl" => "sp_5_43_GetSavingWithdrawlDateWise",
                _ => throw new ArgumentException($"Unknown TransactionType '{concreteType}'. Expected 'Deposit' or 'Withdrawl'.")
            };

            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
            parameters.Add("@SqlChequeId", request.ChequeDetail, DbType.Int32);

            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var rows = await connection.QueryAsync<SavingDepositDateWiseRowDto>(
                spName,
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: CommandTimeoutSeconds
            );

            return rows.AsList();
        }

        /// <summary>
        /// Sorts the merged Deposit+Withdrawl list in-memory when TransactionType == "All",
        /// since each half was fetched from a different SP and can't share a single SQL
        /// ORDER BY. Mirrors the same label set as BuildOrderByClause.
        /// </summary>
        private static List<SavingDepositDateWiseRowDto> ApplyInMemoryOrderBy(
            List<SavingDepositDateWiseRowDto> rows, string orderBy)
        {
            return orderBy switch
            {
                "Date" => rows.OrderBy(r => r.Date).ToList(),
                "Member Id" => rows.OrderBy(r => r.MemberId).ToList(),
                "Member Name" => rows.OrderBy(r => r.MemberName).ToList(),
                "Saving Type" => rows.OrderBy(r => r.SavingType).ToList(),
                "Account No" => rows.OrderBy(r => r.AccountNo).ToList(),
                "Amount" => rows.OrderByDescending(r => r.Amount).ToList(),
                _ => rows
            };
        }

        private static string BuildOrderByClause(string orderBy)
        {
            return orderBy switch
            {
                "Date" => " order by Date ",
                "Member Id" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId",
                "Member Name" => " order by MemberName ",
                "Saving Type" => " order by savingType ",
                "Account No" => " order by substring(AccountNo, 1,(len(AccountNo)-charindex('-', AccountNo))-1), AccountNo",
                "Amount" => " order by Amount DESC",
                _ => string.Empty
            };
        }

        private static string GetChequeDetailText(int chequeDetail)
        {
            return chequeDetail switch
            {
                -1 => "All",
                1 => "Only Cheque",
                2 => "Cash Only",
                3 => "Bank Only",
                _ => "All"
            };
        }

        private async Task<MemberDetailDto?> GetMemberDetailsAsync(long memberRegistrationId)
        {
            try
            {
                const string query = @"
                    SELECT 
                        MemberId,
                        CONCAT(FirstName, ' ', ISNULL(MiddleName, ''), ' ', LastName) AS MemberName
                    FROM MemMemberRegistration 
                    WHERE MemMemberRegistrationId = @MemberId";

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                return await connection.QueryFirstOrDefaultAsync<MemberDetailDto>(
                    query,
                    new { MemberId = memberRegistrationId }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting member details for ID: {MemberId}", memberRegistrationId);
                return null;
            }
        }

        private async Task<string?> GetSavingTypeNameAsync(long savingTypeId)
        {
            try
            {
                const string query = @"
                    SELECT DepositTypeName 
                    FROM SycDepositType 
                    WHERE SycDepositTypeId = @SavingTypeId AND IsActive = 1";

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                return await connection.QueryFirstOrDefaultAsync<string>(
                    query,
                    new { SavingTypeId = savingTypeId }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting saving type name for ID: {SavingTypeId}", savingTypeId);
                return null;
            }
        }

        private class MemberDetailDto
        {
            public string? MemberId { get; set; }
            public string? MemberName { get; set; }
        }
    }
}