// Repository/Microfinance/MicrofinanceSheetReports/CenterCollectionSheetAccountWiseRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Microfinance.MicrofinanceSheetReports;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Microfinance.MicrofinanceSheetReports;
using System.Data;

namespace NexgenCosysReport.Repository.Microfinance.MicrofinanceSheetReports
{
    public class CenterCollectionSheetAccountWiseRepository : ICenterCollectionSheetAccountWiseRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<CenterCollectionSheetAccountWiseRepository> _logger;

        public CenterCollectionSheetAccountWiseRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<CenterCollectionSheetAccountWiseRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        private static string SanitizeId(string? id)
        {
            if (string.IsNullOrWhiteSpace(id) || id == "-1" || id == "string")
                return "0";

            return long.TryParse(id, out var v) ? v.ToString() : "0";
        }

        public async Task<CenterCollectionSheetAccountWiseData> GetReportDataAsync(CenterCollectionSheetAccountWiseRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.TillDateBs) || request.TillDateBs == "-1")
                    throw new ArgumentException("Till Date is required.");

                var collectionCenterId = SanitizeId(request.CollectionCenterId);
                if (collectionCenterId == "0")
                    throw new ArgumentException("Please select a Collection Center.");

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDateBs);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlTillDate", tillDateAd.ToString("MM-dd-yyyy"), DbType.String, size: -1);
                parameters.Add("@SqlCollectionCenterId", collectionCenterId, DbType.String, size: -1);

                using var multi = await connection.QueryMultipleAsync(
                    "sp_5_43_GetCenterCollectionSheetAccountTypeWise",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 600);

                // Result set 1: Member + Loan summary
                var members = (await multi.ReadAsync<CenterCollectionSheetAccountMemberDto>()).AsList();

                // Result set 2: Member receivable detail
                var receivables = (await multi.ReadAsync<CenterCollectionSheetAccountReceivableDto>()).AsList();

                // Result set 3: Saving summary
                var savingSummary = (await multi.ReadAsync<CenterCollectionSheetAccountSavingSummaryDto>()).AsList();

                // Result set 4: Loan summary
                var loanSummary = (await multi.ReadAsync<CenterCollectionSheetAccountLoanSummaryDto>()).AsList();

                // Result set 5: Evaluation summary (single row)
                var evaluation = await multi.ReadFirstOrDefaultAsync<CenterCollectionSheetAccountEvaluationDto>();

                string? collectionCenterName = null;
                if (collectionCenterId != "0")
                {
                    collectionCenterName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT CollectionCenterName FROM SycCollectionCenter WHERE SycCollectionCenterId = @Id",
                        new { Id = long.Parse(collectionCenterId) });
                }

                return new CenterCollectionSheetAccountWiseData
                {
                    Members = members,
                    Receivables = receivables,
                    SavingSummary = savingSummary,
                    LoanSummary = loanSummary,
                    Evaluation = evaluation,

                    TotalMembers = members.Count,
                    TotalSaving1 = members.Sum(m => m.Saving1 ?? 0),
                    TotalSaving2 = members.Sum(m => m.Saving2 ?? 0),
                    TotalSaving3 = members.Sum(m => m.Saving3 ?? 0),
                    TotalSaving4 = members.Sum(m => m.Saving4 ?? 0),
                    TotalSaving5 = members.Sum(m => m.Saving5 ?? 0),
                    TotalSaving6 = members.Sum(m => m.Saving6 ?? 0),
                    TotalShare = members.Sum(m => m.Share ?? 0),
                    TotalLoan1 = members.Sum(m => m.Loan1 ?? 0),
                    TotalLoan2 = members.Sum(m => m.Loan2 ?? 0),
                    TotalLoan3 = members.Sum(m => m.Loan3 ?? 0),
                    TotalLoan4 = members.Sum(m => m.Loan4 ?? 0),
                    TotalLoan5 = members.Sum(m => m.Loan5 ?? 0),
                    TotalLoan6 = members.Sum(m => m.Loan6 ?? 0),
                    TotalReceivable = receivables.Sum(r => r.ReceivableAmt ?? 0),

                    TillDateBs = request.TillDateBs,
                    TillDateAd = tillDateAd.ToString("yyyy-MM-dd"),
                    CollectionCenterName = collectionCenterName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync (CenterCollectionSheetAccountWise)");
                throw;
            }
        }
    }
}