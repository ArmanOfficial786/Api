using NexgenCosysReport.Dtos.RequestDtos.Account.AccountingReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.AccountingReport
{
    public interface ICostofFund
    {
        Task<CostOfFundData> GetCostOfFund(CostOfFundRequest request);
    }
}
