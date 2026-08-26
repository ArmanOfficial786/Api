//// Repository/AccountOperation/MemberAccountDeactiveRepository.cs
//using Dapper;
using NexgenCosysReport.DbContext;
//using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore;
//using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount;
//using System.Data;

//namespace NexgenCosysReport.Repository.MemberAccount
//{
//    public class MemberAccountDeactiveRepository : IMemberAccDeactive
//    {
//        private readonly AppDbContext _context;
//        private readonly IDateConverterService _dateConverter;
//        private readonly ILogger<MemberAccountDeactiveRepository> _logger;

//        public MemberAccountDeactiveRepository(
//            AppDbContext context,
//            IDateConverterService dateConverter,
//            ILogger<MemberAccountDeactiveRepository> logger)
//        {
//            _context = context;
//            _dateConverter = dateConverter;
//            _logger = logger;
//        }

//        public async Task<MemberAccountDeactiveData> GetMemberAccountDeactive(MemberAccountDeactiveRequest request)
//        {
//            var connectionString = _context.Database.GetConnectionString();
//            using var connection = new SqlConnection(connectionString);
//            await connection.OpenAsync();

//            // Convert Nepali date to English
//            string tillDateStr = "";
//            if (!string.IsNullOrEmpty(request.TillDate) && request.TillDate != "-1")
//            {
//                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDate);
//                tillDateStr = tillDateAd.ToString("MM/dd/yyyy");
//            }
//            string typeIdStr = request.TypeId == 0 ? "-1" : request.TypeId.ToString();
//            // Build branch filter
//            string branchFilter = "-1";
//            if (!string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
//            {
//                branchFilter = request.BranchIds;
//            }

//            string orderByClause = MapOrderBy(request.OrderBy);

//            // Determine SP based on transaction type
//            string spName = request.TransactionType == "S"
//                ? "sp_5_43_GetMemberAccountDeactiveSavingReport"
//                : "sp_5_43_GetMemberAccountDeactiveLoanReport";

//            var parameters = new DynamicParameters();
//            parameters.Add("@SqlFilterExp", BuildFilterExpression(branchFilter, typeIdStr));
//            parameters.Add("@SqlFilterExpTill", tillDateStr);
//            parameters.Add("@SqlFilterExpDuePeriod", request.DuePeriod.ToString());
//            parameters.Add("@SqlFilterExpOrderBy", orderByClause);
//            parameters.Add("@IsActive", request.IsActive);

//            _logger.LogInformation(
//                "SP: {SpName}, Filter: {Filter}, OrderBy: {OrderBy}, Till: {Till}, DuePeriod: {DuePeriod}",
//                spName, typeIdStr, orderByClause, tillDateStr, request.DuePeriod);

//            var rows = await connection.QueryAsync<MemberAccountDeactiveRowDto>(
//                spName,
//                parameters,
//                commandType: CommandType.StoredProcedure
//            );

//            var list = rows.AsList();

//            return new MemberAccountDeactiveData
//            {
//                Rows = list,
//                TotalRecords = list.Count,
//                ActiveCount = list.Count(r => r.Status?.Equals("Active", StringComparison.OrdinalIgnoreCase) == true),
//                InactiveCount = list.Count(r => r.Status?.Equals("Inactive", StringComparison.OrdinalIgnoreCase) == true)
//            };
//        }

//        private string BuildFilterExpression(string branchFilter, string? typeId)
//        {
//            var filters = new List<string>();

//            if (branchFilter != "-1")
//                filters.Add($" And M.UsmOfficeId in ({branchFilter})");

//            if (!string.IsNullOrEmpty(typeId) && typeId != "-1")
//            {

//                filters.Add($" And A.SycDepositTypeId = {typeId}");
//            }

//            return string.Join(" ", filters);
//        }

//        private string MapOrderBy(string orderBy)
//        {
//            string clause = orderBy switch
//            {
//                "Member Name" => " order by MemberName",
//                "MemberId" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId",
//                "Account No" => " order by substring(AccountNo, 1,(len(AccountNo)-charindex('-', AccountNo))-1), AccountNo",
//                "Type" => " order by Type",
//                "LastDate" => " order by LastTransactionDate DESC",
//                "Age" => " order by Age",
//                _ => " order by MemberName"
//            };

//            return clause;
//        }
//    }
//}





// Repository/AccountOperation/MemberAccountDeactiveRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.SavingAcWiseReport;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.SavingAcWiseReport;

namespace NexgenCosysReport.Repository.MemberAccount.SavingAcWiseReport
{
    public class MemberAccountDeactiveRepository : IMemberAccDeactive
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<MemberAccountDeactiveRepository> _logger;

