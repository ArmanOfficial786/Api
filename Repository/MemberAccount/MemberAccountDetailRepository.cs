//// Repository/AccountOperation/MemberAccountDetailRepository.cs
//using Dapper;
using NexgenCosysReport.DbContext;
//using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore;
//using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount;
//using System.Data;

//namespace NexgenCosysReport.Repository.MemberAccount
//{
//    public class MemberAccountDetailRepository : IMemberAccountDetail
//    {
//        private readonly AppDbContext _context;
//        private readonly IDateConverterService _dateConverter;
//        private readonly ILogger<MemberAccountDetailRepository> _logger;

//        public MemberAccountDetailRepository(
//            AppDbContext context,
//            IDateConverterService dateConverter,
//            ILogger<MemberAccountDetailRepository> logger)
//        {
//            _context = context;
//            _dateConverter = dateConverter;
//            _logger = logger;
//        }

//        public async Task<MemberAccountDetailData> GetMemberAccountDetailReport(MemberAccountDetailRequest request)
//        {
//            var connectionString = _context.Database.GetConnectionString();
//            using var connection = new SqlConnection(connectionString);
//            await connection.OpenAsync();

//            // --- Convert Nepali date to English ---
//            string tillDateStr = string.Empty;
//            if (!string.IsNullOrEmpty(request.TillDate) && request.TillDate != "-1")
//            {
//                try
//                {
//                    var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDate);
//                    tillDateStr = tillDateAd.ToString("MM/dd/yyyy");
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "Date conversion failed for TillDate: {TillDate}", request.TillDate);
//                    tillDateStr = DateTime.Now.ToString("MM/dd/yyyy");
//                }
//            }
//            else
//            {
//                tillDateStr = DateTime.Now.ToString("MM/dd/yyyy");
//            }

//            // --- Build filters ---
//            string memberFilter = string.Empty;
//            if (request.MemberRegistrationId != -1)
//            {
//                memberFilter = $" And m.MemMemberRegistrationId = {request.MemberRegistrationId}";
//            }

//            // --- Deposit Type filter ---
//            string depositTypeFilter = string.Empty;
//            if (!string.IsNullOrEmpty(request.DepositTypeId) && request.DepositTypeId != "-1")
//            {
//                depositTypeFilter = $" And a.SycDepositTypeId = {request.DepositTypeId}";
//            }

//            // --- Collector filter ---
//            string collectorFilter = string.Empty;
//            if (!string.IsNullOrEmpty(request.CollectorId) && request.CollectorId != "-1")
//            {
//                collectorFilter = $" And a.HurCollectorId = {request.CollectorId}";
//            }

//            // --- Branch filter ---
//            string branchFilter = string.Empty;
//            if (!request.SameCompanyName && !string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
//            {
//                branchFilter = $" And a.UsmOfficeId in ({request.BranchIds})";
//            }

//            // --- Member Group filter ---
//            string memberGroupFilter = string.Empty;
//            if (!string.IsNullOrEmpty(request.MemberGroupId) && request.MemberGroupId != "-1")
//            {
//                memberGroupFilter = $" And m.SycMemberGroupId = {request.MemberGroupId}";
//            }

//            // --- Collection Center filter ---
//            string collectionCenterFilter = string.Empty;
//            if (!string.IsNullOrEmpty(request.CollectionCenterId) && request.CollectionCenterId != "-1")
//            {
//                collectionCenterFilter = $" And mg.SycCollectionCenterId = {request.CollectionCenterId}";
//            }

//            // --- Date filter ---
//            string dateFilter = $" And t.TransactionOn <= '{tillDateStr}' ";

//            // --- Status filter ---
//            string statusFilter = string.Empty;
//            if (request.Status == 1) // Opened
//            {
//                statusFilter = "WHERE MamAccountStatusId IN (1,4,5) ";
//            }
//            else if (request.Status == 2) // Closed
//            {
//                statusFilter = "WHERE MamAccountStatusId IN (2) ";
//            }
//            else if (request.Status == 3) // With Balance
//            {
//                statusFilter = "WHERE Balance > 0 ";
//            }
//            else if (request.Status == 4) // Suspended
//            {
//                statusFilter = "WHERE MamAccountStatusId IN (4) ";
//            }
//            else if (request.Status == 5) // Disable
//            {
//                statusFilter = "WHERE MamAccountStatusId IN (5) ";
//            }
//            else
//            {
//                statusFilter = "WHERE MamAccountStatusId IN (1,2,4,5) ";
//            }

