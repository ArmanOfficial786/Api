// Repositories/MemberAccount/SavingsAccountInterestTransfer/SavingsAccountInterestTransferRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestPayableReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.InterestPayableReport;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.MemberAccount.SavingsAccountInterestTransfer
{
    public class SavingsAccountInterestTransferRepository : ISavingsAccountInterestTransferRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<SavingsAccountInterestTransferRepository> _logger;

        public SavingsAccountInterestTransferRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<SavingsAccountInterestTransferRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<SavingsAccountInterestTransferData> GetReportDataAsync(SavingsAccountInterestTransferRequestDto request)
        {
            try
            {
                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);
                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                var sqlFilterExp = new StringBuilder();

                // Date filter - using NextInterestDateOn
                sqlFilterExp.Append($" AND Ao.NextInterestDateOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");

                if (!string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
                {
                    sqlFilterExp.Append($" AND Ao.UsmOfficeId IN ({request.BranchIds})");
                }

                if (request.DepositTypeId != -1)
                {
                    sqlFilterExp.Append($" AND Ao.SycDepositTypeId = {request.DepositTypeId}");
                }

                sqlFilterExp.Append(BuildOrderByClause(request.OrderBy));

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var rows = await connection.QueryAsync<dynamic>(
                    "sp_5_43_GetSavingAcMaturityAndInterestTransfer",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                // ---------------------------------------------------------------
                // Map explicitly from the SP's actual SELECT list — it returns
                // MemberId, Name, ContactNo, AccountNo, DepositTypeName,
                // InterestRate, AccountOpenOnBs, NextInterestDateOnBs,
                // MaturityOnBs, LedgerBalance, AccountStatus.
                //
                // The previous code ran a *typed* Dapper query straight into
                // SavingsAccountInterestTransferRowDto, whose properties are
                // named AccountOpenDate / NextInterestDate (no "OnBs" suffix).
                // Dapper only maps on an exact column-name match, so those two
                // properties were always null — hence the empty date columns.
                // ---------------------------------------------------------------
                var resultList = rows.Select(r => new SavingsAccountInterestTransferRowDto
                {
                    DepositTypeName = r.DepositTypeName,
                    MemberId = r.MemberId,
                    MemberName = r.Name,
                    AccountNo = r.AccountNo,
                    AccountOpenDate = r.AccountOpenOnBs,
                    AccountOpenDateBs = r.AccountOpenOnBs,
                    NextInterestDate = r.NextInterestDateOnBs,
                    NextInterestDateBs = r.NextInterestDateOnBs,
                    InterestRate = r.InterestRate,
                    Balance = r.LedgerBalance
                }).ToList();

                var totalDepositTypes = resultList
                    .Select(r => r.DepositTypeName)
                    .Distinct()
                    .Count();

                string depositTypeName = string.Empty;
                if (request.DepositTypeId != -1 && resultList.Any())
                {
                    depositTypeName = resultList.First().DepositTypeName ?? string.Empty;
                }

                return new SavingsAccountInterestTransferData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    // The SP has no separate interest-amount column — Balance
                    // (LedgerBalance) is the only monetary figure it returns.
                    TotalBalance = resultList.Sum(r => r.Balance ?? 0),
                    TotalInterestAmount = resultList.Sum(r => r.InterestAmount ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchNames = request.BranchName,
                    OrderBy = request.OrderBy,
                    DepositTypeName = depositTypeName,
                    TotalDepositTypes = totalDepositTypes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync for Savings Account Interest Transfer Report");
                throw;
            }
        }

        private static string BuildOrderByClause(string orderBy)
        {
            // Note the SP's SELECT list aliases the member's display name as
            // "Name" (not MemberName), so "Member Name" sorts by Name.
            return orderBy switch
            {
                "Deposit Type" or "Savings Type" => " ORDER BY DepositTypeName",
                "Member Id" or "Member ID" => " ORDER BY substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId",
                "Member Name" => " ORDER BY Name",
                "Account No" => " ORDER BY substring(AccountNo, 1,(len(AccountNo)-charindex('-', AccountNo))-1), AccountNo",
                "Account Opened Date" or "A‎ccount Opened Date‎" => " ORDER BY AccountOpenOnBs",
                "Interest Transfer Date" or "Interest Transfer Date‎" => " ORDER BY NextInterestDateOnBs",
                _ => " ORDER BY NextInterestDateOnBs"
            };
        }
    }
}