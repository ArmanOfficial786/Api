using NexgenCosysReport.Dtos.RequestDtos.Account.AccountingReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.AccountingReport
{
    public interface ICashFlowDetail
    {
        Task<CashFlowDetailsData> GetCashFlowDetails(CashFlowDetailsRequest request);
    }
}