//            // --- Order By ---
//            string orderByClause = MapOrderBy(request.OrderBy);

//            // --- Build full filter ---
//            string sqlFilterExpa = branchFilter + depositTypeFilter + memberFilter + collectorFilter + memberGroupFilter + collectionCenterFilter;
//            string sqlFilterExpt = branchFilter + depositTypeFilter + memberFilter + collectorFilter + memberGroupFilter + collectionCenterFilter + dateFilter;
//            string sqlFilterExpOrderBy = statusFilter + orderByClause;

//            _logger.LogInformation(
//                "MemberAccountDetail SP params -> SqlFilterExpa: {SqlFilterExpa}, SqlFilterExpt: {SqlFilterExpt}, SqlFilterExpOrderBy: {SqlFilterExpOrderBy}",
//                sqlFilterExpa, sqlFilterExpt, sqlFilterExpOrderBy);

//            var parameters = new DynamicParameters();
//            parameters.Add("@SqlFilterExpa", sqlFilterExpa);
//            parameters.Add("@SqlFilterExpt", sqlFilterExpt);
//            parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy);

//            try
//            {
//                var rawRows = await connection.QueryAsync(
//                    "sp_5_43_GetMemberAccountDetails",
//                    parameters,
//                    commandType: CommandType.StoredProcedure
//                );

//                var list = new List<MemberAccountDetailRowDto>();
//                foreach (var r in rawRows)
//                {
//                    var dict = (IDictionary<string, object>)r;
//                    var row = new MemberAccountDetailRowDto
//                    {
//                        MemberId = GetString(dict, "MemberId"),
//                        MemberName = GetString(dict, "Name"),
//                        PermanentAddress = GetString(dict, "PermanentAddress"),
//                        TemporaryAddress = GetString(dict, "TemporaryAddress"),
//                        ContactNo = GetString(dict, "ContactNo"),
//                        BirthOnBS = GetString(dict, "BirthOnBS"),
//                        CitizenshipNo = GetString(dict, "CitizenshipNo"),
//                        FatherName = GetString(dict, "FatherName"),
//                        MotherName = GetString(dict, "MotherName"),
//                        SpouseName = GetString(dict, "SpouseName"),
//                        RegisteredOn = GetString(dict, "RegisteredOn"),
//                        SavingAccountType = GetString(dict, "DepositTypeName"),
//                        AccountNo = GetString(dict, "AccountNo"),
//                        AccountOpenOn = GetString(dict, "AccountOpenOnBs"),
//                        MatureDate = GetString(dict, "MaturityOnBs"),
//                        InterestTransferType = GetString(dict, "InterestTransferType"),
//                        InterestRate = GetDecimal(dict, "InterestRate"),
//                        TaxRate = GetDecimal(dict, "TaxRate"),
//                        InterestTransferAccount = GetString(dict, "InterestTransferAccount"),
//                        FreezeAmount = GetDecimal(dict, "FreezeAmount"),
//                        GuaranteeAmount = GetDecimal(dict, "GuaranteeAmount"),
//                        InstallmentAmount = GetDecimal(dict, "InstallmentAmount"),
//                        InstallmentType = GetString(dict, "InstallmentType"),
//                        DueCount = GetInt(dict, "DueCount"),
//                        Deposit = GetDecimal(dict, "Deposit"),
//                        Withdraw = GetDecimal(dict, "Withdraw"),
//                        Balance = GetDecimal(dict, "Balance"),
//                        CollectionCenter = GetString(dict, "CollectionCenter"),
//                        MemberGroup = GetString(dict, "MemberGroup"),
//                        CollectorName = GetString(dict, "CollectorName"),
//                        StatusId = GetInt(dict, "MamAccountStatusId"),
//                        StatusName = GetString(dict, "StatusName")
//                    };
//                    list.Add(row);
//                }

//                _logger.LogInformation("MemberAccountDetail returned {Count} rows", list.Count);

