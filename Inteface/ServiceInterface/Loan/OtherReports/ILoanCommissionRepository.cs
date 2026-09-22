using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports
{
    public interface ILoanCommissionRepository
    {
        Task<LoanCommissionData> GetReportDataAsync(LoanCommissionRequestDto request);
    }
}