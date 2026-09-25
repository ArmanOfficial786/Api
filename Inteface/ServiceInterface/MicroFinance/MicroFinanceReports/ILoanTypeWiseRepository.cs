// Inteface/ServiceInterface/Microfinance/MicrofinanceReport/ILoanTypeWiseRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MicroFinance.MicroFinanceReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Microfinance.MicrofinanceReport
{
    public interface ILoanTypeWiseRepository
    {
        Task<LoanTypeWiseData> GetReportDataAsync(LoanTypeWiseRequestDto request);
    }
}