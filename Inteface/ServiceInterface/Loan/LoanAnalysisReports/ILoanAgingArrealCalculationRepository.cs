
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport
{
    public interface ILoanAgingArrealCalculationRepository
    {
        Task<LoanAgingArrealCalculationData> GetReportDataAsync(LoanAgingArrealCalculationRequestDto request);
        Task<List<LoanAgingArrealExcelExportDto>> GetExcelExportDataAsync(LoanAgingArrealCalculationRequestDto request);
    }
}