//                return new MemberAccountDetailData
//                {
//                    Rows = list,
//                    TotalRecords = list.Count,
//                    TotalDeposit = list.Sum(x => x.Deposit ?? 0m),
//                    TotalWithdraw = list.Sum(x => x.Withdraw ?? 0m),
//                    TotalBalance = list.Sum(x => x.Balance ?? 0m)
//                };
//            }
//            catch (SqlException ex)
//            {
//                _logger.LogError(ex, "SQL Error in MemberAccountDetail. Parameters: {@Parameters}", parameters);
//                throw new Exception($"Database error: {ex.Message}", ex);
//            }
//        }

//        private string MapOrderBy(string orderBy)
//        {
//            return orderBy switch
//            {
//                "Member Name" => "ORDER BY Name",
//                "Member Id" => "ORDER BY substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId",
//                "Account No" => "ORDER BY substring(AccountNo, 1,(len(AccountNo)-charindex('-', AccountNo))-1), AccountNo",
//                "Interest Rate" => "ORDER BY InterestRate DESC",
//                "Deposit" => "ORDER BY Deposit DESC",
//                "Withdrawl" => "ORDER BY Withdraw DESC",
//                "Balance" => "ORDER BY Balance DESC",
//                _ => "ORDER BY Name"
//            };
//        }

//        private static string? GetString(IDictionary<string, object> dict, string key)
//        {
//            if (dict.TryGetValue(key, out var val) && val != DBNull.Value)
//            {
//                return val?.ToString();
//            }
//            return null;
//        }

//        private static decimal? GetDecimal(IDictionary<string, object> dict, string key)
//        {
//            if (dict.TryGetValue(key, out var val) && val != DBNull.Value)
//            {
//                try
//                {
//                    return Convert.ToDecimal(val);
//                }
//                catch
//                {
//                    return null;
//                }
//            }
//            return null;
//        }

//        private static int? GetInt(IDictionary<string, object> dict, string key)
//        {
//            if (dict.TryGetValue(key, out var val) && val != DBNull.Value)
//            {
//                try
//                {
//                    return Convert.ToInt32(val);
//                }
//                catch
//                {
//                    return null;
//                }
//            }
//            return null;
//        }
//    }
//}





// Repository/AccountOperation/MemberAccountDetailRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount;
using System.Data;

namespace NexgenCosysReport.Repository.MemberAccount
{
    public class MemberAccountDetailRepository : IMemberAccountDetail
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<MemberAccountDetailRepository> _logger;

        // sp_5_43_GetMemberAccountDetails joins Member/Account/Transaction tables with
        // dynamically-built filter strings, so it is intentionally not sargable and can
        // legitimately run well past ADO.NET's 30s default CommandTimeout on large branches.
        // The legacy WebForms CDataAccessLayer had this configured higher; that setting was
        // lost in the migration. Restore it explicitly here instead of relying on the default.
        private const int ReportCommandTimeoutSeconds = 180;

