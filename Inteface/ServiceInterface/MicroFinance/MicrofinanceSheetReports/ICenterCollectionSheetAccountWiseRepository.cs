// Inteface/ServiceInterface/Microfinance/MicrofinanceSheetReports/ICenterCollectionSheetAccountWiseRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Microfinance.MicrofinanceSheetReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Microfinance.MicrofinanceSheetReports
{
    public interface ICenterCollectionSheetAccountWiseRepository
    {
        Task<CenterCollectionSheetAccountWiseData> GetReportDataAsync(CenterCollectionSheetAccountWiseRequestDto request);
    }
}