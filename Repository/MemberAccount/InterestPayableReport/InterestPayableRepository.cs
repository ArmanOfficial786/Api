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

namespace NexgenCosysReport.Repository.MemberAccount.InterestPayableReport
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
                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDateBs);
                var tillDateStr = tillDateAd.ToString("yyyy-MM-dd");

                var sqlFilterExp = new StringBuilder();

                if (!string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
                {
                    sqlFilterExp.Append($" AND a.UsmOfficeId IN ({request.BranchIds})");
                }

                // DepositTypeName is the primary sort key so rows arrive grouped by deposit
                // type first, matching the report's visual grouping (view's GroupBy
                // preserves first-seen order, does not sort).
                var orderByClause = BuildOrderByClause(request.OrderBy);

                string spName;
                if (request.ReportView == "P")
                {
                    spName = "sp_5_43_GetInterestCalculationDailyReceivableOnlyPayableOnTillDate";
                }
                else
                {
                    spName = "sp_5_43_GetInterestCalculationDailyReceivable";
                }

                var parameters = new DynamicParameters();
                parameters.Add("@SqlInterestDate", tillDateStr, DbType.String);
                parameters.Add("@SqlInterestDateBS", request.TillDateBs, DbType.String);
                parameters.Add("@SqlOfficeId", request.BranchIds, DbType.String);
                parameters.Add("@SqlUserId", "-1", DbType.String);
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString() + orderByClause, DbType.String, size: -1);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var rows = await connection.QueryAsync<InterestPayableRowDto>(
                    spName,
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var resultList = rows.AsList();

                var totalDepositTypes = resultList
                    .Select(r => r.DepositTypeName)
                    .Distinct()
                    .Count();

                var reportViewName = request.ReportView == "P" ? "Only On Till Date" : "All";

                // Root-cause fix: resolve the real office name from OfficeId instead of
                // echoing request.BranchName back — same pattern as DataEditedReport,
                // SavingAccountDeleted, SavingIssue, SavingAccountClosed.
                var branchName = await GetOfficeNameByIdAsync(request.BranchIds);

                return new InterestPayableData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalInterest = resultList.Sum(r => r.InterestAmount ?? 0),
                    TotalTax = resultList.Sum(r => r.TaxAmount ?? 0),
                    TotalBalance = resultList.Sum(r => r.Balance ?? 0),
                    TillDateBs = request.TillDateBs,
                    OfficeName = branchName,
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

        /// <summary>
        /// Resolves a UsmOfficeId (or comma-separated list) into office name(s).
        /// Falls back to "All" when unfiltered. Same pattern used across the project.
        /// </summary>
        private async Task<string> GetOfficeNameByIdAsync(string? officeIdCsv)
        {
            if (string.IsNullOrWhiteSpace(officeIdCsv) || officeIdCsv == "-1")
            {
                return "All";
            }

            try
            {
                var ids = officeIdCsv
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(id => long.TryParse(id, out var parsed) ? parsed : (long?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .ToList();

                if (!ids.Any())
                {
                    return officeIdCsv;
                }

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                const string sql = "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN @Ids";
                var names = (await connection.QueryAsync<string>(sql, new { Ids = ids })).ToList();

                return names.Any() ? string.Join(", ", names) : officeIdCsv;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetOfficeNameByIdAsync");
                return officeIdCsv;
            }
        }

        private static string BuildOrderByClause(string orderBy)
        {
            return orderBy switch
            {
                "Deposit Type" => " ORDER BY DepositTypeName",
                "Member Id" => " ORDER BY DepositTypeName, substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId",
                "Member Name" => " ORDER BY DepositTypeName, MemberName",
                "Account No" => " ORDER BY DepositTypeName, substring(AccountNo, 1,(len(AccountNo)-charindex('-', AccountNo))-1), AccountNo",
                "Interest Rate" => " ORDER BY DepositTypeName, InterestRate DESC",
                "Interest Date From" => " ORDER BY DepositTypeName, InterestFrom",
                "Interest" => " ORDER BY DepositTypeName, InterestAmount DESC",
                "Tax" => " ORDER BY DepositTypeName, TaxAmount DESC",
                "Balance" => " ORDER BY DepositTypeName, Balance DESC",
                _ => " ORDER BY DepositTypeName, substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId"
            };
        }
    }
}