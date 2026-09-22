using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports
{
    public interface ILoanInterestDiscountRepository
    {
        Task<LoanInterestDiscountData> GetReportDataAsync(LoanInterestDiscountRequestDto request);
    }
}