using NexgenCosysReport.Dtos.RequestDtos.Account;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account
{
    public interface IPLAccount
    {
        Task<object> GetPLAccountReport(PLAccountRequest request);
        // Returns either PLAccountHorizontalData or PLAccountVerticalData based on DisplayType
    }
}
