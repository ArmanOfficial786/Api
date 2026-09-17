//// Repositories/Implementations/Loan/OtherReports/LoanFollowUpRepository.cs
//using Dapper;
//using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore;
//using NexgenCosysReport.DbContext;
//using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports;
//using System.Data;
//using System.Text;

//namespace NexgenCosysReport.Repository.Loan.OtherReports
//{
//    public class LoanFollowUpRepository : ILoanFollowUpRepository
//    {
//        private readonly AppDbContext _context;
//        private readonly IDateConverterService _dateConverter;
//        private readonly ILogger<LoanFollowUpRepository> _logger;

//        public LoanFollowUpRepository(
//            AppDbContext context,
//            IDateConverterService dateConverter,
//            ILogger<LoanFollowUpRepository> logger)
//        {
//            _context = context;
//            _dateConverter = dateConverter;
//            _logger = logger;
//        }


//        private static string BuildSqlOrderBy(LoanFollowUpRequestDto request)
//        {
//            return request.OrderBy?.Trim() switch
//            {
//                "Member Name" => " order by MemberName ",
//                "Member Id" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId  ",
//                "Account No" => " order by substring(LoanAccountNo, 1,(len(LoanAccountNo)-charindex('-', LoanAccountNo))-1), LoanAccountNo",
//                "Follow Up Person" => "order by FollowUpPerson",
//                "Follow Up Date" => " order by FollowUpDateOn ",
//                "Follow Up By" => " order by FollowUpBy ",
//                _ => string.Empty
//            };
//        }

//        // --------------------------------------------------------------
//        // Guards against injection through the comma-separated branch id
//        // list, same pattern used across the other reports.
//        // --------------------------------------------------------------
//        private static string SanitizeBranchIds(string? branchIds)
//        {
//            if (string.IsNullOrWhiteSpace(branchIds) || branchIds == "-1" || branchIds == "string")
//                return string.Empty;

//            var validIds = branchIds
//                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
//                .Where(id => long.TryParse(id, out _));

//            return string.Join(",", validIds);
//        }

//        // --------------------------------------------------------------
//        // Resolves a human-readable MemberId code (e.g. "M-001") to the
//        // internal MemMemberRegistrationId used by the SP's filter, via
//        // the active-member lookup — mirrors CMemMemberRegistration
//        // .GetByActiveMemMemberId used in the legacy WebForm.
//        // --------------------------------------------------------------
//        private async Task<long?> ResolveMemberRegistrationIdAsync(SqlConnection connection, string memberIdCode)
//        {
//            return await connection.QueryFirstOrDefaultAsync<long?>(
//                "SELECT MemMemberRegistrationId FROM MemMemberRegistration WHERE MemberId = @MemberId AND IsActive = 1",
//                new { MemberId = memberIdCode.Trim() });
//        }

//        public async Task<LoanFollowUpData> GetReportDataAsync(LoanFollowUpRequestDto request)
//        {
//            try
//            {
//                var branchIds = SanitizeBranchIds(request.BranchId);

//                var connectionString = _context.Database.GetConnectionString();
//                using var connection = new SqlConnection(connectionString);
//                await connection.OpenAsync();

//                var sqlFilterExp = new StringBuilder();
//                string? memberName = null;
//                long memberRegistrationId = -1;

//                // --------------------------------------------------------------
//                // Mode selection mirrors the legacy WebForm's two buttons:
//                //   btnViewByMemberId -> filters by mr.MemMemberRegistrationId
//                //   btnViewByDate     -> filters by lfu.FollowUpDateOn range
//                // A supplied MemberId always takes priority, matching the
//                // legacy BLL's `if (memberId != -1) ... else if (dates) ...`
//                // --------------------------------------------------------------
//                if (!string.IsNullOrWhiteSpace(request.MemberId))
//                {
//                    var resolvedId = await ResolveMemberRegistrationIdAsync(connection, request.MemberId);
//                    if (!resolvedId.HasValue)
//                    {
//                        throw new ArgumentException($"No active member found for Member Id '{request.MemberId}'.");
//                    }

//                    memberRegistrationId = resolvedId.Value;
//                    sqlFilterExp.Append(" And mr.MemMemberRegistrationId = ").Append(memberRegistrationId);

