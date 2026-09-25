// Inteface/ServiceInterface/Microfinance/MicrofinanceSheetReports/ICenterCollectionSheetReportMasterRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Microfinance.MicrofinanceSheetReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Microfinance.MicrofinanceSheetReports
{
    public interface ICenterCollectionSheetReportMasterRepository
    {
        Task<List<CenterCollectionSheetReportMasterDto>> GetAllAsync();
        Task<CenterCollectionSheetReportMasterDto?> GetByIdAsync(int id);
        Task<CenterCollectionSheetReportMasterResponseDto> UpdateAsync(CenterCollectionSheetReportMasterUpdateDto dto);
    }
}