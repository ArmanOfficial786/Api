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

namespace NexgenCosysReport.Repositories.MemberAccount.SavingsAccountMaturityReport
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
                // Convert Nepali dates to English
                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);
                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                // Build filter expression
                var sqlFilterExp = new StringBuilder();

                // Date filter
                sqlFilterExp.Append($" AND Ao.MaturityOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");

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

                // Build Order By clause (converted from legacy switch in GetSavingAcMaturity)
                var orderByClause = BuildOrderByClause(request.OrderBy);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var rows = await connection.QueryAsync<SavingsAccountMaturityRowDto>(
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

                return new SavingsAccountMaturityData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalDepositAmount = resultList.Sum(r => r.DepositAmount ?? 0),
                    TotalMaturityAmount = resultList.Sum(r => r.MaturityAmount ?? 0),
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
                _logger.LogError(ex, "Error in GetReportDataAsync for Savings Account Maturity Report");
                throw;
            }
        }

        private static string BuildOrderByClause(string orderBy)
        {
            // Converted from legacy switch in GetSavingAcMaturity
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