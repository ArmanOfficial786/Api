// Inteface/ServiceInterface/Microfinance/CenterDetailReports/ILoanEvaluationRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Microfinance.CenterDetailReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Microfinance.CenterDetailReports
{
    public interface ILoanEvaluationRepository
    {
        Task<LoanEvaluationData> GetReportDataAsync(LoanEvaluationReqResponse request);
    }
}