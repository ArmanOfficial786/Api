// Repository/MemberAccount/InterestExpenseReport/FixedDepositInterestTransferRepository.cs
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
    public class FixedDepositInterestTransferRepository : IFixedDepositInterestTransferRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<FixedDepositInterestTransferRepository> _logger;

        public FixedDepositInterestTransferRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<FixedDepositInterestTransferRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<FixedDepositInterestTransferData> GetReportDataAsync(FixedDepositInterestTransferRequestDto request)
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

                // Build Order By clause
                var orderByClause = BuildOrderByClause(request.OrderBy);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrder", orderByClause, DbType.String, size: -1);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var rows = await connection.QueryAsync<FixedDepositInterestTransferRowDto>(
                    "sp_5_43_GetFixedDepositInterestTransferReport",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var resultList = rows.AsList();

                return new FixedDepositInterestTransferData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalInterestAmount = resultList.Sum(r => r.InterestAmount ?? 0),
                    TotalTaxAmount = resultList.Sum(r => r.TaxAmount ?? 0),
                    TotalNetAmount = resultList.Sum(r => r.NetAmount ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchNames = request.BranchName,
                    OrderBy = request.OrderBy,
                    TotalTransactions = resultList.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync for Fixed Deposit Interest Transfer Report");
                throw;
            }
        }

        private static string BuildOrderByClause(string orderBy)
        {
            return orderBy switch
            {
                "MemberId" => " ORDER BY substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId",
                "MemberName" => " ORDER BY MemberName",
                "AccountNo" => " ORDER BY substring(AccountNo, 1,(len(AccountNo)-charindex('-', AccountNo))-1), AccountNo",
                "InterestDate" => " ORDER BY InterestDate",
                "Interest" => " ORDER BY Interest DESC",
                "Tax" => " ORDER BY Tax DESC",
                "Remarks" => " ORDER BY Remarks",
                _ => " ORDER BY MemberId"
            };
        }
    }
}