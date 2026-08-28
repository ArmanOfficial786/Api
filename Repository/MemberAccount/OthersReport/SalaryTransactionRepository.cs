// Repository/MemberAccount/OthersReport/SalaryTransactionRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.MemberAccount.OthersReport
{
    public class SalaryTransactionRepository : ISalaryTransactionRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<SalaryTransactionRepository> _logger;

        public SalaryTransactionRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<SalaryTransactionRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<SalaryTransactionData> GetReportDataAsync(SalaryTransactionRequestDto request)
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
                sqlFilterExp.Append($" AND ES.SalaryPaidOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");

                // Branch filter
                if (!string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
                {
                    sqlFilterExp.Append($" AND ES.UsmOfficeId IN ({request.BranchIds})");
                }

                // Staff filter
                if (request.StaffId.HasValue && request.StaffId.Value != -1)
                {
                    sqlFilterExp.Append($" AND HR.HurHumanResourceId = {request.StaffId.Value}");
                }

                // Transfer On filter
                if (!string.IsNullOrEmpty(request.TransferOn) && request.TransferOn != "A")
                {
                    sqlFilterExp.Append($" AND ES.SalaryTransferOn = '{request.TransferOn}'");
                }

                // Build Order By clause
                var orderByClause = BuildOrderByClause(request.OrderBy);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrder", orderByClause, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpIsSummary", request.ReportType, DbType.Int64);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var rows = await connection.QueryAsync<SalaryTransactionRowDto>(
                    "sp_5_43_GetSalaryTransaction",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var resultList = rows.AsList();

                // Get staff name if specific staff is selected
                string? selectedStaffName = null;
                if (request.StaffId.HasValue && request.StaffId.Value != -1)
                {
                    selectedStaffName = await GetStaffNameAsync(request.StaffId.Value);
                }

                // Calculate totals
                var totalSalary = resultList.Sum(r => r.SalaryAmount ?? 0);
                var totalTds = resultList.Sum(r => r.TdsAmount ?? 0);
                var totalAllowance = resultList.Sum(r => r.AllowanceAmount ?? 0);
                var totalPFFO = resultList.Sum(r => r.PFFO ?? 0);
                var totalPFFS = resultList.Sum(r => r.PFFS ?? 0);
                var totalOverTime = resultList.Sum(r => r.OverTimeSalaryAmount ?? 0);
                var totalLeaveDeduction = resultList.Sum(r => r.LeaveDeductedAmount ?? 0);
                var totalAdvanceDeduction = resultList.Sum(r => r.AdvanceDeductedAmount ?? 0);
                var totalNetSalary = resultList.Sum(r => r.NetSalary ?? 0);

                return new SalaryTransactionData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalSalaryAmount = totalSalary,
                    TotalTdsAmount = totalTds,
                    TotalAllowanceAmount = totalAllowance,
                    TotalPFFO = totalPFFO,
                    TotalPFFS = totalPFFS,
                    TotalOverTimeSalary = totalOverTime,
                    TotalLeaveDeduction = totalLeaveDeduction,
                    TotalAdvanceDeduction = totalAdvanceDeduction,
                    TotalNetSalary = totalNetSalary,
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchNames = request.BranchName,
                    OrderBy = request.OrderBy,
                    ReportType = request.ReportType,
                    TransferOn = request.TransferOn,
                    StaffSelection = request.StaffSelection,
                    SelectedStaffName = selectedStaffName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync for Salary Transaction Report");
                throw;
            }
        }

        private static string BuildOrderByClause(string orderBy)
        {
            return orderBy switch
            {
                "Staff Name" => " ORDER BY MemberName",
                "Account No" => " ORDER BY substring(AccountNo, 1,(len(AccountNo)-charindex('-', AccountNo))-1), AccountNo",
                "Salary Amount" => " ORDER BY SalaryAmount DESC",
                "Tds Amount" => " ORDER BY TdsAmount DESC",
                "Allowance Amount" => " ORDER BY AllowanceAmount DESC",
                "Provident Fund From Office" => " ORDER BY PFFO DESC",
                "Provident Fund From Salary" => " ORDER BY PFFS DESC",
                "Net Salary Amount" => " ORDER BY NetSalary DESC",
                "Over Time Salary" => " ORDER BY OverTimeSalaryAmount DESC",
                "Leave Deduction" => " ORDER BY LeaveDeductedAmount DESC",
                "Advance Deduction" => " ORDER BY AdvanceDeductedAmount DESC",
                _ => " ORDER BY MemberName"
            };
        }

        private async Task<string?> GetStaffNameAsync(long staffId)
        {
            try
            {
                const string query = @"
                    SELECT FullName 
                    FROM HurHumanResource 
                    WHERE HurHumanResourceId = @StaffId AND IsActive = 1";

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                return await connection.QueryFirstOrDefaultAsync<string>(
                    query,
                    new { StaffId = staffId }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting staff name for ID: {StaffId}", staffId);
                return null;
            }
        }
    }
}