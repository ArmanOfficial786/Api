// Repositories/MemberAccount/ChequeBookReport/ChequeBookLostRepository.cs
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
    public class ChequeBookLostRepository : IChequeBookLost
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<ChequeBookLostRepository> _logger;

        public ChequeBookLostRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<ChequeBookLostRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<ChequeBookLostData> GetReportDataAsync(ChequeBookLostRequestDto request)
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

                    sqlFilterExp.Append($" AND i.ChequeIssueOn BETWEEN '{fromDateStr}' AND '{toDateStr}'");
                }

                if (!string.IsNullOrEmpty(request.BranchIds) && request.BranchIds != "-1")
                {
                    sqlFilterExp.Append($" AND i.UsmOfficeId IN ({request.BranchIds})");
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

                // Queried dynamically (not typed to ChequeBookLostRowDto) because
                // the SP's column aliases don't reliably match the DTO's property
                // names — see the mapping below and the comments on each field.
                // I don't have sp_5_43_GetChequeLost's actual SELECT list, so
                // these aliases are best-effort based on the sibling
                // sp_5_43_GetChequeBookIssue's conventions and this repository's
                // own BuildOrderByClause hints (e.g. "ORDER BY Name" for Member
                // Name). If any field still comes back blank, share the SP
                // definition and these can be tightened to exact matches.
                var rawRows = await connection.QueryAsync(
                    "sp_5_43_GetChequeLost",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var resultList = new List<ChequeBookLostRowDto>();
                var isFirstRow = true;
                foreach (IDictionary<string, object> r in rawRows)
                {
                    if (isFirstRow)
                    {
                        // One-time diagnostic: logs the SP's actual column names so the
                        // Lost Date / Operator mismatch can be fixed exactly instead of
                        // by further guessing. Check the application log after running
                        // this report once, then tell me (or update the GetString calls
                        // below yourself) with whatever names show up here.
                        _logger.LogWarning(
                            "sp_5_43_GetChequeLost actual columns: {Columns}",
                            string.Join(", ", r.Keys));
                        isFirstRow = false;
                    }

                    resultList.Add(new ChequeBookLostRowDto
                    {
                        MemberId = GetString(r, "MemberId"),
                        MemberName = GetString(r, "Name", "MemberName"),
                        AccountNo = GetString(r, "AccountNo"),
                        ChequeNo = GetLong(r, "ChequeNo"),
                        ChequeIssueDateBs = GetString(r, "IssueDate", "ChequeIssueOnBs", "ChequeIssueDateBs"),
                        LostDateBs = GetString(r, "LostDate", "LostDateBs", "LastModifiedOnBs", "LastModifiedOn", "ModifiedOnBs", "DeletedOnBs", "LostOnBs", "LostOn", "ChequeLostOnBs", "ChequeLostDateBs", "DateLost"),
                        Status = GetString(r, "Status"),
                        Operator = GetString(r, "Operator", "OperatorName", "LastModifiedBy", "ModifiedBy", "UserName", "FullName", "CreatedByName"),
                        BranchName = GetString(r, "BranchName")
                    });
                }

                return new ChequeBookLostData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalChequesLost = resultList.Count, // Each row represents a lost cheque
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
                _logger.LogError(ex, "Error in GetReportDataAsync for Cheque Book Lost Report");
                throw;
            }
        }

        // --------------------------------------------------------------
        // Case-insensitive, multi-alias lookups into a Dapper dynamic row.
        // --------------------------------------------------------------
        private static string? GetString(IDictionary<string, object> row, params string[] keys)
        {
            foreach (var key in keys)
            {
                var match = row.Keys.FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
                if (match != null && row[match] != null && row[match] != DBNull.Value)
                    return row[match].ToString();
            }
            return null;
        }

        private static long? GetLong(IDictionary<string, object> row, params string[] keys)
        {
            foreach (var key in keys)
            {
                var match = row.Keys.FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
                if (match != null && row[match] != null && row[match] != DBNull.Value &&
                    long.TryParse(row[match].ToString(), out var val))
                {
                    return val;
                }
            }
            return null;
        }

        private static string BuildOrderByClause(string orderBy)
        {
            // NOTE: the issue-date column belongs to the "i" alias (see the
            // filter above: i.ChequeIssueOn), not "c" — "c" is the lost/cheque
            // table and only has ChequeNo/LastModifiedBy/LastModifiedOn.
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
                case "Cheque No":
                    result = " ORDER BY c.ChequeNo";
                    break;
                case "Issue Date":
                    result = " ORDER BY i.ChequeIssueOnBs";
                    break;
                case "Operator":
                    result = " ORDER BY c.LastModifiedBy";
                    break;
                case "Lost Date":
                    result = " ORDER BY c.LastModifiedOn";
                    break;
                default:
                    result = " ORDER BY i.ChequeIssueOnBs";
                    break;
            }

            return result;
        }
    }
}