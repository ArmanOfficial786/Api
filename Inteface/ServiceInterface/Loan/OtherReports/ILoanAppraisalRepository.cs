// Inteface/ServiceInterface/Loan/OtherReports/ILoanAppraisalRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports
{
    public interface ILoanAppraisalRepository
    {
        Task<LoanAppraisalData> GetReportDataAsync(LoanAppraisalRequestDto request);
    }
}