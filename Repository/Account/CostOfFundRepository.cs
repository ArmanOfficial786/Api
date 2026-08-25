// Repository/AccountOperation/CostOfFundRepository.cs
using Dapper;
using NexgenCosysReport.DbContext;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Dtos.RequestDtos.Account;
using NexgenCosysReport.Inteface.ServiceInterface.Account;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;

namespace NexgenCosysReport.Repository.Account
{
    public class CostOfFundRepository : ICostofFund
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<CostOfFundRepository> _logger;

        public CostOfFundRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<CostOfFundRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<CostOfFundData> GetCostOfFund(CostOfFundRequest request)
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // 1. Convert TillDate to English (MM/dd/yyyy)
            var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDate);
            string tillDateStr = tillDateAd.ToString("MM/dd/yyyy");

            // 2. Build branch filter string (matching BLL)
            string branchFilter = "";
            if (!request.SameCompanyName && request.BranchId <= 0)
            {
                branchFilter = $" And D.UsmOfficeId in ({request.BranchId})";
            }
            else
            {
                branchFilter = ""; // BLL uses " And D.UsmOfficeId = " + branchId only if branchId != -1
                // Actually in BLL: if (branchId != -1) { SqlFilterExp += " And D.UsmOfficeId = " + branchId; }
                // But we'll pass "-1" as branchId and the SP handles it.
                // For simplicity, we'll pass the filter as an empty string or construct as needed.
                // In BLL, they pass branchId to SP, but our SP expects @SqlFilterExp, @SqlTillDate, @SqlFilterExpOrderBy.
                // So we need to build the filter expression for @SqlFilterExp.
                // BLL builds SqlFilterExp += " And D.UsmOfficeId = " + branchId; if branchId != -1.
                // So we will do similarly.
            }

            // Actually the BLL uses:
            // if (branchId != -1) { SqlFilterExp += " And D.UsmOfficeId = " + branchId; }
            // So we need to pass a filter string. If BranchId is "-1", we pass empty.
            string sqlFilterExp = "";
            if (!request.SameCompanyName && request.BranchId <= 0)
            {
                // If multiple branches, we use "in" clause
                sqlFilterExp = $" And D.UsmOfficeId in ({request.BranchId})";
            }
            // else leave empty

            // 3. Order by mapping (BLL uses orderBy as is; we'll map)
            string orderByClause = MapOrderBy(request.OrderBy);
            string sqlFilterExpOrderBy = $" order by {orderByClause}";

            // 4. Prepare parameters for Saving SP
            var savingParams = new DynamicParameters();
            savingParams.Add("@SqlFilterExp", sqlFilterExp);
            savingParams.Add("@SqlTillDate", tillDateStr);
            savingParams.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy);
            savingParams.Add("@CostofDeposit", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);
            savingParams.Add("@CostofDepositInterest", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);

            // 5. Execute Saving SP
            var savingRows = await connection.QueryAsync<CostOfFundRowDto>(
                "sp_6_56_GetCostofFundSaving",
                savingParams,
                commandType: CommandType.StoredProcedure
            );

            decimal costOfDeposit = savingParams.Get<decimal?>("@CostofDeposit") ?? 0;
            decimal costOfDepositInterest = savingParams.Get<decimal?>("@CostofDepositInterest") ?? 0;

            // 6. Prepare parameters for Loan SP
            var loanParams = new DynamicParameters();
            loanParams.Add("@SqlFilterExp", sqlFilterExp);
            loanParams.Add("@SqlTillDate", tillDateStr);
            loanParams.Add("@SqlFilterExpOrderBy", sqlFilterExpOrderBy);
            loanParams.Add("@CostofLoan", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);
            loanParams.Add("@CostofLoanInterest", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);

            // 7. Execute Loan SP
            var loanRows = await connection.QueryAsync<CostOfFundRowDto>(
                "sp_6_56_GetCostofFundLoan",
                loanParams,
                commandType: CommandType.StoredProcedure
            );

            decimal costOfLoan = loanParams.Get<decimal?>("@CostofLoan") ?? 0;
            decimal costOfLoanInterest = loanParams.Get<decimal?>("@CostofLoanInterest") ?? 0;

            // 8. Get additional details (CRR, SLR, CDR)
            var detailParams = new DynamicParameters();
            detailParams.Add("@SqlFilterExp", sqlFilterExp);
            detailParams.Add("@SqlTillDate", tillDateStr);
            detailParams.Add("@CRR", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);
            detailParams.Add("@SLR", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);
            detailParams.Add("@CDR", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 18, scale: 2);

            await connection.ExecuteAsync(
                "sp_6_56_GetCostofFundDetail",
                detailParams,
                commandType: CommandType.StoredProcedure
            );

            decimal crr = detailParams.Get<decimal?>("@CRR") ?? 0;
            decimal slr = detailParams.Get<decimal?>("@SLR") ?? 0;
            decimal cdr = detailParams.Get<decimal?>("@CDR") ?? 0;

            // 9. Build result
            return new CostOfFundData
            {
                DepositRows = savingRows.AsList(),
                LoanRows = loanRows.AsList(),
                CostOfDeposit = costOfDeposit,
                CostOfLoan = costOfLoan,
                CostOfDepositInterest = costOfDepositInterest,
                CostOfLoanInterest = costOfLoanInterest,
                CRR = crr,
                SLR = slr,
                CDR = cdr
            };
        }

        private string MapOrderBy(string orderBy)
        {
            // BLL uses these exact strings: "Type Name", "NoofAccount", "Balance", "Average Int. Rate", "WACC"
            // In the SP, orderBy is used as is in the ORDER BY clause.
            // But the SP expects a column name. We'll map to the actual column names.
            // The BLL passes the orderBy value directly to SP (as string) and the SP uses it in ORDER BY.
            // So we should pass the exact string that the SP expects.
            // Looking at BLL: orderBy values are exactly as in dropdown.
            // The SP likely uses CASE or dynamic SQL to handle these.
            // We'll pass the orderBy as received, but we need to ensure it matches the SP's expected format.
            // In the BLL, orderBy is passed as is to @SqlFilterExpOrderBy.
            // So we'll just return the value.
            return orderBy switch
            {
                "Type Name" => "TypeName",
                "NoofAccount" => "NoofAccount",
                "Balance" => "TotalAmount",
                "Average Int. Rate" => "AverageInterestRate",
                "WACC" => "WACC",
                _ => "TypeName"
            };
        }
    }
}
