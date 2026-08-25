////// Repository/AccountOperation/RatioAnalysisRepository.cs
////using Dapper;
using NexgenCosysReport.DbContext;
////using Microsoft.Data.SqlClient;
////using Microsoft.EntityFrameworkCore;
////using NexgenCosysReport.Dtos.RequestDtos.Account;
////using NexgenCosysReport.Inteface.ServiceInterface.Account;
////using NexgenCosysReport.Inteface.ServiceInterface.Common;
////using System.Data;

////namespace NexgenCosysReport.Repository.Account
////{
////    public class RatioAnalysisRepository : IRatioAnalysis
////    {
////        private readonly AppDbContext _context;
////        private readonly IDateConverterService _dateConverter;
////        private readonly ILogger<RatioAnalysisRepository> _logger;

////        public RatioAnalysisRepository(
////            AppDbContext context,
////            IDateConverterService dateConverter,
////            ILogger<RatioAnalysisRepository> logger)
////        {
////            _context = context;
////            _dateConverter = dateConverter;
////            _logger = logger;
////        }

////        public async Task<RatioAnalysisData> GetRatioAnalysis(RatioAnalysisRequest request)
////        {
////            var connectionString = _context.Database.GetConnectionString();
////            using var connection = new SqlConnection(connectionString);
////            await connection.OpenAsync();

////            // Convert dates to English (DateTime)
////            var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDate);
////            var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDate);

////            // Build branch filter
////            string branchFilter = "";
////            if (!request.SameCompanyName && !string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
////            {
////                branchFilter = request.BranchIds;
////            }

////            var parameters = new DynamicParameters();
////            parameters.Add("@FromDate", fromDateAd);
////            parameters.Add("@ToDate", toDateAd);
////            parameters.Add("@BranchIds", branchFilter);
////            parameters.Add("@IsLoanMaturity1to30", request.Enable1to30Days);
////            parameters.Add("@ProvisionType", request.ProvisionType);

////            // Execute SP and get multiple result sets
////            var result = await connection.QueryMultipleAsync(
////                "sp_6_113_RatioAnalysisReport",
////                parameters,
////                commandType: CommandType.StoredProcedure
////            );

////            var rows = new List<RatioAnalysisRowDto>();

////            // Read all tables from the result set
////            while (!result.IsConsumed)
////            {
////                var table = await result.ReadAsync<dynamic>();
////                foreach (var row in table)
////                {
////                    // Try to extract Category, RatioName, and Value
////                    string? category = row.Category ?? row.LedgerHead ?? row.GroupName ?? null;
////                    string? ratioName = row.RatioName ?? row.Ratio ?? row.Description ?? row.SubLedger ?? null;
////                    decimal value = 0;
////                    if (row.Value != null) value = Convert.ToDecimal(row.Value);
////                    else if (row.Amount != null) value = Convert.ToDecimal(row.Amount);
////                    else if (row.Balance != null) value = Convert.ToDecimal(row.Balance);

////                    if (!string.IsNullOrEmpty(ratioName))
////                    {
////                        rows.Add(new RatioAnalysisRowDto
////                        {
////                            Category = category?.ToString(),
////                            RatioName = ratioName?.ToString(),
////                            Value = value
////                        });
////                    }
////                }
////            }

////            // If IsTotalOnly, we might filter or keep all; the SP likely returns only totals.
////            // We'll keep all rows.

////            return new RatioAnalysisData { Rows = rows };
////        }
////    }
////}








//// Repository/AccountOperation/RatioAnalysisRepository.cs
//using Dapper;
//using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore;
//using NexgenCosysReport.Dtos.RequestDtos.Account;
//using NexgenCosysReport.Inteface.ServiceInterface.Account;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using System.Data;

//namespace NexgenCosysReport.Repository.Account
//{
//    public class RatioAnalysisRepository : IRatioAnalysis
//    {
//        private readonly AppDbContext _context;
//        private readonly IDateConverterService _dateConverter;
//        private readonly ILogger<RatioAnalysisRepository> _logger;

//        public RatioAnalysisRepository(
//            AppDbContext context,
//            IDateConverterService dateConverter,
//            ILogger<RatioAnalysisRepository> logger)
//        {
//            _context = context;
//            _dateConverter = dateConverter;
//            _logger = logger;
//        }

//        public async Task<RatioAnalysisData> GetRatioAnalysis(RatioAnalysisRequest request)
//        {
//            var connectionString = _context.Database.GetConnectionString();
//            using var connection = new SqlConnection(connectionString);
//            await connection.OpenAsync();

//            // Convert dates to English (DateTime) — matches legacy's
//            // cComCalender.NepaliToEnglish(fromDateBs)/toDateBs calls before
//            // invoking the SP.
//            var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDate);
//            var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDate);

