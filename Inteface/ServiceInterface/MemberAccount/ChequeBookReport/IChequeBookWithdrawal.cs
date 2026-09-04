using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.ChequeBookReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.ChequeBookReport
{
    public interface IChequeBookWithdrawal
    {
        Task<ChequeBookWithdrawalData> GetReportDataAsync(ChequeBookWithdrawalRequestDto request);
    }
}
