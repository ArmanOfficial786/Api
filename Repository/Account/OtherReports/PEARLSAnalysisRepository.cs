// Repository/AccountOperation/OthersReport/PEARLSAnalysisRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports;
using NexgenCosysReport.Inteface.ServiceInterface.Account.OtherReports;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;
using System.Globalization;

namespace NexgenCosysReport.Repository.Account.OtherReports
{
    public class PEARLSAnalysisRepository : IPEARLSAnalysis
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;

        public PEARLSAnalysisRepository(AppDbContext context, IDateConverterService dateConverter)
        {
            _context = context;
            _dateConverter = dateConverter;
        }

        // --------------------------------------------------------------
        // Fiscal year label, e.g. "2081/82" — same logic as legacy
        // WebForm code-behind (month > 3 => Shrawan-start fiscal year)
        // --------------------------------------------------------------
        private static string GetFiscalYearLabel(string tillDateBs)
        {
            if (string.IsNullOrEmpty(tillDateBs) || tillDateBs == "-1")
                return string.Empty;

            int year = Convert.ToInt32(tillDateBs.Substring(0, 4));
            int month = Convert.ToInt32(tillDateBs.Substring(5, 2));

            return month > 3
                ? $"{year - 1}/{year.ToString().Substring(2, 2)}"
                : $"{year - 2}/{(year - 1).ToString().Substring(2, 2)}";
        }

        public async Task<PEARLSAnalysisData> GetPEARLSAnalysisDataAsync(PEARLSAnalysisRequestDto request)
        {
            var data = new PEARLSAnalysisData();

            if (string.IsNullOrEmpty(request.TillDate) || request.TillDate == "-1")
                throw new ArgumentException("TillDate is required.", nameof(request.TillDate));

            // ---- Convert Nepali (BS) date to English (AD) DateTime ----
            // The legacy WebForm passes an actual DateTime (via CComCalender.NepaliToEnglish),
            // NOT a string — the SP parameter @toDate is typed DateTime, so we must match that.
            string tillDateAdStr = await _dateConverter.BsToAdStringAsync(request.TillDate);

            if (string.IsNullOrEmpty(tillDateAdStr) || !DateTime.TryParse(
                    tillDateAdStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var tillDateAd))
            {
                throw new ArgumentException($"Unable to convert TillDate '{request.TillDate}' to a valid date.");
            }

            var fiscalYear = GetFiscalYearLabel(request.TillDate);
            var previousDate = tillDateAd.AddYears(-1);

            var connectionString = _context.Database.GetConnectionString();
            await using var connection = new SqlConnection(connectionString);

            // ---- SP only declares 2 parameters: @toDate (DateTime), @PenaltyType (string) ----
            // Passing anything extra (e.g. @BranchId) throws:
            // "Procedure or function ... has too many arguments specified."
            var parameters = new DynamicParameters();
            parameters.Add("@toDate", tillDateAd, DbType.DateTime);
            parameters.Add("@PenaltyType", "R", DbType.String, size: 10);

            using var result = await connection.QueryMultipleAsync(
                "sp_6_56_GetPearlsBalance",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120
            );

            // ---- Result sets come back in a fixed order: P, E, A, R, L, S ----
            data.PEARLSP = (await result.ReadAsync<PEARLSBalanceDto>()).ToList();
            data.PEARLSE = (await result.ReadAsync<PEARLSBalanceDto>()).ToList();
            data.PEARLSA = (await result.ReadAsync<PEARLSBalanceDto>()).ToList();
            data.PEARLSR = (await result.ReadAsync<PEARLSBalanceDto>()).ToList();
            data.PEARLSL = (await result.ReadAsync<PEARLSBalanceDto>()).ToList();
            data.PEARLSS = (await result.ReadAsync<PEARLSBalanceDto>()).ToList();

            data.TillDate = request.TillDate;
            data.FiscalYear = fiscalYear;
            data.PreviousDate = previousDate;

            // ---- Branch name is display-only here (SP itself is not branch-filtered,
            // matching the legacy WebForm behavior where GetPEARLSBalance ignores branch) ----
            data.BranchNames = await ResolveBranchNamesAsync(connection, request.BranchId);

            return data;
        }

        private static async Task<string> ResolveBranchNamesAsync(SqlConnection connection, string? branchId)
        {
            if (string.IsNullOrEmpty(branchId) || branchId == "-1")
                return "All Branches";

            // Parse and validate every id is numeric — reject anything that isn't
            var validIds = branchId
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(id => long.TryParse(id, out var parsed) ? (long?)parsed : null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            if (validIds.Count == 0)
                return "All Branches";

            // Build a fully parameterized IN clause — no string concatenation of user input into SQL
            var paramNames = validIds.Select((_, i) => $"@BranchId{i}").ToList();
            var sql = $"SELECT STRING_AGG(OfficeName, ', ') FROM UsmOffice WHERE UsmOfficeId IN ({string.Join(",", paramNames)})";

            var parameters = new DynamicParameters();
            for (int i = 0; i < validIds.Count; i++)
            {
                parameters.Add(paramNames[i], validIds[i], DbType.Int64);
            }

            var branchNames = await connection.QueryFirstOrDefaultAsync<string>(sql, parameters);

            return string.IsNullOrEmpty(branchNames) ? "All Branches" : branchNames;
        }
    }
}