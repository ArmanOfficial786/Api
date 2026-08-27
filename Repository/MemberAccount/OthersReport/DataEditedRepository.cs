// Repositories/Implementations/AccountOperation/DataEditedReportRepository.cs
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
    public class DataEditedReportRepository : IDataEdited
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<DataEditedReportRepository> _logger;

        public DataEditedReportRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<DataEditedReportRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<DataEditedReportData> GetReportDataAsync(DataEditedReportRequestDto request)
        {
            try
            {
                var fromDateAd = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDateAd = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);

                // ISO format avoids SQL Server regional/language ambiguity for string->datetime literals
                var fromDateStr = fromDateAd.ToString("yyyy-MM-dd");
                var toDateStr = toDateAd.ToString("yyyy-MM-dd");

                long entryBy = request.EntryBy ?? -1;
                long editedBy = request.EditedBy ?? -1;
                long memberRegistrationId = request.MemberRegistrationId ?? -1;
                string officeIds = string.IsNullOrWhiteSpace(request.BranchIds) ? "-1" : request.BranchIds;

                // ---- Build @SqlFilterExp (saving side, alias t) and
                //      @SqlFilterExpLoan (loan side, alias l) exactly like the legacy BLL method ----
                var sqlFilterExp = new StringBuilder();
                var sqlFilterExpLoan = new StringBuilder();

                sqlFilterExp.Append(" And t.TransactionOn between '")
                            .Append(fromDateStr).Append("' And '")
                            .Append(toDateStr).Append("' ");
                sqlFilterExpLoan.Append(" And l.LoanIssueOn between '")
                                 .Append(fromDateStr).Append("' And '")
                                 .Append(toDateStr).Append("' ");

                if (officeIds != "-1")
                {
                    sqlFilterExp.Append(" And t.UsmOfficeId in (").Append(officeIds).Append(")");
                    sqlFilterExpLoan.Append(" And l.UsmOfficeId in (").Append(officeIds).Append(")");
                }

                if (entryBy != -1)
                {
                    sqlFilterExp.Append(" And t.CreatedBy = ").Append(entryBy);
                    sqlFilterExpLoan.Append(" And l.CreatedBy = ").Append(entryBy);
                }

                if (editedBy != -1)
                {
                    sqlFilterExp.Append(" And t.LastModifiedBy = ").Append(editedBy);
                    sqlFilterExpLoan.Append(" And l.LastModifiedBy = ").Append(editedBy);
                }

                if (memberRegistrationId != -1)
                {
                    sqlFilterExp.Append(" And t.MemMemberRegistrationId = ").Append(memberRegistrationId);
                    sqlFilterExpLoan.Append(" And l.MemMemberRegistrationId = ").Append(memberRegistrationId);
                }

                // ---- Build @SqlFilterExpOrder exactly like the legacy BLL method did ----
                var sqlFilterExpOrder = new StringBuilder();
                switch (request.OrderBy)
                {
                    case "Date":
                        sqlFilterExpOrder.Append(" order by Date");
                        break;
                    case "Member Id":
                        sqlFilterExpOrder.Append(" order by substring(MemberId, 1,(len(MemberId)-charindex('-', MemberId))-1), MemberId");
                        break;
                    case "Account No":
                        sqlFilterExpOrder.Append(" order by substring(AccountNo, 1,(len(AccountNo)-charindex('-', AccountNo))-1), AccountNo");
                        break;
                    case "Description":
                        sqlFilterExpOrder.Append(" order by Description");
                        break;
                    case "Actual Amount":
                        sqlFilterExpOrder.Append(" order by ActualAmount DESC");
                        break;
                    case "Edited Date":
                        sqlFilterExpOrder.Append(" order by EditedDate");
                        break;
                    case "Edited By":
                        sqlFilterExpOrder.Append(" order by EditedBy");
                        break;
                    default:
                        sqlFilterExpOrder.Append(" order by Date");
                        break;
                }

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // ---- Only pass the 3 params the SP actually declares ----
                var parameters = new DynamicParameters();
                parameters.Add("@SqlFilterExp", sqlFilterExp.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpLoan", sqlFilterExpLoan.ToString(), DbType.String, size: -1);
                parameters.Add("@SqlFilterExpOrder", sqlFilterExpOrder.ToString(), DbType.String, size: -1);

                var rows = await connection.QueryAsync<DataEditedRowDto>(
                    "sp_5_43_GetDataEdited",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var resultList = rows.AsList();

                string? entryByName = entryBy != -1 ? await GetUserNameByIdAsync(entryBy) : null;
                string? editedByName = editedBy != -1 ? await GetUserNameByIdAsync(editedBy) : null;

                return new DataEditedReportData
                {
                    Rows = resultList,
                    TotalRecords = resultList.Count,
                    TotalActualAmount = resultList.Sum(r => r.ActualAmount ?? 0),
                    FromDateBs = request.FromDateBs,
                    ToDateBs = request.ToDateBs,
                    BranchName = officeIds == "-1" ? "All" : officeIds,
                    EntryByName = entryBy == -1 ? "All" : (entryByName ?? entryBy.ToString()),
                    EditedByName = editedBy == -1 ? "All" : (editedByName ?? editedBy.ToString()),
                    OrderBy = request.OrderBy
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportDataAsync");
                throw;
            }
        }

        private async Task<string?> GetUserNameByIdAsync(long userId)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                const string sql = "SELECT FullName FROM UsmUser WHERE UsmUserId = @UserId";
                return await connection.ExecuteScalarAsync<string>(sql, new { UserId = userId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserNameByIdAsync");
                return null;
            }
        }
    }
}