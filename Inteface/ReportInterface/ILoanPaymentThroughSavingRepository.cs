using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport
{
    public interface ILoanPaymentThroughSavingRepository
    {
        Task<LoanPaymentThroughSavingData> GetReportDataAsync(LoanPaymentThroughSavingRequestDto request);
        Task<List<string>> GetBranchNamesListAsync(List<long> branchIds);
    }
}