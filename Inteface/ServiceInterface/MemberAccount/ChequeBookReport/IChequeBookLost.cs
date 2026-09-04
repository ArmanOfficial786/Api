using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.ChequeBookReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.ChequeBookReport
{
    public interface IChequeBookLost
    {
        Task<ChequeBookLostData> GetReportDataAsync(ChequeBookLostRequestDto request);
    }
}
