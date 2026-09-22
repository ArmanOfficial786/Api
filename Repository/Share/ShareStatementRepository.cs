using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Share;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Share;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.Share
{
    public class ShareStatementRepository : IShareStatement
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<ShareStatementRepository> _logger;

        public ShareStatementRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<ShareStatementRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<ShareStatementData> GetReportDataAsync(ShareStatementRequestDto request)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sqlFilterExp = new StringBuilder();
                string? shareTypeName = null;

                // --------------------------------------------------------------
                // FIX: request.MemberId is the display code (e.g. "MR-01-205"),
                // not the internal MemMemberRegistrationId. Filtering on the
                // numeric id, unquoted, produced invalid SQL the moment the
                // value wasn't a plain integer ("And m.MemMemberRegistrationId
                // = MR-01-205" parses "MR" as a bare identifier). m.MemberId
                // is a real column on MemMemberRegistration (m), so filter on
                // that directly - same pattern used by every other report's
                // member filter in this codebase.
                // --------------------------------------------------------------
                if (!string.IsNullOrWhiteSpace(request.MemberId) && request.MemberId != "-1")
                {
                    sqlFilterExp.Append(" And m.MemberId = '").Append(request.MemberId.Trim()).Append("'");
                }

                if (request.ShareTypeId != -1)
                {
                    sqlFilterExp.Append(" And st.ShmShareTypeId = ").Append(request.ShareTypeId);
                    shareTypeName = await connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT ShareTypeName FROM ShmShareType WHERE ShmShareTypeId = @Id",
                        new { Id = request.ShareTypeId });
                }

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);

                var rows = await connection.QueryAsync<ShareStatementRowDto>(
                    "sp_8_14_GetShareStatement",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );

                var resultList = rows.AsList();

                // --------------------------------------------------------------
                // The SP only returns one date column (Date, holding the BS
                // string from t.TransactionOnBs). The image shows a separate
                // AD-formatted date alongside the BS one, so it's computed
                // here per row rather than touching the SP.
                // --------------------------------------------------------------
                foreach (var row in resultList)
                {
                    if (!string.IsNullOrWhiteSpace(row.Date))
                    {
                        try
                        {
                            var ad = await _dateConverter.NepaliToEnglishAsync(row.Date);
                            row.DateAd = ad.ToString("yyyy-MM-dd");
                        }
                        catch
                        {
                            row.DateAd = "";
                        }
                    }
                }

                var first = resultList.FirstOrDefault();

                return new ShareStatementData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalPurchasedAmount = resultList.Sum(r => r.PurchasedAmount ?? 0),
                    TotalReturnedAmount = resultList.Sum(r => r.ReturnedAmount ?? 0),
                    ClosingBalance = resultList.LastOrDefault()?.Balance ?? 0,
                    MemberId = first?.MemberId,
                    MemberName = first?.MemberName,
                    Address = first?.Address,
                    PhoneNo = first?.PhoneNo,
                    ShareTypeName = shareTypeName ?? "All"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync");
                throw;
            }
        }
    }
}