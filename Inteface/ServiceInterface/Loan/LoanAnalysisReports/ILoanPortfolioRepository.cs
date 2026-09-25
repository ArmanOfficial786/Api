
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport
{
    public interface ILoanPortfolioRepository
    {
        Task<LoanPortfolioData> GetReportDataAsync(LoanPortfolioRequestDto request);
    }
}