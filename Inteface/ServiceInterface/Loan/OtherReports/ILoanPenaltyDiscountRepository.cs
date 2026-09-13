// Inteface/ServiceInterface/Loan/OtherReports/ILoanPenaltyDiscountRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports
{
    public interface ILoanPenaltyDiscountRepository
    {
        Task<LoanPenaltyDiscountData> GetReportDataAsync(LoanPenaltyDiscountRequestDto request);
    }
}