//                    var name = await connection.QueryFirstOrDefaultAsync<string>(
//                        @"SELECT FirstName + ' ' +
//                                 CASE WHEN MiddleName = '' THEN '' ELSE MiddleName + ' ' END +
//                                 LastName
//                          FROM MemMemberRegistration WHERE MemMemberRegistrationId = @Id",
//                        new { Id = memberRegistrationId });
//                    memberName = name;
//                }
//                else if (!string.IsNullOrEmpty(request.FromDateBs) && !string.IsNullOrEmpty(request.ToDateBs))
//                {
//                    var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
//                    var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

//                    // ISO format avoids SQL Server regional/language ambiguity for string->date literals
//                    var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
//                    var toDateStr = toDateAd.ToString("yyyy-MM-dd");

//                    sqlFilterExp.Append(" And lfu.FollowUpDateOn between '")
//                                .Append(fromDateStr).Append("' And '")
//                                .Append(toDateStr).Append("' ");
//                }
//                else
//                {
//                    throw new ArgumentException("Either MemberId or both FromDateBs and ToDateBs must be supplied.");
//                }

//                if (!string.IsNullOrEmpty(branchIds))
//                {
//                    sqlFilterExp.Append(" And us.UsmOfficeId in (").Append(branchIds).Append(")");
//                }

//                var sqlFilterExpOrder = BuildSqlOrderBy(request);

//                var parameters = new DynamicParameters();
//                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
//                parameters.Add("@SqlFilterExpOrder", sqlFilterExpOrder, DbType.String, size: -1);

//                var rows = await connection.QueryAsync<LoanFollowUpRowDto>(
//                    "sp_7_16_LoanFollowUpReport",
//                    parameters,
//                    commandType: CommandType.StoredProcedure,
//                    commandTimeout: 120
//                );

//                var resultList = rows.AsList();

//                string branchName = "All";
//                if (!string.IsNullOrEmpty(branchIds))
//                {
//                    var names = await connection.QueryAsync<string>(
//                        $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");
//                    var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
//                    branchName = nameList.Count > 0 ? string.Join(", ", nameList) : "All";
//                }

//                return new LoanFollowUpData
//                {
//                    Rows = resultList,
//                    TotalRecords = resultList.Count,
//                    FromDateBs = request.FromDateBs,
//                    ToDateBs = request.ToDateBs,
//                    BranchName = branchName,
//                    MemberName = memberName,
//                    OrderBy = request.OrderBy
//                };
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error in GetReportDataAsync");
//                throw;
//            }
//        }
//    }
//}






// Repositories/Implementations/Loan/OtherReports/LoanFollowUpRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports;
using System.Data;
using System.Text;

namespace NexgenCosysReport.Repository.Loan.OtherReports
{
    public class LoanFollowUpRepository : ILoanFollowUpRepository
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<LoanFollowUpRepository> _logger;

        public LoanFollowUpRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<LoanFollowUpRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        // Carries everything BuildSqlFilter resolves in one pass, so the
        // member lookup is not repeated once for the filter and again for
        // the report header.
        private sealed record FilterResult(string Filter, string? MemberName);

