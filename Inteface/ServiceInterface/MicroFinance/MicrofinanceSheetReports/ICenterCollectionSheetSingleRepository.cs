// Inteface/ServiceInterface/Microfinance/MicrofinanceSheetReports/ICenterCollectionSheetSingleRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Microfinance.MicrofinanceSheetReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Microfinance.MicrofinanceSheetReports
{
    public interface ICenterCollectionSheetSingleRepository
    {
        Task<CenterCollectionSheetSingleData> GetReportDataAsync(CenterCollectionSheetSingleRequestDto request);
    }
}