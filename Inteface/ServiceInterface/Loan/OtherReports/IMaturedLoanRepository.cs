using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports
{
    public interface IMaturedLoanRepository
    {
        Task<MaturedLoanData> GetReportDataAsync(MaturedLoanRequestDto request);
    }
}