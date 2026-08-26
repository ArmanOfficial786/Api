// Repository/AccountOperation/OfficeProgressRepository.cs
using Dapper;
using NexgenCosysReport.DbContext;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;
using NexgenCosysReport.Dtos.RequestDtos.Account.AccountingReport;
using NexgenCosysReport.Inteface.ServiceInterface.Account.AccountingReport;

namespace NexgenCosysReport.Repository.Account.AccountingReport
{
    public class OfficeProgressRepository : IOfficeProgress
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<OfficeProgressRepository> _logger;

        public OfficeProgressRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<OfficeProgressRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<OfficeProgressData> GetOfficeProgress(OfficeProgressRequest request)
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Convert Nepali date to English (DateTime)
            var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDate);

            // Build branch filter
            string branchFilter = "";
            if (!request.SameCompanyName && !string.IsNullOrEmpty(request.BranchId) && request.BranchId != "-1")
            {
                branchFilter = request.BranchId;
            }

            // Select SP based on report type
            string spName = GetStoredProcedureName(request.ReportType);

            var parameters = new DynamicParameters();
            parameters.Add("@TillDate", tillDateAd);
            parameters.Add("@BranchIds", branchFilter);
            parameters.Add("@IsLoanMaturity1to30", request.Enable1to30Days);
            parameters.Add("@ProvisionType", request.ProvisionType);

            // For Saving and Loan reports, we need additional parameters
            if (request.ReportType == "Saving" || request.ReportType == "Loan")
            {
                // The original uses different SPs for Saving and Loan
                // We'll handle them separately
            }

            var rows = await connection.QueryAsync<OfficeProgressRowDto>(
                spName,
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var list = rows.AsList();
            return new OfficeProgressData
            {
                Rows = list,
                GrandTotal = list.Sum(r => r.Amount)
            };
        }

        private string GetStoredProcedureName(string reportType)
        {
            return reportType switch
            {
                "Saving" => "sp_5_43_GetSavingSummaryReport",
                "Loan" => "sp_7_16_LoanSummaryReportForMicrofinance",
                _ => "sp_6_113_OfficeProgressReport"  // Office Progress
            };
        }
    }
}
