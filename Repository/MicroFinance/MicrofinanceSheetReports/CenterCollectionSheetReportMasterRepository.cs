// Repository/Microfinance/MicrofinanceSheetReports/CenterCollectionSheetReportMasterRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Microfinance.MicrofinanceSheetReports;
using NexgenCosysReport.Inteface.ServiceInterface.Microfinance.MicrofinanceSheetReports;

namespace NexgenCosysReport.Repository.Microfinance.MicrofinanceSheetReports
{
    public class CenterCollectionSheetReportMasterRepository : ICenterCollectionSheetReportMasterRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CenterCollectionSheetReportMasterRepository> _logger;

        public CenterCollectionSheetReportMasterRepository(
            AppDbContext context,
            ILogger<CenterCollectionSheetReportMasterRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<CenterCollectionSheetReportMasterDto>> GetAllAsync()
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var rows = await connection.QueryAsync<CenterCollectionSheetReportMasterDto>(
                    @"SELECT SycCollectionCenterScemeId,
                             SavingName,
                             SavingNameCode,
                             LoanName,
                             LoanNameCode
                      FROM   SycCollectionCenterSceme
                      ORDER BY SycCollectionCenterScemeId");

                return rows.AsList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllAsync (CenterCollectionSheetReportMaster)");
                throw;
            }
        }

        public async Task<CenterCollectionSheetReportMasterDto?> GetByIdAsync(int id)
        {
            try
            {
                if (id <= 0)
                    throw new ArgumentException("Invalid scheme id.");

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var row = await connection.QueryFirstOrDefaultAsync<CenterCollectionSheetReportMasterDto>(
                    @"SELECT SycCollectionCenterScemeId,
                             SavingName,
                             SavingNameCode,
                             LoanName,
                             LoanNameCode
                      FROM   SycCollectionCenterSceme
                      WHERE  SycCollectionCenterScemeId = @Id",
                    new { Id = id });

                return row;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetByIdAsync (CenterCollectionSheetReportMaster)");
                throw;
            }
        }

        public async Task<CenterCollectionSheetReportMasterResponseDto> UpdateAsync(CenterCollectionSheetReportMasterUpdateDto dto)
        {
            try
            {
                if (dto.SycCollectionCenterScemeId <= 0)
                    return new CenterCollectionSheetReportMasterResponseDto
                    {
                        Success = false,
                        Message = "Select scheme."
                    };

                if (string.IsNullOrWhiteSpace(dto.SavingName) ||
                    string.IsNullOrWhiteSpace(dto.SavingNameCode) ||
                    string.IsNullOrWhiteSpace(dto.LoanName) ||
                    string.IsNullOrWhiteSpace(dto.LoanNameCode))
                {
                    return new CenterCollectionSheetReportMasterResponseDto
                    {
                        Success = false,
                        Message = "All fields are required."
                    };
                }

                var connectionString = _context.Database.GetConnectionString();
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var affected = await connection.ExecuteAsync(
                    @"UPDATE SycCollectionCenterSceme
                      SET    SavingName     = @SavingName,
                             SavingNameCode = @SavingNameCode,
                             LoanName       = @LoanName,
                             LoanNameCode   = @LoanNameCode
                      WHERE  SycCollectionCenterScemeId = @Id",
                    new
                    {
                        Id = dto.SycCollectionCenterScemeId,
                        SavingName = dto.SavingName.Trim(),
                        SavingNameCode = dto.SavingNameCode.Trim(),
                        LoanName = dto.LoanName.Trim(),
                        LoanNameCode = dto.LoanNameCode.Trim()
                    });

                return affected > 0
                    ? new CenterCollectionSheetReportMasterResponseDto
                    {
                        Success = true,
                        Message = "Updated successfully."
                    }
                    : new CenterCollectionSheetReportMasterResponseDto
                    {
                        Success = false,
                        Message = "Update failed."
                    };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateAsync (CenterCollectionSheetReportMaster)");
                throw;
            }
        }
    }
}