// Repositories/MemberAccount/ChequeBookReport/ChequeBookIssueRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.ChequeBookReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.ChequeBookReport;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.MemberAccount.ChequeBookReport
{
    public class ChequeBookIssueRepository : IChequeBookIssueRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<ChequeBookIssueRepository> _logger;

        public ChequeBookIssueRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<ChequeBookIssueRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        private static T SafeGetValue<T>(dynamic obj, string propertyName)
        {
            try
            {
                if (obj is IDictionary<string, object> dict)
                {
                    if (dict.TryGetValue(propertyName, out var value))
                    {
                        if (value == null)
                            return default;

                        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            var underlyingType = Nullable.GetUnderlyingType(typeof(T));
                            return (T)Convert.ChangeType(value, underlyingType);
                        }

                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                }
                else
                {
                    var property = obj.GetType().GetProperty(propertyName);
                    if (property != null)
                    {
                        var value = property.GetValue(obj);
                        if (value == null)
                            return default;

                        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            var underlyingType = Nullable.GetUnderlyingType(typeof(T));
                            return (T)Convert.ChangeType(value, underlyingType);
                        }

                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                }
                return default;
            }
            catch
            {
                return default;
            }
        }

        public async Task<ChequeBookIssueData> GetReportDataAsync(ChequeBookIssueRequestDto request)
        {
            try
            {
                var sqlFilterExp = new StringBuilder();

                if (request.MemberId != -1)
                {
                    sqlFilterExp.Append($" AND m.MemMemberRegistrationId = {request.MemberId}");
                }

                if (request.ReportView == "Date" &&
                    !string.IsNullOrEmpty(request.FromDateBs) && request.FromDateBs != "-1" &&
                    !string.IsNullOrEmpty(request.ToDateBs) && request.ToDateBs != "-1")
                {
                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);
                    var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                    var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                    sqlFilterExp.Append($" AND c.ChequeIssueOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");
                }

                if (!string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
                {
                    sqlFilterExp.Append($" AND c.UsmOfficeId IN ({request.BranchIds})");
                }

                var orderByClause = BuildOrderByClause(request.OrderBy);
                if (!string.IsNullOrEmpty(orderByClause))
                {
                    sqlFilterExp.Append(orderByClause);
                }

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var rows = await connection.QueryAsync<dynamic>(
                    "sp_5_43_GetChequeBookIssue",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                // ---------------------------------------------------------------
                // Confirmed against the real sp_5_43_GetChequeBookIssue SELECT list:
                // the date column comes back aliased as "IssueDate"
                // (c.ChequeIssueOnBs As IssueDate) — not "ChequeIssueOnBs" or
                // "ChequeIssueOn", which is what this mapping was previously
                // looking for. That mismatch is why Issue Date was always empty.
                // The SP also never returns Status/Remarks/BranchName/TotalCheques
                // columns at all, so those stay null/blank here — that's expected
                // given this SELECT list, not a mapping bug.
                // ---------------------------------------------------------------
                var resultList = rows.Select(r => new ChequeBookIssueRowDto
                {
                    MemberId = SafeGetValue<string>(r, "MemberId"),
                    MemberName = SafeGetValue<string>(r, "Name"),
                    AccountNo = SafeGetValue<string>(r, "AccountNo"),
                    ChequeIssueDate = SafeGetValue<string>(r, "IssueDate"),
                    ChequeIssueDateBs = SafeGetValue<string>(r, "IssueDate"),
                    ChequeNoFrom = SafeGetValue<long?>(r, "ChequeNoFrom"),
                    ChequeNoTo = SafeGetValue<long?>(r, "ChequeNoTo"),
                    TotalCheques = SafeGetValue<int?>(r, "TotalCheques"),
                    Status = SafeGetValue<string>(r, "Status"),
                    Remarks = SafeGetValue<string>(r, "Remarks"),
                    BranchName = SafeGetValue<string>(r, "BranchName")
                }).ToList();

                return new ChequeBookIssueData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalChequesIssued = resultList.Sum(r => r.TotalCheques ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchNames = request.BranchName,
                    OrderBy = request.OrderBy,
                    MemberId = request.MemberIdText,
                    MemberName = request.MemberName,
                    ReportView = request.ReportView
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync for Cheque Book Issue Report");
                throw;
            }
        }

        private static string BuildOrderByClause(string orderBy)
        {
            string result;

            switch (orderBy)
            {
                case "Member Name":
                    result = " ORDER BY Name";
                    break;
                case "Member Id":
                    result = " ORDER BY substring(m.MemberId, 1,(len(m.MemberId)-charindex('-', m.MemberId))-1), m.MemberId";
                    break;
                case "Account No":
                    result = " ORDER BY substring(a.AccountNo, 1,(len(a.AccountNo)-charindex('-', a.AccountNo))-1), a.AccountNo";
                    break;
                case "Cheque Issue Date":
                    result = " ORDER BY c.ChequeIssueOnBs";
                    break;
                case "Cheque No From":
                    result = " ORDER BY c.ChequeNoFrom";
                    break;
                default:
                    result = " ORDER BY c.ChequeIssueOnBs";
                    break;
            }

            return result;
        }
    }
}