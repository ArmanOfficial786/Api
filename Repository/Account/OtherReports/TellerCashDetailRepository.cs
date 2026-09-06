// Repositories/Implementations/AccountOperation/TellerCashDetailRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.AccountOperation.OthersReport;
using NexgenCosysReport.Inteface.ServiceInterface.AccountOperation.OthersReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;

namespace NexgenCosysReport.Repository.AccountOperation.OthersReport
{
    public class TellerCashDetailRepository : ITellerCashDetailRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<TellerCashDetailRepository> _logger;

        public TellerCashDetailRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<TellerCashDetailRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        // --------------------------------------------------------------
        // @SqlFilterExpOrder — column names match sp_6_56_GetTellerCashDetailTransaction's
        // final SELECT aliases (MemberId, MemberName, AccountNo, BillNo, CreatedOn).
        // Preserves the legacy substring-based natural sort for MemberId/AccountNo
        // (strips a trailing "-N" suffix before sorting) exactly as in the BLL.
        // --------------------------------------------------------------
        private static string BuildSqlOrderBy(TellerCashDetailRequestDto request)
        {
            return request.OrderBy?.Trim() switch
            {
                "Member Id" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId ",
                "Member Name" => " order by MemberName ",
                "Account No" => " order by substring(AccountNo, 1,(len(AccountNo)-charindex('-', AccountNo))-1), AccountNo",
                "BillNo" => " order by BillNo ",
                _ => " order by CreatedOn "
            };
        }

        private static long ResolveId(string? id)
        {
            if (string.IsNullOrWhiteSpace(id) || id == "string")
                return -1;

            return long.TryParse(id, out var parsed) ? parsed : -1;
        }

        public async Task<TellerCashDetailData> GetReportDataAsync(TellerCashDetailRequestDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.TransactionDateBs) || request.TransactionDateBs == "-1")
                {
                    throw new ArgumentException("TransactionDateBs is required.", nameof(request.TransactionDateBs));
                }

                var dateAdStr = await _dateConverter.NepaliToEnglishAsync(request.TransactionDateBs);
                var dateAd = dateAdStr; // ISO string, e.g. "2026-06-15" — passed as the SP expects a date/string param

                var branchId = ResolveId(request.BranchId);
                var tellerId = ResolveId(request.TellerId);
                var orderByClause = BuildSqlOrderBy(request);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // ---- sp_6_56_GetTellerCashDetailTransaction ----
                var transactionParams = new DynamicParameters();
                transactionParams.Add("@SqlFilterExpDate", dateAd);
                transactionParams.Add("@SqlFilterExpBranchId", branchId);
                transactionParams.Add("@SqlFilterExpUserId", tellerId);
                transactionParams.Add("@SqlFilterExpOrder", orderByClause);
                transactionParams.Add("@TotalTransactionDebit", dbType: DbType.Double, direction: ParameterDirection.Output);
                transactionParams.Add("@TotalTransactionCredit", dbType: DbType.Double, direction: ParameterDirection.Output);

                var transactionRows = (await connection.QueryAsync<TellerCashDetailTransactionRowDto>(
                    "sp_6_56_GetTellerCashDetailTransaction",
                    transactionParams,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                )).AsList();

                var transactionDR = transactionParams.Get<double?>("@TotalTransactionDebit") ?? 0;
                var transactionCR = transactionParams.Get<double?>("@TotalTransactionCredit") ?? 0;

                // ---- sp_6_56_GetTellerCashDetailManual ----
                var manualParams = new DynamicParameters();
                manualParams.Add("@SqlFilterExpDate", dateAd);
                manualParams.Add("@SqlFilterExpBranchId", branchId);
                manualParams.Add("@SqlFilterExpUserId", tellerId);
                manualParams.Add("@TotalTransactionDebit", dbType: DbType.Double, direction: ParameterDirection.Output);
                manualParams.Add("@TotalTransactionCredit", dbType: DbType.Double, direction: ParameterDirection.Output);
                manualParams.Add("@TotalTellerCashFrom", dbType: DbType.Double, direction: ParameterDirection.Output);
                manualParams.Add("@TotalCashTransferFrom", dbType: DbType.Double, direction: ParameterDirection.Output);
                manualParams.Add("@TotalCashTransferTo", dbType: DbType.Double, direction: ParameterDirection.Output);
                manualParams.Add("@TotalTellerCashTo", dbType: DbType.Double, direction: ParameterDirection.Output);

                var manualRows = (await connection.QueryAsync<TellerCashDetailManualRowDto>(
                    "sp_6_56_GetTellerCashDetailManual",
                    manualParams,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                )).AsList();

                var voucherDR = manualParams.Get<double?>("@TotalTransactionDebit") ?? 0;
                var voucherCR = manualParams.Get<double?>("@TotalTransactionCredit") ?? 0;
                var tellerCashFromVault = manualParams.Get<double?>("@TotalTellerCashFrom") ?? 0;
                var tellerCashFromTeller = manualParams.Get<double?>("@TotalCashTransferFrom") ?? 0;
                var tellerCashToTeller = manualParams.Get<double?>("@TotalCashTransferTo") ?? 0;
                var tellerCashToVault = manualParams.Get<double?>("@TotalTellerCashTo") ?? 0;

                // ---- Resolve display names (branch/teller), matching the legacy WebForm's lookups ----
                string branchName = "All";
                if (branchId != -1)
                {
                    var name = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId = @BranchId",
                        new { BranchId = branchId });
                    branchName = string.IsNullOrEmpty(name) ? "All" : name;
                }

                string tellerName = "All Teller";
                if (tellerId != -1)
                {
                    var name = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT FullName FROM UsmUser WHERE UsmUserId = @TellerId",
                        new { TellerId = tellerId });
                    tellerName = string.IsNullOrEmpty(name) ? "All Teller" : name;
                }

                return new TellerCashDetailData
                {
                    TransactionRows = transactionRows,
                    ManualRows = manualRows,
                    TransactionCashBalanceDR = (decimal)transactionDR,
                    TransactionCashBalanceCR = (decimal)transactionCR,
                    VoucherCashBalanceDR = (decimal)voucherDR,
                    VoucherCashBalanceCR = (decimal)voucherCR,
                    TellerCashFromVault = (decimal)tellerCashFromVault,
                    TellerCashFromTeller = (decimal)tellerCashFromTeller,
                    TellerCashToTeller = (decimal)tellerCashToTeller,
                    TellerCashToVault = (decimal)tellerCashToVault,
                    TransactionDateBs = request.TransactionDateBs,
                    BranchName = branchName,
                    TellerName = tellerName,
                    OrderBy = request.OrderBy,
                    ReportType = string.IsNullOrEmpty(request.ReportType) ? "D" : request.ReportType
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync");
                throw;
            }
        }
    }
}