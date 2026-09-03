// Repositories/MemberAccount/InterestExpenseReport/InterestAndTaxDetailRepository.cs
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
    public class InterestAndTaxDetailRepository : IInterestAndTaxDetailRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<InterestAndTaxDetailRepository> _logger;

        public InterestAndTaxDetailRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<InterestAndTaxDetailRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<InterestAndTaxDetailData> GetReportDataAsync(InterestAndTaxDetailRequestDto request)
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
                sqlFilterExp.Append($" AND At.TransactionOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");

                // Branch filter
                if (!string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
                {
                    sqlFilterExp.Append($" AND At.UsmOfficeId IN ({request.BranchIds})");
                }

                // Member filter
                if (request.MemberRegistrationId != -1)
                {
                    sqlFilterExp.Append($" AND At.MemMemberRegistrationId = {request.MemberRegistrationId}");
                }

                // Build Order By clause (converted from legacy switch)
                var orderByClause = BuildOrderByClause(request.OrderBy);

                // Use the same stored procedure as legacy
                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrder", orderByClause, DbType.String, size: -1);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var rows = await connection.QueryAsync<InterestAndTaxDetailRowDto>(
                    "sp_5_43_GetInterestAndTaxDetails",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var resultList = rows.AsList();

                // Get unique deposit types count
                var totalDepositTypes = resultList
                    .Select(r => r.DepositTypeName)
                    .Distinct()
                    .Count();

                return new InterestAndTaxDetailData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalInterest = resultList.Sum(r => r.Interest ?? 0),
                    TotalTax = resultList.Sum(r => r.Tax ?? 0),
                    TotalNetAmount = resultList.Sum(r => r.NetAmount ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchNames = request.BranchName,
                    OrderBy = request.OrderBy,
                    MemberId = request.MemberId,
                    MemberName = request.MemberName,
                    TotalDepositTypes = totalDepositTypes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync for Interest and Tax Detail Report");
                throw;
            }
        }

        private static string BuildOrderByClause(string orderBy)
        {
            // Converted from legacy switch statement in GetInterestAndTaxDetail
            return orderBy switch
            {
                "Deposit Type" => " ORDER BY DepositTypeName",
                "Member Id" => " ORDER BY substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId",
                "Member Name" => " ORDER BY MemberName",
                "Account No" => " ORDER BY substring(AccountNo, 1,(len(AccountNo)-charindex('-', AccountNo))-1), AccountNo",
                "Particulars" => " ORDER BY Narration",
                "Interest" => " ORDER BY Interest DESC",
                "Tax" => " ORDER BY Tax DESC",
                "Date" => " ORDER BY Date",
                _ => " ORDER BY Date"
            };
        }
    }
}