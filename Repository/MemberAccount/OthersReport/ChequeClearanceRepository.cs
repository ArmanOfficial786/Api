//// Repositories/AccountOperation/ChequeClearanceRepository.cs
//using Dapper;
//using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore;
//using NexgenCosysReport.DbContext;
//using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport;
//using System.Data;
//using System.Text;

//namespace NexgenCosysReport.Repository.MemberAccount.OthersReport
//{
//    public class ChequeClearanceRepository : IChequeClearance
//    {
//        private readonly AppDbContext _context;
//        private readonly IDateConverterService _dateConverter;
//        private readonly ILogger<ChequeClearanceRepository> _logger;

//        public ChequeClearanceRepository(
//            AppDbContext context,
//            IDateConverterService dateConverter,
//            ILogger<ChequeClearanceRepository> logger)
//        {
//            _context = context;
//            _dateConverter = dateConverter;
//            _logger = logger;
//        }

//        public async Task<ChequeClearanceData> GetReportDataAsync(ChequeClearanceRequestDto request)
//        {
//            try
//            {
//                var sqlFilterExp = new StringBuilder();

//                if (!string.IsNullOrEmpty(request.FromDateBs) && request.FromDateBs != "-1" &&
//                    !string.IsNullOrEmpty(request.ToDateBs) && request.ToDateBs != "-1")
//                {
//                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
//                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);
//                    var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
//                    var toDateStr = toDateAd.ToString("yyyy-MM-dd");

//                    // Build filter based on cheque type
//                    switch (request.ChequeType)
//                    {
//                        case "Get All":
//                            sqlFilterExp.Append($" AND (ac.ChequeReceivedOn BETWEEN '{fromDateStr}' AND '{toDateStr}')");
//                            sqlFilterExp.Append($" OR (ac.LastModifiedOn BETWEEN '{fromDateStr}' AND '{toDateStr}')");
//                            break;
//                        case "Receive Cheque":
//                            sqlFilterExp.Append($" AND ac.IsActive = 1 AND ac.IsChequeSend = 0 AND ac.IsChequeCleared = 0 AND ac.IsChequeRejected = 0");
//                            sqlFilterExp.Append($" AND ac.ChequeReceivedOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");
//                            break;
//                        case "Deleted Cheque":
//                            sqlFilterExp.Append($" AND ac.IsActive = 0 AND ac.IsChequeSend = 0 AND ac.IsChequeCleared = 0 AND ac.IsChequeRejected = 0");
//                            sqlFilterExp.Append($" AND ac.ChequeReceivedOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");
//                            break;
//                        case "Send Cheque":
//                            sqlFilterExp.Append($" AND ac.IsActive = 1 AND ac.IsChequeSend = 1 AND ac.IsChequeCleared = 0 AND ac.IsChequeRejected = 0");
//                            sqlFilterExp.Append($" AND ac.LastModifiedOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");
//                            break;
//                        case "Clearance Cheque":
//                            sqlFilterExp.Append($" AND ac.IsActive = 1 AND ac.IsChequeSend = 1 AND ac.IsChequeCleared = 1 AND ac.IsChequeRejected = 0");
//                            sqlFilterExp.Append($" AND ac.LastModifiedOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");
//                            break;
//                        case "Rejected Cheque":
//                            sqlFilterExp.Append($" AND ac.IsChequeRejected = 1");
//                            sqlFilterExp.Append($" AND ac.LastModifiedOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");
//                            break;
//                        default:
//                            sqlFilterExp.Append($" AND (ac.ChequeReceivedOn BETWEEN '{fromDateStr}' AND '{toDateStr}')");
//                            sqlFilterExp.Append($" OR (ac.LastModifiedOn BETWEEN '{fromDateStr}' AND '{toDateStr}')");
//                            break;
//                    }
//                }

//                var parameters = new DynamicParameters();
//                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);

//                var connectionString = _context.Database.GetConnectionString();
//                using var connection = new SqlConnection(connectionString);
//                await connection.OpenAsync();

//                var rows = await connection.QueryAsync<ChequeClearanceRowDto>(
//                    "sp_5_43_GetChequeClearance",
//                    parameters,
//                    commandType: CommandType.StoredProcedure
//                );

//                var resultList = rows.AsList();

//                // Calculate statistics
//                var receivedCount = resultList.Count(r => r.Status == "Received" || r.Status == "Receive");
//                var sendCount = resultList.Count(r => r.Status == "Sent" || r.Status == "Send");
//                var clearedCount = resultList.Count(r => r.Status == "Cleared" || r.Status == "Clearance");
//                var rejectedCount = resultList.Count(r => r.Status == "Rejected");
//                var deletedCount = resultList.Count(r => r.Status == "Deleted");