        public MemberAccountDeactiveRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<MemberAccountDeactiveRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<MemberAccountDeactiveData> GetMemberAccountDeactive(MemberAccountDeactiveRequest request)
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Convert Nepali date to English
            // Webform: tillDate = ncpTillDateOnBS.ShortNepaliDate, always populated (DefaultDate="True",
            // RequiredValidation="true" on the picker) — the "-1" skip only matters if the API caller
            // omits it, which the aspx never did.
            string tillDateStr = "";
            if (!string.IsNullOrEmpty(request.TillDate) && request.TillDate != "-1")
            {
                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDate);
                tillDateStr = tillDateAd.ToString("MM/dd/yyyy");
            }

            // Webform: typeId = Convert.ToInt64(ddlType.SelectedValue); Utils.AddSelectInDropDrownAllList
            // adds an "All" item whose value is 0, so 0 from the UI means "no type filter" == "-1".
            string typeIdStr = request.TypeId == 0 ? "-1" : request.TypeId.ToString();

            // Webform: branchId = -1 when TotalCount == count (i.e. all branches selected/"Select All"
            // checked), otherwise branchSelected is the comma-joined list of checked UsmOfficeIds.
            // No SameCompanyName concept in the aspx — branch filtering is purely from the checkbox list.
            string branchFilter = "-1";
            if (!string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
            {
                branchFilter = request.BranchIds;
            }

            // Map order by — MUST include the "order by" keyword itself, because
            // the SP concatenates @SqlFilterExpOrderBy directly into its dynamic SQL
            // (same convention as every other report SP in this system, e.g.
            // sp_5_43_GetAllDepositStatementVerifiedUnVerifiedReport's @SqlFilterExpOrder).
            // Sending a bare column name like "MemberName" produces invalid SQL:
            //     ...) AS Temp MemberName   -->  "Incorrect syntax near 'MemberName'"
            string orderByClause = MapOrderBy(request.OrderBy);

            // Determine SP based on transaction type (webform: rbtnTransactionType.SelectedValue, "S"/"L")
            string spName = request.TransactionType == "S"
                ? "sp_5_43_GetMemberAccountDeactiveSavingReport"
                : "sp_5_43_GetMemberAccountDeactiveLoanReport";

            var parameters = new DynamicParameters();
            parameters.Add("@SqlFilterExp", BuildFilterExpression(branchFilter, typeIdStr, request.TransactionType));
            parameters.Add("@SqlFilterExpTill", tillDateStr);
            parameters.Add("@SqlFilterExpDuePeriod", request.DuePeriod.ToString());
            parameters.Add("@SqlFilterExpOrderBy", orderByClause);
            parameters.Add("@IsActive", request.IsActive);

            _logger.LogInformation(
                "SP: {SpName}, Filter: {Filter}, OrderBy: {OrderBy}, Till: {Till}, DuePeriod: {DuePeriod}",
                spName, typeIdStr, orderByClause, tillDateStr, request.DuePeriod);

            var rows = await connection.QueryAsync<MemberAccountDeactiveRowDto>(
                spName,
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var list = rows.AsList();

            return new MemberAccountDeactiveData
            {
                Rows = list,
                TotalRecords = list.Count,
                ActiveCount = list.Count(r => r.Status?.Equals("Active", StringComparison.OrdinalIgnoreCase) == true),
                InactiveCount = list.Count(r => r.Status?.Equals("Inactive", StringComparison.OrdinalIgnoreCase) == true)
            };
        }

        private string BuildFilterExpression(string branchFilter, string? typeId, string transactionType)
        {
            var filters = new List<string>();

            if (branchFilter != "-1")
                filters.Add($" And M.UsmOfficeId in ({branchFilter})");

            if (!string.IsNullOrEmpty(typeId) && typeId != "-1")
            {
                // Webform's LoadDdlType binds ddlType differently per transaction type:
                //   Saving -> DataValueField = "SycDepositTypeId"  (CSycDepositType.GetAllActive)
                //   Loan   -> DataValueField = "LmtLoanTypeMasterId" (CLmtLoanTypeMaster.GetAll)
                // so the value posted back, and the column it must filter on, depends on
                // rbtnTransactionType. Using the wrong column throws "Invalid column name"
                // because that column doesn't exist against the other SP's result set.
                string typeColumn = transactionType == "S"
                    ? "A.SycDepositTypeId"
                    : "A.LmtLoanTypeMasterId";

                filters.Add($" And {typeColumn} = {typeId}");
            }

            return string.Join(" ", filters);
        }

        private string MapOrderBy(string orderBy)
        {
            // Matches the original CMemberAccountManagementReports.GetMemberAccountDeactiveReport
            // switch exactly, including the front-end's actual option text ("Member Name" with
            // a space, "Account No" with a space) rather than the API-DTO-style names used before.
            string clause = orderBy switch
            {
                "Member Name" => " order by MemberName",
                "MemberId" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId",
                "Account No" => " order by substring(AccountNo, 1,(len(AccountNo)-charindex('-', AccountNo))-1), AccountNo",
                "Type" => " order by Type",
                "LastDate" => " order by LastTransactionDate DESC",
                "Age" => " order by Age",
                _ => " order by MemberName"
            };

            return clause;
        }
    }
}
