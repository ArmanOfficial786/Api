// Inteface/ServiceInterface/Microfinance/MicrofinanceSheetReports/ICenterCollectionRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Microfinance.MicrofinanceSheetReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Microfinance.MicrofinanceSheetReports
{
    public interface ICenterCollectionRepository
    {
        Task<CenterCollectionData> GetReportDataAsync(CenterCollectionRequestDto request);
    }
}