//            // FIX: legacy (btnViewReport_Click) always passed the actual
//            // selected office IDs straight through — there was no
//            // "SameCompanyName" gate suppressing the filter. Gating on
//            // SameCompanyName here meant @BranchIds was silently sent as ""
//            // on every request where SameCompanyName == true, which is very
//            // likely why the SP returned zero rows ("no data found").
//            //
//            // "-1" is the one legitimate case that should map to "no
//            // filter" (i.e. all branches) — everything else passes through
//            // verbatim, exactly as the WebForms code did with branchSelected.
//            string branchFilter = (!string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
//                ? request.BranchIds
//                : string.Empty;

//            _logger.LogInformation(
//                "RatioAnalysis query: FromDate={FromDate}, ToDate={ToDate}, BranchIds(raw)={RawBranchIds}, BranchFilter(sent)={BranchFilter}, SameCompanyName={SameCompanyName}, IsLoanMaturity1to30={Enable1to30}, ProvisionType={ProvisionType}",
//                fromDateAd, toDateAd, request.BranchIds, branchFilter, request.SameCompanyName, request.Enable1to30Days, request.ProvisionType);

//            var parameters = new DynamicParameters();
//            parameters.Add("@FromDate", fromDateAd);
//            parameters.Add("@ToDate", toDateAd);
//            parameters.Add("@BranchIds", branchFilter);
//            parameters.Add("@IsLoanMaturity1to30", request.Enable1to30Days);
//            parameters.Add("@ProvisionType", request.ProvisionType);

//            // Execute SP and get multiple result sets
//            var result = await connection.QueryMultipleAsync(
//                "sp_6_113_RatioAnalysisReport",
//                parameters,
//                commandType: CommandType.StoredProcedure
//            );

//            var rows = new List<RatioAnalysisRowDto>();
//            var tableIndex = 0;

//            // Read all tables from the result set
//            while (!result.IsConsumed)
//            {
//                var table = (await result.ReadAsync<dynamic>()).ToList();

//                // DIAGNOSTIC: log the actual column names Dapper sees on the
//                // first row of each result set. This is the fastest way to
//                // confirm whether Category/RatioName/Value truly exist under
//                // those names, or under something else entirely (e.g.
//                // "Title", "LedgerHead", "Percentage", "Amount" — the SP's
//                // actual output columns are unknown to me without seeing
//                // sp_6_113_RatioAnalysisReport's definition).
//                if (table.Count > 0)
//                {
//                    var firstRowDict = (IDictionary<string, object>)table[0];
//                    _logger.LogInformation(
//                        "RatioAnalysis result set #{TableIndex} column names: {Columns}",
//                        tableIndex, string.Join(", ", firstRowDict.Keys));
//                }
//                else
//                {
//                    _logger.LogWarning("RatioAnalysis result set #{TableIndex} returned zero rows.", tableIndex);
//                }

//                foreach (var row in table)
//                {
//                    string? category = row.Category ?? row.LedgerHead ?? row.GroupName ?? null;
//                    string? ratioName = row.RatioName ?? row.Ratio ?? row.Description ?? row.SubLedger ?? null;
//                    decimal value = 0;
//                    if (row.Value != null) value = Convert.ToDecimal(row.Value);
//                    else if (row.Amount != null) value = Convert.ToDecimal(row.Amount);
//                    else if (row.Balance != null) value = Convert.ToDecimal(row.Balance);

//                    if (!string.IsNullOrEmpty(ratioName))
//                    {
//                        rows.Add(new RatioAnalysisRowDto
//                        {
//                            Category = category?.ToString(),
//                            RatioName = ratioName?.ToString(),
//                            Value = value
//                        });
//                    }
//                    else
//                    {
//                        _logger.LogWarning(
//                            "RatioAnalysis row skipped — no RatioName/Ratio/Description/SubLedger column matched. Available columns: {Columns}",
//                            string.Join(", ", ((IDictionary<string, object>)row).Keys));
//                    }
//                }

//                tableIndex++;
//            }

//            return new RatioAnalysisData { Rows = rows };
//        }
//    }
//}


// Repository/AccountOperation/RatioAnalysisRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Dtos.RequestDtos.Account;
using NexgenCosysReport.Inteface.ServiceInterface.Account;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;

namespace NexgenCosysReport.Repository.Account
{
    public class RatioAnalysisRepository : IRatioAnalysis
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<RatioAnalysisRepository> _logger;

