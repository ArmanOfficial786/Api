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

                return new CenterCollectionSheetPrintData
                {
                    Header = header,
                    Rows = rows,
                    TotalRecords = rows.Count,

                    TotalSavingRemBalance = rows.Sum(r => r.SavingRemBalance ?? 0),
                    TotalSavingPayableDeposit = rows.Sum(r => r.SavingPayableDeposit ?? 0),
                    TotalSavingPayableWithdrawal = rows.Sum(r => r.SavingPayableWithdrawal ?? 0),
                    TotalSavingPayableInt = rows.Sum(r => r.SavingPayableInt ?? 0),

                    TotalLoanRemPrinciple = rows.Sum(r =>
                        (r.Loan1RemPrinciple ?? 0) + (r.Loan2RemPrinciple ?? 0) +
                        (r.Loan3RemPrinciple ?? 0) + (r.Loan4RemPrinciple ?? 0)),
                    TotalLoanPayablePri = rows.Sum(r =>
                        (r.Loan1PayablePri ?? 0) + (r.Loan2PayablePri ?? 0) +
                        (r.Loan3PayablePri ?? 0) + (r.Loan4PayablePri ?? 0)),
                    TotalLoanPayableInt = rows.Sum(r =>
                        (r.Loan1PayableInt ?? 0) + (r.Loan2PayableInt ?? 0) +
                        (r.Loan3PayableInt ?? 0) + (r.Loan4PayableInt ?? 0)),

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