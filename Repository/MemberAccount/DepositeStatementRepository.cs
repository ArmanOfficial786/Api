// Repositories/Implementations/AccountOperation/DepositStatementRepository.cs
using Dapper;
using NexgenCosysReport.DbContext;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount;
using System.Data;
using System.Reflection;

namespace NexgenCosysAPI.Repository.MemberAccount
{
    public class DepositStatementRepository : IDepositeStatement
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<DepositStatementRepository> _logger;

        // Only MemberDetail needs a Dapper CustomPropertyTypeMap: every one of its
        // columns is already the same CLR type as the DTO property (string<-string,
        // decimal?<-numeric), so a name remap is sufficient -- no cast problems.
        private static readonly Dictionary<string, string> MemberDetailColumnMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // SP column          -> DTO property (confirmed from sp_5_43_GetDepositStatementMemberDetails[Nepali])
                ["AccountOpenedDate"] = nameof(DepositStatementMemberDetailDto.AccountOpenDate),
                ["AccountType"] = nameof(DepositStatementMemberDetailDto.DepositTypeName),
                ["PhoneNo"] = nameof(DepositStatementMemberDetailDto.MobileNo),
                // MemberId, MemberName, AccountNo, Address, InterestRate match by name already.
            };

        public DepositStatementRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<DepositStatementRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;

            if (SqlMapper.GetTypeMap(typeof(DepositStatementMemberDetailDto)) is not CustomPropertyTypeMap)
            {
                SqlMapper.SetTypeMap(
                    typeof(DepositStatementMemberDetailDto),
                    new CustomPropertyTypeMap(typeof(DepositStatementMemberDetailDto), (type, columnName) =>
                    {
                        var propName = MemberDetailColumnMap.TryGetValue(columnName, out var mapped)
                            ? mapped
                            : columnName;

                        return type.GetProperty(
                            propName,
                            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    })
                );
            }

            // NOTE: DepositStatementRowDto is intentionally NOT given a typed Dapper map.
            // TransactionOn/ValueDateOn are SQL `date` (-> C# DateTime) and TN is SQL
            // `numeric` (-> C# decimal), while the DTO stores Date/ValueDate/BillNo as
            // string for display. Dapper's typed QueryAsync<T> throws InvalidCastException
            // on DateTime->string / decimal->string, so rows are read as `dynamic` and
            // converted manually in MapRow() below.
        }

        private SqlConnection GetOpenConnection() =>
            new SqlConnection(_context.Database.GetConnectionString());

        public async Task<DepositStatementData> GetDepositStatement(DepositStatementRequestDto request)
        {
            var accountId = await GetAccountIdByAccountNo(request.AccountNo);
            if (accountId == 0)
            {
                throw new ArgumentException($"Account not found for AccountNo '{request.AccountNo}'");
            }

            var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
            var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

            var (openingBalance, closingBalance, rows) = await GetDepositStatementData(
                fromDateAd.ToString("MM/dd/yyyy"),
                toDateAd.ToString("MM/dd/yyyy"),
                request.AccountNo,
                request.EnableInterest,
                request.EntryBy,
                request.Language,
                request.CustomNarration);

            var memberDetail = await GetMemberDetails(accountId, request.Language);

            decimal interestAmount = 0;
            decimal taxAmount = 0;
            if (request.EnableInterest)
            {
                var (interest, tax, _) = await GetInterestAndTax(accountId, toDateAd.ToString("MM/dd/yyyy"));
                interestAmount = interest;
                taxAmount = tax;

                var interestPayable = await GetInterestPayable(accountId);
                if (interestPayable.HasValue && interestPayable.Value > 0)
                {
                    interestAmount += interestPayable.Value;
                }
            }

            var officeInfo = await GetOfficeInfo(accountId);
            var verifiedTillBs = await GetLatestVerification(request.AccountNo);

            return new DepositStatementData
            {
                Rows = rows,
                MemberDetail = memberDetail,
                OpeningBalance = openingBalance,
                ClosingBalance = closingBalance,
                InterestAmount = interestAmount,
                TaxAmount = taxAmount,
                AccountNo = request.AccountNo,
                FromDateBs = request.FromDateBs,
                ToDateBs = request.ToDateBs,
                HasVerification = !string.IsNullOrEmpty(verifiedTillBs),
                VerifiedTillBs = verifiedTillBs,
                OfficeId = officeInfo?.UsmOfficeId,
                OfficeName = officeInfo?.OfficeName,
                TotalRecords = rows.Count,
                MamAccountOpeningId = accountId
            };
        }


        public async Task<(decimal OpeningBalance, decimal ClosingBalance, List<DepositStatementRowDto> Rows)> GetDepositStatementData(
            string fromDate,
            string toDate,
            string accountNo,
            bool enableInterest,
            bool entryBy,
            string language,
            bool customNarration)
        {
            using var connection = GetOpenConnection();
            await connection.OpenAsync();

            string spName = language == "Nepali"
                ? (customNarration ? "sp_5_43_GetDepositStatementNepaliCustum" : "sp_5_43_GetDepositStatementNepali")
                : "sp_5_43_GetDepositStatement";

            var parameters = new DynamicParameters();
            parameters.Add("@SqlFromDate", fromDate);
            parameters.Add("@SqlTodate", toDate);
            parameters.Add("@SqlAccountNo", accountNo);
            parameters.Add("@SqlInterest", enableInterest ? 1 : 0);
            parameters.Add("@SqlEntryBy", entryBy ? 1 : 0);
            parameters.Add("@OpeningBalance", dbType: DbType.Double, direction: ParameterDirection.Output);
            parameters.Add("@ClosingBalance", dbType: DbType.Double, direction: ParameterDirection.Output);

            _logger.LogInformation(
                "Executing SP: {SpName}, Account: {AccountNo}, From: {From}, To: {To}, Interest: {Interest}, EntryBy: {EntryBy}",
                spName, accountNo, fromDate, toDate, enableInterest, entryBy);

            var rawRows = (await connection.QueryAsync(
                spName, parameters, commandType: CommandType.StoredProcedure)).ToList();

            var allRows = rawRows
                .Select(r => MapRow((IDictionary<string, object>)r))
                .ToList();

            // ROOT CAUSE: when @SqlInterest = 1, the SP full-joins the date range against a
            // generated #Numbers calendar so it can compute daily interest -- that produces
            // one row per calendar day even when nothing happened that day (empty Description,
            // zero Deposit/Withdraw, balance just carried forward). WebForms' report only ever
            // rendered rows with real activity; keep parity by dropping the no-activity filler
            // rows here rather than in the view, so totals/paging/rendering all stay correct.
            var rows = allRows
                .Where(r => !string.IsNullOrWhiteSpace(r.Particulars)
                            || (r.Deposit ?? 0) != 0
                            || (r.Withdraw ?? 0) != 0)
                .ToList();

            var openingBalance = Convert.ToDecimal(parameters.Get<double?>("@OpeningBalance") ?? 0);
            var closingBalance = Convert.ToDecimal(parameters.Get<double?>("@ClosingBalance") ?? 0);

            _logger.LogInformation(
                "SP {SpName} returned {RawCount} raw rows, {FilteredCount} after dropping no-activity filler rows.",
                spName, allRows.Count, rows.Count);

            return (openingBalance, closingBalance, rows);
        }

        // Confirmed columns (all three SPs, any branch): TransactionOn, TransactionOnBs,
        // Description, DepositAmount, WithdrawlAmount, Balance, TN, ValueDateOn, ValueDateOnBs,
        // + one unnamed IsPayable column (ignored -- not used by the report).
        private static DepositStatementRowDto MapRow(IDictionary<string, object> row)
        {
            var lookup = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in row)
                lookup[kv.Key] = kv.Value is DBNull ? null : kv.Value;

            object? Get(string col) => lookup.TryGetValue(col, out var v) ? v : null;

            var (particulars, rowEntryBy) = SplitDescription(AsString(Get("Description")));

            return new DepositStatementRowDto
            {
                Date = AsDateString(Get("TransactionOn")),
                ValueDate = AsDateString(Get("ValueDateOn")),
                Particulars = particulars,
                EntryBy = rowEntryBy,
                Deposit = AsDecimal(Get("DepositAmount")),
                Withdraw = AsDecimal(Get("WithdrawlAmount")),
                Balance = AsDecimal(Get("Balance")) ?? 0,
                BillNo = FormatBillNo(AsDecimal(Get("TN")))
            };
        }

        // sp_5_43_GetDepositStatement builds Description1 as:
        //   Description + ' EntryBy: ' + FullName   (only when @SqlEntryBy = 1)
        // There is no separate "entry by" column -- it's appended server-side to the
        // narration text. Split it back out so Particulars and EntryBy render cleanly.
        private static (string? Particulars, string? EntryBy) SplitDescription(string? raw)
        {
            if (string.IsNullOrEmpty(raw)) return (raw, null);

            const string marker = " EntryBy: ";
            var idx = raw.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return (raw, null);

            return (raw[..idx], raw[(idx + marker.Length)..]);
        }

        private static string? AsString(object? v) => v?.ToString();

        private static string? AsDateString(object? v) => v switch
        {
            null => null,
            DateTime dt => dt.ToString("MM/dd/yyyy"),
            _ => v.ToString()
        };

        private static decimal? AsDecimal(object? v) => v switch
        {
            null => null,
            decimal d => d,
            double db => Convert.ToDecimal(db),
            float f => Convert.ToDecimal(f),
            int i => i,
            long l => l,
            _ => Convert.ToDecimal(v)
        };

        // TN (transaction number) is 0/absent on interest & tax rows -- treat as no bill no.
        private static string? FormatBillNo(decimal? tn) =>
            tn is null or 0 ? null : tn.Value.ToString("0");

        public async Task<DepositStatementMemberDetailDto?> GetMemberDetails(long accountId, string language)
        {
            using var connection = GetOpenConnection();
            await connection.OpenAsync();

            string spName = language == "Nepali"
                ? "sp_5_43_GetDepositStatementMemberDetailsNepali"
                : "sp_5_43_GetDepositStatementMemberDetails";

            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExpDetails", $" And a.MamAccountOpeningId = {accountId}");

            var detail = await connection.QueryFirstOrDefaultAsync<DepositStatementMemberDetailDto>(
                spName,
                parameters,
                commandType: CommandType.StoredProcedure);

            if (detail != null && string.IsNullOrEmpty(detail.MemberName))
            {
                _logger.LogWarning(
                    "DepositStatement member detail bound but MemberName is empty for {SpName}, accountId {AccountId}.",
                    spName, accountId);
            }

            return detail;
        }

        public async Task<(decimal Interest, decimal Tax, decimal ClosingBalance)> GetInterestAndTax(
            long mamAccountOpeningId,
            string toDate)
        {
            using var connection = GetOpenConnection();
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@SqlInterestDate", toDate);
            parameters.Add("@SqlMamAccountOpeningId", mamAccountOpeningId.ToString());
            parameters.Add("@finalInterestAmount", dbType: DbType.Double, direction: ParameterDirection.Output);
            parameters.Add("@finalTaxAmount", dbType: DbType.Double, direction: ParameterDirection.Output);
            parameters.Add("@closingBalance", dbType: DbType.Double, direction: ParameterDirection.Output);

            await connection.QueryAsync(
                "sp_5_43_GetInterestCalculationSingleAC",
                parameters,
                commandType: CommandType.StoredProcedure);

            var interest = Convert.ToDecimal(parameters.Get<double?>("@finalInterestAmount") ?? 0);
            var tax = Convert.ToDecimal(parameters.Get<double?>("@finalTaxAmount") ?? 0);
            var balance = Convert.ToDecimal(parameters.Get<double?>("@closingBalance") ?? 0);

            return (interest, tax, balance);
        }

        public async Task<long> GetAccountIdByAccountNo(string accountNo)
        {
            using var connection = GetOpenConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT TOP 1 MamAccountOpeningId
                FROM MamAccountOpening
                WHERE AccountNo = @AccountNo
                  AND (IsDeleted IS NULL OR IsDeleted = 0)
                ORDER BY MamAccountOpeningId DESC";

            var result = await connection.ExecuteScalarAsync<long?>(
                sql,
                new { AccountNo = accountNo });

            if (result is null or 0)
            {
                _logger.LogWarning("No MamAccountOpening row matched AccountNo '{AccountNo}'.", accountNo);
            }

            return result ?? 0;
        }

        public async Task<dynamic?> GetOfficeInfo(long accountId)
        {
            using var connection = GetOpenConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT ao.UsmOfficeId, o.OfficeName
                FROM MamAccountOpening ao
                LEFT JOIN UsmOffice o ON ao.UsmOfficeId = o.UsmOfficeId
                WHERE ao.MamAccountOpeningId = @AccountId";

            return await connection.QueryFirstOrDefaultAsync(
                sql,
                new { AccountId = accountId });
        }

        public async Task<string?> GetLatestVerification(string accountNo)
        {
            using var connection = GetOpenConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT TOP 1 v.VerifiedToDateOnBs
                FROM MamDepositStatementVerification v
                INNER JOIN MamAccountOpening a ON v.MamAccountOpeningId = a.MamAccountOpeningId
                WHERE a.AccountNo = @AccountNo
                ORDER BY v.MamDepositStatementVerificationId DESC";

            return await connection.ExecuteScalarAsync<string?>(
                sql,
                new { AccountNo = accountNo });
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
                sql,
                new { AccountId = accountId });
        }
    }
}