        public RatioAnalysisRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<RatioAnalysisRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        /// <summary>
        /// CComCalender/NepaliCalenderPicker convention used everywhere in the
        /// legacy CAccountOperationReports class is "yyyy/MM/dd". Normalize
        /// hyphenated input so a silent mis-parse doesn't produce a bogus date
        /// range.
        /// </summary>
        private static string NormalizeBsDate(string bsDate)
        {
            if (string.IsNullOrWhiteSpace(bsDate)) return bsDate;
            return bsDate.Replace('-', '/');
        }

        public async Task<RatioAnalysisData> GetRatioAnalysis(RatioAnalysisRequest request)
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var normalizedFromDate = NormalizeBsDate(request.FromDate);
            var normalizedToDate = NormalizeBsDate(request.ToDate);

            var fromDateAd = (await _dateConverter.NepaliToEnglishAsync(normalizedFromDate)).Date;
            var toDateAd = (await _dateConverter.NepaliToEnglishAsync(normalizedToDate)).Date;

            if (toDateAd < fromDateAd)
            {
                _logger.LogWarning(
                    "RatioAnalysis: converted ToDate ({ToDateAd}) is before FromDate ({FromDateAd}).",
                    toDateAd, fromDateAd);
            }

            // sp_6_113_RatioAnalysisReport expects @BranchIds as a literal,
            // comma-separated list of office ids (its WHILE loop splits on
            // commas and inserts each piece into an INT column). It does NOT
            // understand an empty string or "-1" as "all branches" — an empty
            // string will actually throw a conversion error when inserted
            // into the @tblBranchIds INT column. Only pass through what the
            // caller actually selected.
            string branchFilter = request.BranchId ?? string.Empty;

            _logger.LogInformation(
                "RatioAnalysis query: FromDate(AD)={FromDate}, ToDate(AD)={ToDate}, BranchIds={BranchIds}, IsLoanMaturity1to30={Enable1to30}, ProvisionType={ProvisionType}",
                fromDateAd, toDateAd, branchFilter, request.Enable1to30Days, request.ProvisionType);

            var parameters = new DynamicParameters();
            parameters.Add("@FromDate", fromDateAd, DbType.Date);
            parameters.Add("@ToDate", toDateAd, DbType.Date);
            parameters.Add("@BranchIds", branchFilter);
            parameters.Add("@IsLoanMaturity1to30", request.Enable1to30Days);
            parameters.Add("@ProvisionType", request.ProvisionType);

            using var result = await connection.QueryMultipleAsync(
                "sp_6_113_RatioAnalysisReport",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            // Result set 1: #tempTable — GroupOrder, GroupName, SN, Detail,
            // Total, TotalPercent, plus dynamic Col{n}/ColPer{n} pairs per
            // selected branch. We only need GroupName/Detail/Total since the
            // DTO is a flat Category/RatioName/Value shape and Total is
            // already the SP's aggregate across whichever branches were sent.
            var mainRows = (await result.ReadAsync()).ToList();

            // Result set 2: one row per selected branch, OfficeName only.
            var branchRows = (await result.ReadAsync()).ToList();
            var branchNames = branchRows
                .Select(r => ((IDictionary<string, object>)r).TryGetValue("OfficeName", out var n) ? n?.ToString() : null)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();

            _logger.LogInformation(
                "RatioAnalysis: {RowCount} detail rows returned, branches resolved: {Branches}",
                mainRows.Count, string.Join(", ", branchNames));

            var rows = new List<RatioAnalysisRowDto>();

            foreach (var row in mainRows)
            {
                var dict = (IDictionary<string, object>)row;

                string? groupName = dict.TryGetValue("GroupName", out var gn) ? gn?.ToString() : null;
                string? detail = dict.TryGetValue("Detail", out var d) ? d?.ToString() : null;

                decimal value = 0;
                if (dict.TryGetValue("Total", out var t) && t != null && t != DBNull.Value)
                {
                    value = Convert.ToDecimal(t);
                }

                if (!string.IsNullOrWhiteSpace(detail))
                {
                    rows.Add(new RatioAnalysisRowDto
                    {
                        Category = groupName,
                        RatioName = detail,
                        Value = value
                    });
                }
                else
                {
                    _logger.LogWarning(
                        "RatioAnalysis row skipped — Detail column was empty. Columns: {Columns}",
                        string.Join(", ", dict.Keys));
                }
            }

            if (rows.Count == 0)
            {
                _logger.LogWarning(
                    "RatioAnalysis: SP returned {RawRowCount} raw rows but 0 mapped rows. First row columns (if any): {Columns}",
                    mainRows.Count,
                    mainRows.Count > 0 ? string.Join(", ", ((IDictionary<string, object>)mainRows[0]).Keys) : "(no rows at all)");
            }

            return new RatioAnalysisData { Rows = rows };
        }
    }
}
