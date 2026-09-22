
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport
{
    public interface ILoanInformationRepository
    {
        Task<LoanInformationData> GetReportDataAsync(LoanInformationRequestDto request);
    }
}