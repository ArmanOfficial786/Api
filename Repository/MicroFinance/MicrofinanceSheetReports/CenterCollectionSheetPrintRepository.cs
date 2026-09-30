// Repository/Microfinance/MicrofinanceSheetReports/CenterCollectionSheetPrintRepository.cs
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
    public class CenterCollectionSheetPrintRepository : ICenterCollectionSheetPrintRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<CenterCollectionSheetPrintRepository> _logger;

        public CenterCollectionSheetPrintRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<CenterCollectionSheetPrintRepository> logger)
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

        private static string GetSheetTypeName(string sheetType) =>
            sheetType?.Trim().ToUpper() switch
            {
                "A" => "Saving And Loan",
                "S" => "Saving",
                "L" => "Loan",
                "LS" => "Three (Saving And Loan)",
                _ => "Saving And Loan"
            };

        private static string GetSpName(string sheetType) =>
            sheetType?.Trim().ToUpper() switch
            {
                "A" => "sp_5_43_GetCenterCollectionSheetPrintReport",
                "S" => "sp_5_43_GetCenterCollectionSheetSavingPrintReport",
                "L" => "sp_5_43_GetCenterCollectionSheetLoanPrintReport",
                "LS" => "sp_5_43_GetCollectionCenterCollectionEntry",
                _ => "sp_5_43_GetCenterCollectionSheetPrintReport"
            };

        public async Task<CenterCollectionSheetPrintData> GetReportDataAsync(CenterCollectionSheetPrintRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.TillDateBs) || request.TillDateBs == "-1")
                    throw new ArgumentException("Till Date is required.");

                var collectionCenterId = SanitizeId(request.CollectionCenterId);
                if (collectionCenterId == "0")
                    throw new ArgumentException("Please select a Collection Center.");

                var sheetType = request.SheetType?.Trim().ToUpper();
                if (sheetType != "A" && sheetType != "S" && sheetType != "L" && sheetType != "LS")
                    sheetType = "A";

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var tillDateAd = await _dateConverter.NepaliToEnglishAsync(request.TillDateBs);

                // The SPs accept a SQL fragment/typed value for the till date; we send a typed DATE.
                var spName = GetSpName(sheetType);

                CenterCollectionSheetHeaderDto? header = null;
                List<CenterCollectionSheetRowDto> rows = new();

                if (sheetType == "LS")
                {
                    // Legacy LS mode uses different parameter names & returns only one table
                    var parametersLs = new DynamicParameters();
                    parametersLs.Add("@SqlTransDate", tillDateAd.Date, DbType.Date);
                    parametersLs.Add("@SqlGroupId", long.Parse(collectionCenterId), DbType.Int64);

                    rows = (await connection.QueryAsync<CenterCollectionSheetRowDto>(
                        spName,
                        parametersLs,
                        commandType: CommandType.StoredProcedure,
                        commandTimeout: 600
                    )).AsList();
                }
                else
                {
                    // A / S / L return two result sets: Header + Rows
                    var parameters = new DynamicParameters();
                    parameters.Add("@SqlTillDate", tillDateAd.ToString("MM-dd-yyyy"), DbType.String, size: -1);
                    parameters.Add("@SqlCollectionCenterId", collectionCenterId, DbType.String, size: -1);

                    using var multi = await connection.QueryMultipleAsync(
                        spName,
                        parameters,
                        commandType: CommandType.StoredProcedure,
                        commandTimeout: 600);

                    header = await multi.ReadFirstOrDefaultAsync<CenterCollectionSheetHeaderDto>();
                    rows = (await multi.ReadAsync<CenterCollectionSheetRowDto>()).AsList();
                }

                // Add/replace the totals block inside GetReportDataAsync, sheetType "A"/"S"/"L" branch:
                return new CenterCollectionSheetPrintData
                {
                    Header = header,
                    Rows = rows,
                    TotalRecords = rows.Count,

                    TotalSaving1Balance = rows.Sum(r => r.Saving1Balance ?? 0),
                    TotalSaving2Balance = rows.Sum(r => r.Saving2Balance ?? 0),
                    TotalSaving3Balance = rows.Sum(r => r.Saving3Balance ?? 0),
                    TotalSaving4Balance = rows.Sum(r => r.Saving4Balance ?? 0),

                    TotalLoan1RemPrinciple = rows.Sum(r => r.Loan1RemPrinciple ?? 0),
                    TotalLoan1PayablePri = rows.Sum(r => r.Loan1PayablePri ?? 0),
                    TotalLoan1PayableInt = rows.Sum(r => r.Loan1PayableInt ?? 0),
                    TotalLoan2RemPrinciple = rows.Sum(r => r.Loan2RemPrinciple ?? 0),
                    TotalLoan2PayablePri = rows.Sum(r => r.Loan2PayablePri ?? 0),
                    TotalLoan2PayableInt = rows.Sum(r => r.Loan2PayableInt ?? 0),
                    TotalLoan3RemPrinciple = rows.Sum(r => r.Loan3RemPrinciple ?? 0),
                    TotalLoan3PayablePri = rows.Sum(r => r.Loan3PayablePri ?? 0),
                    TotalLoan3PayableInt = rows.Sum(r => r.Loan3PayableInt ?? 0),
                    TotalLoan4RemPrinciple = rows.Sum(r => r.Loan4RemPrinciple ?? 0),
                    TotalLoan4PayablePri = rows.Sum(r => r.Loan4PayablePri ?? 0),
                    TotalLoan4PayableInt = rows.Sum(r => r.Loan4PayableInt ?? 0),

                    TillDateBs = request.TillDateBs,
                    TillDateAd = tillDateAd.ToString("MM-dd-yyyy"),
                    SheetType = sheetType,
                    SheetTypeName = GetSheetTypeName(sheetType)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync (CenterCollectionSheetPrint)");
                throw;
            }
        }
    }
}