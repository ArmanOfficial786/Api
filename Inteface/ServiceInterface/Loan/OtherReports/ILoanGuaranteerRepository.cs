// Inteface/ServiceInterface/Loan/OtherReports/ILoanGuaranteerRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports
{
    public interface ILoanGuaranteerRepository
    {
        Task<LoanGuaranteerData> GetReportDataAsync(LoanGuaranteerRequestDto request);
    }
}