        public async Task<LoanFollowUpData> GetReportDataAsync(LoanFollowUpRequestDto request)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var branchIds = SanitizeBranchIds(request.BranchId);
                var filterResult = await BuildSqlFilter(connection, request, branchIds);

                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", filterResult.Filter, DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrder", BuildOrderBy(request.OrderBy), DbType.String, size: -1);

                var rows = await connection.QueryAsync<LoanFollowUpRowDto>(
                    "sp_7_16_LoanFollowUpReport",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120);

                var resultList = rows.AsList();

                return new LoanFollowUpData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchName = await GetBranchName(connection, branchIds),
                    MemberName = filterResult.MemberName,
                    OrderBy = request.OrderBy
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync");
                throw;
            }
        }

        // --------------------------------------------------------------
        // Mode selection mirrors the legacy WebForm's two buttons:
        //   btnViewByMemberId -> filters by mr.MemMemberRegistrationId
        //   btnViewByDate     -> filters by lfu.FollowUpDateOn range
        // A supplied MemberId always takes priority, matching the legacy
        // BLL's `if (memberId != -1) ... else if (dates) ...`
        // --------------------------------------------------------------
        private async Task<FilterResult> BuildSqlFilter(
            SqlConnection connection,
            LoanFollowUpRequestDto request,
            string branchIds)
        {
            var sqlFilterExp = new StringBuilder();
            string? memberName = null;

            if (!string.IsNullOrWhiteSpace(request.MemberId))
            {
                var member = await ResolveMemberAsync(connection, request.MemberId);
                if (member is null)
                    throw new ArgumentException($"No active member found for Member Id '{request.MemberId}'.");

                sqlFilterExp.Append(" And mr.MemMemberRegistrationId = ")
                            .Append(member.Value.MemMemberRegistrationId);
                memberName = member.Value.MemberName;
            }
            else if (!string.IsNullOrEmpty(request.FromDateBs) && !string.IsNullOrEmpty(request.ToDateBs))
            {
                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                // ISO format avoids SQL Server regional/language ambiguity
                // for string -> date literals.
                sqlFilterExp.Append(" And lfu.FollowUpDateOn between '")
                            .Append(fromDateAd.ToString("yyyy-MM-dd"))
                            .Append("' And '")
                            .Append(toDateAd.ToString("yyyy-MM-dd"))
                            .Append("' ");
            }
            else
            {
                throw new ArgumentException("Either MemberId or both FromDateBs and ToDateBs must be supplied.");
            }

            if (!string.IsNullOrEmpty(branchIds))
            {
                sqlFilterExp.Append(" And us.UsmOfficeId in (").Append(branchIds).Append(")");
            }

            return new FilterResult(sqlFilterExp.ToString(), memberName);
        }

        private static string BuildOrderBy(string? orderBy)
        {
            return orderBy?.Trim() switch
            {
                "Member Name" => " order by MemberName ",
                "Member Id" => " order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId  ",
                "Account No" => " order by substring(LoanAccountNo, 1,(len(LoanAccountNo)-charindex('-', LoanAccountNo))-1), LoanAccountNo",
                "Follow Up Person" => " order by FollowUpPerson",
                "Follow Up Date" => " order by FollowUpDateOn ",
                "Follow Up By" => " order by FollowUpBy ",
                _ => string.Empty
            };
        }

        // --------------------------------------------------------------
        // Guards against injection through the comma-separated branch id
        // list, same pattern used across the other reports.
        // --------------------------------------------------------------
        private static string SanitizeBranchIds(string? branchIds)
        {
            if (string.IsNullOrWhiteSpace(branchIds) || branchIds == "-1" || branchIds == "string")
                return string.Empty;

            var validIds = branchIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => long.TryParse(id, out _));

            return string.Join(",", validIds);
        }

        // --------------------------------------------------------------
        // Resolves a human-readable MemberId code (e.g. "M-001") to the
        // internal MemMemberRegistrationId used by the SP's filter, and
        // returns the display name in the same round trip — mirrors
        // CMemMemberRegistration.GetByActiveMemMemberId in the legacy WebForm.
        // --------------------------------------------------------------
        private static async Task<(long MemMemberRegistrationId, string? MemberName)?> ResolveMemberAsync(
            SqlConnection connection,
            string memberIdCode)
        {
            var row = await connection.QueryFirstOrDefaultAsync<(long MemMemberRegistrationId, string? MemberName)?>(
                @"SELECT MemMemberRegistrationId,
                         FirstName + ' ' +
                         CASE WHEN MiddleName = '' THEN '' ELSE MiddleName + ' ' END +
                         LastName AS MemberName
                  FROM MemMemberRegistration
                  WHERE MemberId = @MemberId AND IsActive = 1",
                new { MemberId = memberIdCode.Trim() });

            return row;
        }

        private static async Task<string> GetBranchName(SqlConnection connection, string branchIds)
        {
            if (string.IsNullOrEmpty(branchIds))
                return "All";

            var names = await connection.QueryAsync<string>(
                $"SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN ({branchIds})");

            var nameList = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
            return nameList.Count > 0 ? string.Join(", ", nameList) : "All";
        }
    }
}