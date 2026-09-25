// Inteface/ServiceInterface/Microfinance/MicrofinanceSheetReports/ICenterCollectionSheetPrintRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Microfinance.MicrofinanceSheetReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Microfinance.MicrofinanceSheetReports
{
    public interface ICenterCollectionSheetPrintRepository
    {
        Task<CenterCollectionSheetPrintData> GetReportDataAsync(CenterCollectionSheetPrintRequestDto request);
    }
}