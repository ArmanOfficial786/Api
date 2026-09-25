// Inteface/ServiceInterface/Microfinance/MicrofinanceReport/ILoanAgeingArrearCalculationRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MicroFinance.MicroFinanceReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Microfinance.MicrofinanceReport
{
    public interface ILoanAgeingArrearCalculationRepository
    {
        Task<LoanAgeingArrearCalculationData> GetReportDataAsync(LoanAgeingArrearCalculationRequestDto request);
        Task<LoanAgeingArrearCalculationData> GetExcelReportDataAsync(LoanAgeingArrearCalculationRequestDto request);
    }
}