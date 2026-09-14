using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports
{
    public interface ILoanMiscellaneousIncomeRepository
    {
        Task<LoanMiscellaneousIncomeData> GetReportDataAsync(LoanMiscellaneousIncomeRequestDto request);
    }
}