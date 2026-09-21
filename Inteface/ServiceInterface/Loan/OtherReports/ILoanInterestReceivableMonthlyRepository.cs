using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports
{
    public interface ILoanInterestReceivableMonthlyRepository
    {
        Task<LoanInterestReceivableMonthlyData> GetReportDataAsync(LoanInterestReceivableMonthlyRequestDto request);
    }
}