// Repositories/MemberAccount/InterestExpenseReport/InterestAndTaxPostedRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestExpenseReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.InterestExpenseReportInterface;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.MemberAccount.InterestExpenseReport
{
    public class InterestAndTaxPostedRepository : IInterestAndTaxPostedRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<InterestAndTaxPostedRepository> _logger;

        public InterestAndTaxPostedRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<InterestAndTaxPostedRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<InterestAndTaxPostedData> GetReportDataAsync(InterestAndTaxPostedRequestDto request)
        {
            try
            {
                // Convert Nepali dates to English
                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);
                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                // Build optimized filter expression
                var sqlFilterExp = BuildFilterExpression(fromDateStr, toDateStr, request.BranchIds);

                var orderByClause = BuildOrderByClause(request.OrderBy);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrder", orderByClause, DbType.String, size: -1);
                parameters.Add("@SqlTillDate", toDateStr, DbType.String);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);

                var rows = await connection.QueryAsync<InterestAndTaxPostedRowDto>(
                    "sp_5_43_GetInterestAndTaxPosted",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var resultList = rows.AsList();

                // Clean up InterestRate - remove percentage sign if present
                foreach (var row in resultList)
                {
                    if (!string.IsNullOrEmpty(row.InterestRate))
                    {
                        // Remove % sign and trim whitespace
                        row.InterestRate = row.InterestRate.Replace("%", "").Trim();
                    }
                }

                // Calculate totals
                var (totalInterest, totalTax, totalNetBalance) = CalculateTotals(resultList);

                var totalDepositTypes = resultList
                    .Select(r => r.DepositTypeName)
                    .Distinct()
                    .Count();

                return new InterestAndTaxPostedData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalInterest = totalInterest,
                    TotalTax = totalTax,
                    TotalNetBalance = totalNetBalance,
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchNames = request.BranchName,
                    OrderBy = request.OrderBy,
                    TotalDepositTypes = totalDepositTypes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync for Interest and Tax Posted Report");
                throw;
            }
        }

        private static string BuildFilterExpression(string fromDateStr, string toDateStr, string branchIds)
        {
            var filter = new StringBuilder();
            filter.Append($" AND At.TransactionOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");

            if (!string.IsNullOrEmpty(branchIds) && branchIds != "-1")
            {
                filter.Append($" AND At.UsmOfficeId IN ({branchIds})");
            }

            return filter.ToString();
        }

        private static (decimal totalInterest, decimal totalTax, decimal totalNetBalance) CalculateTotals(
            List<InterestAndTaxPostedRowDto> rows)
        {
            decimal totalInterest = 0;
            decimal totalTax = 0;
            decimal totalNetBalance = 0;

            foreach (var row in rows)
            {
                totalInterest += row.Interest ?? 0;
                totalTax += row.Tax ?? 0;
                totalNetBalance += row.NetBalance ?? 0;
            }

            return (totalInterest, totalTax, totalNetBalance);
        }

        private static string BuildOrderByClause(string orderBy)
        {
            return orderBy switch
            {
                "Deposit Type" => " ORDER BY DepositTypeName",
                "Member Id" => " ORDER BY substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId",
                "Member Name" => " ORDER BY MemberName",
                "Account No" => " ORDER BY substring(AccountNo, 1,(len(AccountNo)-charindex('-', AccountNo))-1), AccountNo",
                "Interest Rate" => " ORDER BY InterestRate DESC",
                "Interest Date" => " ORDER BY InterestDate",
                "Interest" => " ORDER BY Interest DESC",
                "Tax" => " ORDER BY Tax DESC",
                "Net Balance" => " ORDER BY NetBalance DESC",
                _ => " ORDER BY InterestDate"
            };
        }
    }
}