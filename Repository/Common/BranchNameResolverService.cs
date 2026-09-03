// Services/Common/BranchNameResolverService.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Inteface.ReportInterface;

namespace NexgenCosysReport.Services.Common
{
    public class BranchNameResolverService : IBranchNameResolverService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BranchNameResolverService> _logger;

        public BranchNameResolverService(AppDbContext context, ILogger<BranchNameResolverService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string> GetBranchNamesAsync(string? officeIdsCsv, string allLabel = "All")
        {
            if (string.IsNullOrWhiteSpace(officeIdsCsv) || officeIdsCsv == "-1" || officeIdsCsv == "string")
                return allLabel;

            try
            {
                var ids = officeIdsCsv
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(id => long.TryParse(id, out var parsed) ? parsed : (long?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .ToList();

                if (ids.Count == 0)
                    return officeIdsCsv;

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                const string sql = "SELECT OfficeName FROM UsmOffice WHERE UsmOfficeId IN @Ids ORDER BY OfficeName";
                var names = (await connection.QueryAsync<string>(sql, new { Ids = ids })).ToList();

                return names.Count > 0 ? string.Join(", ", names) : officeIdsCsv;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetBranchNamesAsync (officeIdsCsv={OfficeIdsCsv})", officeIdsCsv);
                return officeIdsCsv;
            }
        }
    }
}