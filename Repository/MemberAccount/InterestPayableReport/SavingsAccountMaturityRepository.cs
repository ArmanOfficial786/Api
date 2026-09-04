// Repositories/MemberAccount/SavingsAccountMaturityReport/SavingsAccountMaturityRepository.cs

using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestPayableReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.InterestPayableReport;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.MemberAccount.SavingsAccountMaturityReport
{
    public class SavingsAccountMaturityRepository : ISavingsAccountMaturityRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<SavingsAccountMaturityRepository> _logger;

        public SavingsAccountMaturityRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<SavingsAccountMaturityRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<SavingsAccountMaturityData> GetReportDataAsync(SavingsAccountMaturityRequestDto request)
        {
            try
            {
                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);
                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                var sqlFilterExp = new StringBuilder();

                sqlFilterExp.Append($" AND Ao.MaturityOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");

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
                // Column mapping matches the SP's actual SELECT list exactly:
                //   MemberId, Name, ContactNo (PhoneNo;MobileNo), AccountNo,
                //   DepositTypeName, InterestRate, AccountOpenOnBs,
                //   NextInterestDateOnBs, MaturityOnBs, LedgerBalance, AccountStatus
                // There is no DepositAmount / MaturityAmount / Balance / Remarks
                // column in this SP — those were incorrect guesses in the
                // previous mapping, which is why Balance always came back 0.00.
                // ---------------------------------------------------------------
                var resultList = rows.Select(r => new SavingsAccountMaturityRowDto
                {
                    DepositTypeName = r.DepositTypeName,
                    MemberId = r.MemberId,
                    MemberName = r.Name,
                    Phone = r.ContactNo,
                    AccountNo = r.AccountNo,
                    AccountOpenOnBs = r.AccountOpenOnBs,
                    MaturityOnBs = r.MaturityOnBs,
                    Balance = r.LedgerBalance,
                    InterestRate = r.InterestRate
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

                //var branchName = await GetOfficeNameByIdAsync(request.BranchIds);

                return new SavingsAccountMaturityData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalDepositAmount = resultList.Sum(r => r.Balance ?? 0),
                    TotalMaturityAmount = resultList.Sum(r => r.Balance ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    OrderBy = request.OrderBy,
                    DepositTypeName = depositTypeName,
                    TotalDepositTypes = totalDepositTypes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync for Savings Account Maturity Report");
                throw;
            }
        }
        private static string BuildOrderByClause(string orderBy)
        {
            // Converted from the legacy switch in GetSavingAcMaturity. Note the
            // SP's SELECT list uses "Name" (not MemberName) for the member's
            // display name, so "Member Name" sorts by Name to match.
            return orderBy switch
            {
                "Deposit Type" or "Savings Type" => " ORDER BY DepositTypeName",
                "Member Id" or "Member ID" => " ORDER BY substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId",
                "Member Name" => " ORDER BY Name",
                "Account No" => " ORDER BY substring(AccountNo, 1,(len(AccountNo)-charindex('-', AccountNo))-1), AccountNo",
                "Account Opened Date" => " ORDER BY AccountOpenOnBs",
                "Account Maturity Date" => " ORDER BY MaturityOnBs",
                _ => " ORDER BY MaturityOnBs"
            };
        }
    }
}