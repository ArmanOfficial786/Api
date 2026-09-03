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

namespace NexgenCosysReport.Repositories.MemberAccount.SavingsAccountInterestTransfer
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
                // Convert Nepali dates to English
                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);
                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                // Build filter expression
                var sqlFilterExp = new StringBuilder();

                // Date filter - using NextInterestDateOn
                sqlFilterExp.Append($" AND Ao.NextInterestDateOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");

                // Branch filter
                if (!string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
                {
                    sqlFilterExp.Append($" AND Ao.UsmOfficeId IN ({request.BranchIds})");
                }

                // Deposit Type filter
                if (request.DepositTypeId != -1)
                {
                    sqlFilterExp.Append($" AND Ao.SycDepositTypeId = {request.DepositTypeId}");
                }

                // Build Order By clause (converted from legacy switch in GetSavingAcInterestTransfer)
                var orderByClause = BuildOrderByClause(request.OrderBy);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var rows = await connection.QueryAsync<SavingsAccountInterestTransferRowDto>(
                    "sp_5_43_GetSavingAcMaturityAndInterestTransfer",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var resultList = rows.AsList();

                // Get unique deposit types count
                var totalDepositTypes = resultList
                    .Select(r => r.DepositTypeName)
                    .Distinct()
                    .Count();

                // Get deposit type name if a specific type is selected
                string depositTypeName = string.Empty;
                if (request.DepositTypeId != -1 && resultList.Any())
                {
                    depositTypeName = resultList.First().DepositTypeName ?? string.Empty;
                }

                return new SavingsAccountInterestTransferData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
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
            // Converted from legacy switch in GetSavingAcInterestTransfer
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