//                return new ChequeClearanceData
//                {
//                    Rows = resultList,
//                    TotalRecords = resultList.Count,
//                    TotalCheques = resultList.Count,
//                    FromDateBs = request.FromDateBs,
//                    ToDateBs = request.ToDateBs,
//                    ChequeType = request.ChequeType,
//                    ChequeTypeDisplayName = GetDisplayName(request.ChequeType),
//                    ReceivedCount = receivedCount,
//                    SendCount = sendCount,
//                    ClearedCount = clearedCount,
//                    RejectedCount = rejectedCount,
//                    DeletedCount = deletedCount,
//                    TotalAmount = resultList.Sum(r => r.Amount ?? 0)
//                };
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error in GetReportDataAsync for Cheque Clearance Report");
//                throw;
//            }
//        }

//        private static string GetDisplayName(string chequeType)
//        {
//            return chequeType switch
//            {
//                "Get All" => "All Cheques",
//                "Receive Cheque" => "Received Cheques",
//                "Deleted Cheque" => "Deleted Cheques",
//                "Send Cheque" => "Sent Cheques",
//                "Clearance Cheque" => "Cleared Cheques",
//                "Rejected Cheque" => "Rejected Cheques",
//                _ => chequeType
//            };
//        }
//    }
//}






// Repositories/AccountOperation/ChequeClearanceRepository.cs
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
    public class ChequeClearanceRepository : IChequeClearance
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<ChequeClearanceRepository> _logger;

        public ChequeClearanceRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<ChequeClearanceRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<ChequeClearanceData> GetReportDataAsync(ChequeClearanceRequestDto request)
        {
            try
            {
                var sqlFilterExp = new StringBuilder();

                if (!string.IsNullOrEmpty(request.FromDateBs) && request.FromDateBs != "-1" &&
                    !string.IsNullOrEmpty(request.ToDateBs) && request.ToDateBs != "-1")
                {
                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);
                    var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                    var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                    switch (request.ChequeType)
                    {
                        case "Get All":
                            sqlFilterExp.Append($" AND (ac.ChequeReceivedOn BETWEEN '{fromDateStr}' AND '{toDateStr}')");
                            sqlFilterExp.Append($" OR (ac.LastModifiedOn BETWEEN '{fromDateStr}' AND '{toDateStr}')");
                            break;
                        case "Receive Cheque":
                            sqlFilterExp.Append($" AND ac.IsActive = 1 AND ac.IsChequeSend = 0 AND ac.IsChequeCleared = 0 AND ac.IsChequeRejected = 0");
                            sqlFilterExp.Append($" AND ac.ChequeReceivedOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");
                            break;
                        case "Deleted Cheque":
                            sqlFilterExp.Append($" AND ac.IsActive = 0 AND ac.IsChequeSend = 0 AND ac.IsChequeCleared = 0 AND ac.IsChequeRejected = 0");
                            sqlFilterExp.Append($" AND ac.ChequeReceivedOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");
                            break;
                        case "Send Cheque":
                            sqlFilterExp.Append($" AND ac.IsActive = 1 AND ac.IsChequeSend = 1 AND ac.IsChequeCleared = 0 AND ac.IsChequeRejected = 0");
                            sqlFilterExp.Append($" AND ac.LastModifiedOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");
                            break;
                        case "Clearance Cheque":
                            sqlFilterExp.Append($" AND ac.IsActive = 1 AND ac.IsChequeSend = 1 AND ac.IsChequeCleared = 1 AND ac.IsChequeRejected = 0");
                            sqlFilterExp.Append($" AND ac.LastModifiedOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");
                            break;
                        case "Rejected Cheque":
                            sqlFilterExp.Append($" AND ac.IsChequeRejected = 1");
                            sqlFilterExp.Append($" AND ac.LastModifiedOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");
                            break;
                        default:
                            sqlFilterExp.Append($" AND (ac.ChequeReceivedOn BETWEEN '{fromDateStr}' AND '{toDateStr}')");
                            sqlFilterExp.Append($" OR (ac.LastModifiedOn BETWEEN '{fromDateStr}' AND '{toDateStr}')");
                            break;
                    }
                }

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Raw shape matching sp_5_43_GetChequeClearance's actual SELECT list —
                // used only inside this repository, never exposed to the controller/view.
                var rawRows = (await connection.QueryAsync<SpRawRow>(
                    "sp_5_43_GetChequeClearance",
                    parameters,
                    commandType: CommandType.StoredProcedure
                )).ToList();

                var resultList = rawRows.Select(MapToRowDto).ToList();

                var receivedCount = resultList.Count(r => r.Status == "Received" || r.Status == "Receive");
                var sendCount = resultList.Count(r => r.Status == "Sent" || r.Status == "Send");
                var clearedCount = resultList.Count(r => r.Status == "Cleared" || r.Status == "Clearance");
                var rejectedCount = resultList.Count(r => r.Status == "Rejected");
                var deletedCount = resultList.Count(r => r.Status == "Deleted");

                return new ChequeClearanceData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalCheques = resultList.Count,
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    ChequeType = request.ChequeType,
                    ChequeTypeDisplayName = GetDisplayName(request.ChequeType),
                    ReceivedCount = receivedCount,
                    SendCount = sendCount,
                    ClearedCount = clearedCount,
                    RejectedCount = rejectedCount,
                    DeletedCount = deletedCount,
                    TotalAmount = resultList.Sum(r => r.Amount ?? 0)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync for Cheque Clearance Report");
                throw;
            }
        }

        /// <summary>
        /// Root-cause fix: sp_5_43_GetChequeClearance's SELECT list uses different column
        /// names (FullName, ChequeReceivedOnBs, ChequeClearanceSendOnBs, ChequeClearanceOnBs,
        /// ChequeRejectedBs, ChequeDeletedBs) than ChequeClearanceRowDto's properties
        /// (MemberName, ReceiveDateBs, SendDateBs, ClearanceDateBs, RejectedDateBs,
        /// DeletedDateBs). Dapper only fills a property when its name matches a returned
        /// column exactly, so mapping directly onto ChequeClearanceRowDto left those blank.
        /// This maps the SP's raw shape onto the DTO explicitly, by hand.
        /// </summary>
        private static ChequeClearanceRowDto MapToRowDto(SpRawRow raw)
        {
            long? chequeNo = null;
            // ac.ChequeNo comes back through ISNULL(ac.ChequeNo,'-'), i.e. as a string
            // ('-' when absent), not a number — parse defensively.
            if (!string.IsNullOrWhiteSpace(raw.ChequeNo) && raw.ChequeNo != "-" &&
                long.TryParse(raw.ChequeNo, out var parsedChequeNo))
            {
                chequeNo = parsedChequeNo;
            }

            static string? NullIfDash(string? value) =>
                string.IsNullOrWhiteSpace(value) || value == "-" ? null : value;

            return new ChequeClearanceRowDto
            {
                ChequeClearanceId = null, // not returned by this SP
                MemberId = raw.MemberId,
                MemberName = raw.FullName,
                AccountNo = null, // not returned by this SP
                ChequeNo = chequeNo,
                ChequeDate = null,
                ChequeDateBs = NullIfDash(raw.ChequeDateOnBs),
                ReceiveDate = null,
                ReceiveDateBs = NullIfDash(raw.ChequeReceivedOnBs),
                SendDate = null,
                SendDateBs = NullIfDash(raw.ChequeClearanceSendOnBs),
                ClearanceDate = null,
                ClearanceDateBs = NullIfDash(raw.ChequeClearanceOnBs),
                RejectedDate = null,
                RejectedDateBs = NullIfDash(raw.ChequeRejectedBs),
                DeletedDate = null,
                DeletedDateBs = NullIfDash(raw.ChequeDeletedBs),
                Status = null, // SP does not return an explicit status column
                ReceiveBy = raw.CreatedBy,
                // SP only exposes one generic LastModifiedBy for the most recent action —
                // it cannot distinguish "sent by" from "cleared by" from "rejected by".
                SendBy = raw.LastModifiedBy,
                ClearanceBy = raw.LastModifiedBy,
                RejectedBy = raw.LastModifiedBy,
                DeletedBy = raw.LastModifiedBy,
                Remarks = null,
                BranchName = null, // not returned by this SP
                Amount = raw.Amount,
                BankName = NullIfDash(raw.BankName),
                ChequeType = null
            };
        }

        private static string GetDisplayName(string chequeType)
        {
            return chequeType switch
            {
                "Get All" => "All Cheques",
                "Receive Cheque" => "Received Cheques",
                "Deleted Cheque" => "Deleted Cheques",
                "Send Cheque" => "Sent Cheques",
                "Clearance Cheque" => "Cleared Cheques",
                "Rejected Cheque" => "Rejected Cheques",
                _ => chequeType
            };
        }

        /// <summary>
        /// Exact shape of what sp_5_43_GetChequeClearance's SELECT list returns.
        /// Internal to this repository only.
        /// </summary>
        private class SpRawRow
        {
            public string? MemberId { get; set; }
            public string? FullName { get; set; }
            public decimal? Amount { get; set; }
            public string? BankName { get; set; }
            public string? ChequeNo { get; set; }
            public string? PayAgainstThisChequeTo { get; set; }
            public string? ChequeInstitutionName { get; set; }
            public string? ChequeReceivedOnBs { get; set; }
            public string? ChequeDateOnBs { get; set; }
            public string? ChequeClearanceSendOnBs { get; set; }
            public string? ChequeClearanceOnBs { get; set; }
            public string? CreatedBy { get; set; }
            public string? LastModifiedOnBs { get; set; }
            public string? LastModifiedBy { get; set; }
            public string? ChequeSendBs { get; set; }
            public string? ChequeClearedBs { get; set; }
            public string? ChequeRejectedBs { get; set; }
            public string? ChequeDeletedBs { get; set; }
        }
    }
}