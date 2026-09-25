// Inteface/ServiceInterface/Microfinance/MicrofinanceReport/IGroupWiseMemberAccountDetailsRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MicroFinance.MicroFinanceReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Microfinance.MicrofinanceReport
{
    public interface IGroupWiseMemberAccountDetailsRepository
    {
        Task<GroupWiseMemberAccountDetailsData> GetReportDataAsync(GroupWiseMemberAccountDetailsRequestDto request);
    }
}