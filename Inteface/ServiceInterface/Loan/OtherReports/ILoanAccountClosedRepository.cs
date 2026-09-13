// Inteface/ServiceInterface/Loan/OtherReports/ILoanAccountClosedRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports
{
    public interface ILoanAccountClosedRepository
    {
        Task<LoanAccountClosedData> GetReportDataAsync(LoanAccountClosedRequestDto request);
    }
}