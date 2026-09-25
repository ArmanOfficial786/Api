// Inteface/ServiceInterface/Microfinance/MicrofinanceSheetReports/ICenterCollectionSheetReportRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Microfinance.MicrofinanceSheetReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Microfinance.MicrofinanceSheetReports
{
    public interface ICenterCollectionSheetReportRepository
    {
        Task<CenterCollectionSheetReportData> GetReportDataAsync(CenterCollectionSheetReportRequestDto request);
    }
}