// Repository/MemberAccount/OthersReport/BranchToBranchCollectionRepository.cs
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
    public class BranchToBranchCollectionRepository : IBranchToBranchCollection
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<BranchToBranchCollectionRepository> _logger;

        public BranchToBranchCollectionRepository(AppDbContext context, IDateConverterService dateConverter, ILogger<BranchToBranchCollectionRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<BranchToBranchCollectionData> GetReportDataAsync(BranchToBranchCollectionRequestDto request)
        {
            try
            {
                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                var sqlFilterExp = new StringBuilder();
                sqlFilterExp.Append(" And t.TransactionOn between '")
                            .Append(fromDateStr).Append("' And '")
                            .Append(toDateStr).Append("' ");

                if (request.BranchFromId != -1)
                {
                    sqlFilterExp.Append(" And t.UsmOfficeId = ").Append(request.BranchFromId);
                }

                if (request.CollectorId.HasValue && request.CollectorId.Value != -1)
                {
                    sqlFilterExp.Append(" And t.HurCollectorId = ").Append(request.CollectorId.Value);
                }

                var sqlAccountExp = new StringBuilder();
                switch (request.ReportType)
                {
                    case "Saving Wise":
                        sqlAccountExp.Append(" And a.UsmOfficeId = ").Append(request.BranchToId);
                        break;
                    case "Loan Wise":
                        sqlAccountExp.Append(" And l.UsmOfficeId = ").Append(request.BranchToId);
                        break;
                    case "Miscellaneous Wise":
                        sqlAccountExp.Append(" And m.UsmOfficeId = ").Append(request.BranchToId);
                        break;
                    case "Share Wise":
                        sqlAccountExp.Append(" And m.UsmOfficeId = ").Append(request.BranchToId);
                        break;
                    default: // All
                        break;
                }

                var sqlOrderBy = new StringBuilder();
                switch (request.OrderBy)
                {
                    case "Member Id":
                        sqlOrderBy.Append(" order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId");
                        break;
                    case "Member Name":
                        sqlOrderBy.Append(" order by MemberName");
                        break;
                    case "Account No":
                        sqlOrderBy.Append(" order by substring(AccountNo, 1,(len(AccountNo)-charindex('-', AccountNo))-1), AccountNo");
                        break;
                    case "Amount":
                        sqlOrderBy.Append(" order by Amount DESC");
                        break;
                    default:
                        sqlOrderBy.Append(" order by MemberId");
                        break;
                }

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterAccountExp", sqlAccountExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterOrderBy", sqlOrderBy.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlCollectorExp", string.Empty, DbType.String, size: -1);

                if (request.ReportType == "All")
                {
                    return await GetAllReportDataAsync(connection, parameters, request);
                }
                else
                {
                    string spName = GetStoredProcedureName(request.ReportType);
                    var rows = await connection.QueryAsync<BranchToBranchCollectionRowDto>(
                        spName,
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    var resultList = rows.AsList();
                    NormalizeSavingType(resultList);

                    var branchFromName = await GetOfficeNameByIdAsync(request.BranchFromId);
                    var branchToName = await GetOfficeNameByIdAsync(request.BranchToId);
                    string? collectorName = null;
                    if (request.CollectorId.HasValue && request.CollectorId.Value != -1)
                    {
                        collectorName = await GetCollectorNameByIdAsync(request.CollectorId.Value);
                    }

                    return new BranchToBranchCollectionData
                    {
                        Rows = resultList,
                        TotalRecords = resultList.Count,
                        TotalAmount = resultList.Sum(r => r.Amount ?? 0),
                        FromDateBs = request.FromDateBs,
                        ToDateBs = request.ToDateBs,
                        BranchFromId = request.BranchFromId,
                        BranchFromName = branchFromName ?? "Unknown",
                        BranchToId = request.BranchToId,
                        BranchToName = branchToName ?? "Unknown",
                        CollectorId = request.CollectorId,
                        CollectorName = collectorName,
                        ReportType = request.ReportType,
                        OrderBy = request.OrderBy
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync");
                throw;
            }
        }

        private async Task<BranchToBranchCollectionData> GetAllReportDataAsync(
            SqlConnection connection,
            DynamicParameters baseParams,
            BranchToBranchCollectionRequestDto request)
        {
            var allRows = new List<BranchToBranchCollectionRowDto>();

            // Each SP maps to a distinct report-type label — used as a fallback
            // when the SP doesn't return SavingType/Type itself (see NormalizeSavingType).
            var spNamesWithLabel = new (string SpName, string Label)[]
            {
                ("sp_5_43_GetBranchToBranchCollectionSaving", "Saving Wise"),
                ("sp_5_43_GetBranchToBranchCollectionLoan", "Loan Wise"),
                ("sp_5_43_GetBranchToBranchCollectionMiscellineous", "Miscellaneous Wise"),
                ("sp_5_43_GetBranchToBranchCollectionShare", "Share Wise")
            };

            foreach (var (spName, label) in spNamesWithLabel)
            {
                var rows = (await connection.QueryAsync<BranchToBranchCollectionRowDto>(
                    spName,
                    baseParams,
                    commandType: CommandType.StoredProcedure
                )).AsList();

                NormalizeSavingType(rows, fallbackLabel: label);
                allRows.AddRange(rows);
            }

            var sortedRows = ApplySorting(allRows, request.OrderBy);

            var branchFromName = await GetOfficeNameByIdAsync(request.BranchFromId);
            var branchToName = await GetOfficeNameByIdAsync(request.BranchToId);
            string? collectorName = null;
            if (request.CollectorId.HasValue && request.CollectorId.Value != -1)
            {
                collectorName = await GetCollectorNameByIdAsync(request.CollectorId.Value);
            }

            return new BranchToBranchCollectionData
            {
                Rows = sortedRows,
                TotalRecords = sortedRows.Count,
                TotalAmount = sortedRows.Sum(r => r.Amount ?? 0),
                FromDateBs = request.FromDateBs,
                ToDateBs = request.ToDateBs,
                BranchFromId = request.BranchFromId,
                BranchFromName = branchFromName ?? "Unknown",
                BranchToId = request.BranchToId,
                BranchToName = branchToName ?? "Unknown",
                CollectorId = request.CollectorId,
                CollectorName = collectorName,
                ReportType = request.ReportType,
                OrderBy = request.OrderBy
            };
        }

        /// <summary>
        /// Root-cause fix: the SP returns the grouping column as SavingType, not Type.
        /// Coalesce so the view's GroupBy(r => r.Type) always has a value to group on,
        /// falling back to the SP's own report-type label when neither is populated.
        /// </summary>
        private static void NormalizeSavingType(List<BranchToBranchCollectionRowDto> rows, string? fallbackLabel = null)
        {
            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.Type))
                {
                    row.Type = !string.IsNullOrWhiteSpace(row.SavingType)
                        ? row.SavingType
                        : fallbackLabel;
                }
            }
        }

        private List<BranchToBranchCollectionRowDto> ApplySorting(List<BranchToBranchCollectionRowDto> rows, string orderBy)
        {
            return orderBy switch
            {
                "Member Id" => rows.OrderBy(r => r.MemberId).ToList(),
                "Member Name" => rows.OrderBy(r => r.MemberName).ToList(),
                "Account No" => rows.OrderBy(r => r.AccountNo).ToList(),
                "Amount" => rows.OrderByDescending(r => r.Amount).ToList(),
                _ => rows.OrderBy(r => r.MemberId).ToList()
            };
        }

        private string GetStoredProcedureName(string reportType)
        {
            return reportType switch
            {
                "Saving Wise" => "sp_5_43_GetBranchToBranchCollectionSaving",
                "Loan Wise" => "sp_5_43_GetBranchToBranchCollectionLoan",
                "Miscellaneous Wise" => "sp_5_43_GetBranchToBranchCollectionMiscellineous",
                "Share Wise" => "sp_5_43_GetBranchToBranchCollectionShare",
                _ => "sp_5_43_GetBranchToBranchCollectionSaving"
            };
        }

        public async Task<string?> GetOfficeNameByIdAsync(long officeId)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                const string sql = "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId = @OfficeId";
                return await connection.ExecuteScalarAsync<string>(sql, new { OfficeId = officeId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetOfficeNameByIdAsync");
                return null;
            }
        }

        public async Task<string?> GetCollectorNameByIdAsync(long collectorId)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                const string sql = "SELECT CollectorFullName FROM HurCollector WHERE HurCollectorId = @CollectorId";
                return await connection.ExecuteScalarAsync<string>(sql, new { CollectorId = collectorId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCollectorNameByIdAsync");
                return null;
            }
        }
    }
}