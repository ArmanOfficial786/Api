// Inteface/ServiceInterface/Loan/OtherReports/ILoanReScheduleRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports
{
    public interface ILoanReScheduleRepository
    {
        Task<LoanReScheduleData> GetReportDataAsync(LoanReScheduleRequestDto request);
    }
}