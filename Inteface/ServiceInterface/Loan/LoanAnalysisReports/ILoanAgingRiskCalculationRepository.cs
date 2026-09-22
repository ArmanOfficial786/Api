
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport
{
    public interface ILoanAgingRiskCalculationRepository
    {
        Task<LoanAgingRiskCalculationData> GetReportDataAsync(LoanAgingRiskCalculationRequestDto request);
    }
}