        public MemberAccountDetailRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<MemberAccountDetailRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<MemberAccountDetailData> GetMemberAccountDetailReport(
            MemberAccountDetailRequest request,
            CancellationToken ct = default)
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            // --- Convert Nepali date to English ---
            string tillDateStr;
            if (!string.IsNullOrEmpty(request.TillDate) && request.TillDate != "-1")
            {
                try
                {
                    var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDate);
                    tillDateStr = tillDateAd.ToString("MM/dd/yyyy");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Date conversion failed for TillDate: {TillDate}", request.TillDate);
                    tillDateStr = DateTime.Now.ToString("MM/dd/yyyy");
                }
            }
            else
            {
                tillDateStr = DateTime.Now.ToString("MM/dd/yyyy");
            }

            // --- Normalize "not selected" sentinels coming from the UI ---
            // Some dropdown option lists (Collection Center, Member Group) previously
            // fell back to a shared placeholder with id "0" before their real options
            // loaded, so "0" can arrive here meaning the same thing as "-1": no filter.
            // Treat both identically rather than relying on the frontend always sending
            // the canonical "-1" sentinel.
            var collectionCenterId = NormalizeFilterId(request.CollectionCenterId);
            var memberGroupId = NormalizeFilterId(request.MemberGroupId);

            // --- Build filters ---
            long memberRegistrationId = request.MemberRegistrationId ?? -1;
            string memberFilter = string.Empty;
            if (memberRegistrationId != -1)
            {
                memberFilter = $" And m.MemMemberRegistrationId = {memberRegistrationId}";
            }

            // --- Deposit Type filter ---
            string depositTypeFilter = string.Empty;
            if (!string.IsNullOrEmpty(request.DepositTypeId) && request.DepositTypeId != "-1")
            {
                depositTypeFilter = $" And a.SycDepositTypeId = {request.DepositTypeId}";
            }

            // --- Collector filter ---
            string collectorFilter = string.Empty;
            if (!string.IsNullOrEmpty(request.CollectorId) && request.CollectorId != "-1")
            {
                collectorFilter = $" And a.HurCollectorId = {request.CollectorId}";
            }

            // --- Branch filter ---
            string branchFilter = string.Empty;
            if (!request.SameCompanyName && !string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
            {
                branchFilter = $" And a.UsmOfficeId in ({request.BranchIds})";
            }

            // --- Member Group filter ---
            string memberGroupFilter = string.Empty;
            if (memberGroupId != null)
            {
                memberGroupFilter = $" And m.SycMemberGroupId = {memberGroupId}";
            }

            // --- Collection Center filter ---
            string collectionCenterFilter = string.Empty;
            if (collectionCenterId != null)
            {
                collectionCenterFilter = $" And mg.SycCollectionCenterId = {collectionCenterId}";
            }

            // --- Date filter ---
            string dateFilter = $" And t.TransactionOn <= '{tillDateStr}' ";

            // --- Status filter ---
            string statusFilter = request.Status switch
            {
                1 => "WHERE MamAccountStatusId IN (1,4,5) ",
                2 => "WHERE MamAccountStatusId IN (2) ",
                3 => "WHERE Balance > 0 ",
                4 => "WHERE MamAccountStatusId IN (4) ",
                5 => "WHERE MamAccountStatusId IN (5) ",
                _ => "WHERE MamAccountStatusId IN (1,2,4,5) "
            };

            // --- Order By ---
            string orderByClause = MapOrderBy(request.OrderBy);

            // --- Build full filter ---
            string sqlFilterExpa = branchFilter + depositTypeFilter + memberFilter + collectorFilter + memberGroupFilter + collectionCenterFilter;
            string sqlFilterExpt = branchFilter + depositTypeFilter + memberFilter + collectorFilter + memberGroupFilter + collectionCenterFilter + dateFilter;
            string sqlFilterExpOrderBy = statusFilter + orderByClause;

            _logger.LogInformation(
                "MemberAccountDetail SP params -> SqlFilterExpa: {SqlFilterExpa}, SqlFilterExpt: {SqlFilterExpt}, SqlFilterExpOrderBy: {SqlFilterExpOrderBy}",
                sqlFilterExpa, sqlFilterExpt, sqlFilterExpOrderBy);

            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExpa", sqlFilterExpa);
            parameters.Add("@SqlFilterExpt", sqlFilterExpt);
            parameters.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy);

            var commandDefinition = new CommandDefinition(
                "sp_5_43_GetMemberAccountDetails",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: ReportCommandTimeoutSeconds,
                cancellationToken: ct);

            try
            {
                var rawRows = await connection.QueryAsync(commandDefinition);

                var list = new List<MemberAccountDetailRowDto>();
                foreach (var r in rawRows)
                {
                    var dict = (IDictionary<string, object>)r;
                    var row = new MemberAccountDetailRowDto
                    {
                        MemberId = GetString(dict, "MemberId"),
                        MemberName = GetString(dict, "Name"),
                        PermanentAddress = GetString(dict, "PermanentAddress"),
                        TemporaryAddress = GetString(dict, "TemporaryAddress"),
                        ContactNo = GetString(dict, "ContactNo"),
                        BirthOnBS = GetString(dict, "BirthOnBS"),
                        CitizenshipNo = GetString(dict, "CitizenshipNo"),
                        FatherName = GetString(dict, "FatherName"),
                        MotherName = GetString(dict, "MotherName"),
                        SpouseName = GetString(dict, "SpouseName"),
                        RegisteredOn = GetString(dict, "RegisteredOn"),
                        SavingAccountType = GetString(dict, "DepositTypeName"),
                        AccountNo = GetString(dict, "AccountNo"),
                        AccountOpenOn = GetString(dict, "AccountOpenOnBs"),
                        MatureDate = GetString(dict, "MaturityOnBs"),
                        InterestTransferType = GetString(dict, "InterestTransferType"),
                        InterestRate = GetDecimal(dict, "InterestRate"),
                        TaxRate = GetDecimal(dict, "TaxRate"),
                        InterestTransferAccount = GetString(dict, "InterestTransferAccount"),
                        FreezeAmount = GetDecimal(dict, "FreezeAmount"),
                        GuaranteeAmount = GetDecimal(dict, "GuaranteeAmount"),
                        InstallmentAmount = GetDecimal(dict, "InstallmentAmount"),
                        InstallmentType = GetString(dict, "InstallmentType"),
                        DueCount = GetInt(dict, "DueCount"),
                        Deposit = GetDecimal(dict, "Deposit"),
                        Withdraw = GetDecimal(dict, "Withdraw"),
                        Balance = GetDecimal(dict, "Balance"),
                        CollectionCenter = GetString(dict, "CollectionCenter"),
                        MemberGroup = GetString(dict, "MemberGroup"),
                        CollectorName = GetString(dict, "CollectorName"),
                        StatusId = GetInt(dict, "MamAccountStatusId"),
                        StatusName = GetString(dict, "StatusName")
                    };
                    list.Add(row);
                }

                _logger.LogInformation("MemberAccountDetail returned {Count} rows", list.Count);

                return new MemberAccountDetailData
                {
                    Rows = list,
                    TotalRecords = list.Count,
                    TotalDeposit = list.Sum(x => x.Deposit ?? 0m),
                    TotalWithdraw = list.Sum(x => x.Withdraw ?? 0m),
                    TotalBalance = list.Sum(x => x.Balance ?? 0m)
                };
            }
            catch (SqlException ex) when (ex.Number == -2 || ex.Message.Contains("Timeout"))
            {
                _logger.LogError(ex,
                    "MemberAccountDetail SP timed out after {Timeout}s. Filters -> SqlFilterExpa: {SqlFilterExpa}, SqlFilterExpt: {SqlFilterExpt}",
                    ReportCommandTimeoutSeconds, sqlFilterExpa, sqlFilterExpt);
                throw new TimeoutException(
                    $"The Member Account Detail report timed out after {ReportCommandTimeoutSeconds}s. " +
                    "Try narrowing the branch/date/status filters, or check that sp_5_43_GetMemberAccountDetails has " +
                    "supporting indexes on UsmOfficeId, MamAccountStatusId, and TransactionOn.", ex);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL Error in MemberAccountDetail. Parameters: {@Parameters}", parameters);
                throw new Exception($"Database error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Treats "", "0", and "-1" all as "no filter selected". Returns null when the
        /// value means "no filter"; otherwise returns the raw id string, unchanged and
        /// still suitable for direct interpolation into the dynamic SQL fragment.
        /// </summary>
        private static string? NormalizeFilterId(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim();
            if (trimmed == "0" || trimmed == "-1")
                return null;

            return trimmed;
        }

        private string MapOrderBy(string orderBy)
        {
            return orderBy switch
            {
                "Member Name" => "ORDER BY Name",
                "Member Id" => "ORDER BY substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId",
                "Account No" => "ORDER BY substring(AccountNo, 1,(len(AccountNo)-charindex('-', AccountNo))-1), AccountNo",
                "Interest Rate" => "ORDER BY InterestRate DESC",
                "Deposit" => "ORDER BY Deposit DESC",
                "Withdrawl" => "ORDER BY Withdraw DESC",
                "Balance" => "ORDER BY Balance DESC",
                _ => "ORDER BY Name"
            };
        }

        private static string? GetString(IDictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var val) && val != DBNull.Value)
            {
                return val?.ToString();
            }
            return null;
        }

        private static decimal? GetDecimal(IDictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var val) && val != DBNull.Value)
            {
                try
                {
                    return Convert.ToDecimal(val);
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        private static int? GetInt(IDictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var val) && val != DBNull.Value)
            {
                try
                {
                    return Convert.ToInt32(val);
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }
    }
}
