// Repositories/MemberAccount/InterestPayableReport/InterestPayableRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestPayableReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.InterestPayableReport;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repositories.MemberAccount.InterestPayableReport
{
    public class InterestPayableRepository : IInterestPayableRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<InterestPayableRepository> _logger;

        public InterestPayableRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<InterestPayableRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<InterestPayableData> GetReportDataAsync(InterestPayableRequestDto request)
        {
            try
            {
                // Convert Nepali date to English
                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDateBs);
                var tillDateStr = tillDateAd.ToString("yyyy-MM-dd");

                // Build filter expression
                var sqlFilterExp = new StringBuilder();

                // Office filter
                if (!string.IsNullOrEmpty(request.OfficeId) && request.OfficeId != "-1")
                {
                    sqlFilterExp.Append($" AND a.UsmOfficeId = {request.OfficeId}");
                }

                // Build Order By clause
                var orderByClause = BuildOrderByClause(request.OrderBy);

                // Determine which stored procedure to use based on report view
                string spName;
                if (request.ReportView == "P") // Only On Till Date
                {
                    spName = "sp_5_43_GetInterestCalculationDailyReceivableOnlyPayableOnTillDate";
                }
                else // All (default)
                {
                    spName = "sp_5_43_GetInterestCalculationDailyReceivable";
                }

                var parameters = new DynamicParameters();
                parameters.Add("@SqlInterestDate", tillDateStr, DbType.String);
                parameters.Add("@SqlInterestDateBS", request.TillDateBs, DbType.String);
                parameters.Add("@SqlOfficeId", request.OfficeId, DbType.String);
                parameters.Add("@SqlUserId", "-1", DbType.String); // UserId filter (optional)
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var rows = await connection.QueryAsync<InterestPayableRowDto>(
                    spName,
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var resultList = rows.AsList();

                // Get unique deposit types count
                var totalDepositTypes = resultList
                    .Select(r => r.DepositTypeName)
                    .Distinct()
                    .Count();

                // Determine report view name
                var reportViewName = request.ReportView == "P" ? "Only On Till Date" : "All";

                return new InterestPayableData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalInterest = resultList.Sum(r => r.InterestAmount ?? 0),
                    TotalTax = resultList.Sum(r => r.TaxAmount ?? 0),
                    TotalBalance = resultList.Sum(r => r.Balance ?? 0),
                    TillDateBs = request.TillDateBs,
                    OfficeName = request.OfficeName,
                    OrderBy = request.OrderBy,
                    ReportView = request.ReportView,
                    ReportViewName = reportViewName,
                    TotalDepositTypes = totalDepositTypes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync for Interest Payable Report");
                throw;
            }
        }

        private static string BuildOrderByClause(string orderBy)
        {
            // Converted from legacy switch in GetPayableInterestCalculation
            return orderBy switch
            {
                "Deposit Type" => " ORDER BY DepositTypeName",
                "Member Id" => " ORDER BY substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId",
                "Member Name" => " ORDER BY MemberName",
                "Account No" => " ORDER BY substring(AccountNo, 1,(len(AccountNo)-charindex('-', AccountNo))-1), AccountNo",
                "Interest Rate" => " ORDER BY InterestRate DESC",
                "Interest Date From" => " ORDER BY InterestFrom",
                "Interest" => " ORDER BY InterestAmount DESC",
                "Tax" => " ORDER BY TaxAmount DESC",
                "Balance" => " ORDER BY Balance DESC",
                _ => " ORDER BY MemberId"
            };
        }
    }
}