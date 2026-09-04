// Interfaces/ServiceInterface/MemberAccount/ChequeBookReport/IChequeBookIssueRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.ChequeBookReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.ChequeBookReport
{
    public interface IChequeBookIssueRepository
    {
        Task<ChequeBookIssueData> GetReportDataAsync(ChequeBookIssueRequestDto request);
    }


}