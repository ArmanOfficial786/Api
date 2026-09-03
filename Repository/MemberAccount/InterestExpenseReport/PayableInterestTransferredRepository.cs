// Repositories/MemberAccount/InterestExpenseReport/PayableInterestTransferredRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestExpenseReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.InterestExpenseReportInterface;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repositories.MemberAccount.InterestExpenseReport
{
    public class PayableInterestTransferredRepository : IPayableInterestTransferredRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<PayableInterestTransferredRepository> _logger;

        public PayableInterestTransferredRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<PayableInterestTransferredRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<PayableInterestTransferredData> GetReportDataAsync(PayableInterestTransferredRequestDto request)
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
                sqlFilterExp.Append($" AND a.TransactionOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");

                // Branch filter
                if (!string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
                {
                    sqlFilterExp.Append($" AND a.UsmOfficeId IN ({request.BranchIds})");
                }

                // Build Order By clause (converted from legacy switch in GetPayableInterestTransferred)
                var orderByClause = BuildOrderByClause(request.OrderBy);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var rows = await connection.QueryAsync<PayableInterestTransferredRowDto>(
                    "sp_5_43_GetPayableInterestTransferred",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var resultList = rows.AsList();

                // Get unique deposit types count
                var totalDepositTypes = resultList
                    .Select(r => r.DepositTypeName)
                    .Distinct()
                    .Count();

                return new PayableInterestTransferredData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalInterest = resultList.Sum(r => r.Interest ?? 0),
                    TotalTax = resultList.Sum(r => r.Tax ?? 0),
                    TotalNetBalance = resultList.Sum(r => r.NetBalance ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchNames = request.BranchName,
                    OrderBy = request.OrderBy,
                    TotalDepositTypes = totalDepositTypes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync for Payable Interest Transferred Report");
                throw;
            }
        }

        private static string BuildOrderByClause(string orderBy)
        {
            // Converted from legacy switch in GetPayableInterestTransferred
            return orderBy switch
            {
                "Deposit Type" => " ORDER BY DepositTypeName",
                "Member Id" => " ORDER BY substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId",
                "Account No" => " ORDER BY substring(AccountNo, 1,(len(AccountNo)-charindex('-', AccountNo))-1), AccountNo",
                "Interest Rate" => " ORDER BY InterestRate DESC",
                "Interest Date" => " ORDER BY InterestDate",
                "Interest" => " ORDER BY Interest DESC",
                "Tax" => " ORDER BY Tax DESC",
                "Net Balance" => " ORDER BY NetBalance DESC",
                _ => " ORDER BY MemberId"
            };
        }
    }
}