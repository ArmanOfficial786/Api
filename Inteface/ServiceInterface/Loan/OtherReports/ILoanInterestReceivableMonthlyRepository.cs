// Inteface/ServiceInterface/Loan/OtherReports/ILoanInterestReceivableMonthlyRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports
{
    public interface ILoanInterestReceivableMonthlyRepository
    {
        Task<LoanInterestReceivableMonthlyData> GetReportDataAsync(LoanInterestReceivableMonthlyRequestDto request);
    }
}