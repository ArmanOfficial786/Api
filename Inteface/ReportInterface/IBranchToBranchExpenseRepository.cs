using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;



namespace NexgenCosysReport.Inteface.ReportInterface
{
    public interface IBranchToBranchExpenseRepository
    {
        Task<BranchToBranchExpenseData> GetReportDataAsync(BranchToBranchExpenseRequestDto request);
        Task<string?> GetOfficeNameByIdAsync(long officeId);
        Task<string?> GetCollectorNameByIdAsync(long collectorId);
    }
}
