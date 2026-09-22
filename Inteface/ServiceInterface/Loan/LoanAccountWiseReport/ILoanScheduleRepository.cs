// Inteface/ServiceInterface/Loan/OtherReports/ILoanScheduleRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports
{
    public interface ILoanScheduleRepository
    {
        Task<LoanScheduleData> GetReportDataAsync(LoanScheduleRequestDto request);
    }
}