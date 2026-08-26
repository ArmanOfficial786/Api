using NexgenCosysReport.Dtos.RequestDtos.Account.AccountingReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.AccountingReport
{
    public interface IPLAccount
    {
        Task<object> GetPLAccountReport(PLAccountRequest request);
        // Returns either PLAccountHorizontalData or PLAccountVerticalData based on DisplayType
    }
}
