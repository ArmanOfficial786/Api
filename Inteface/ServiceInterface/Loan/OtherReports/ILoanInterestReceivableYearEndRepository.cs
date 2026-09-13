// Inteface/ServiceInterface/Loan/OtherReports/ILoanInterestReceivableYearEndRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports
{
    public interface ILoanInterestReceivableYearEndRepository
    {
        Task<LoanInterestReceivableYearEndData> GetReportDataAsync(LoanInterestReceivableYearEndRequestDto request);
    }
}