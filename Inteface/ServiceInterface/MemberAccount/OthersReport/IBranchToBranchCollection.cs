using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport
{
    public interface IBranchToBranchCollection
    {
        Task<BranchToBranchCollectionData> GetReportDataAsync(BranchToBranchCollectionRequestDto request);
        Task<string?> GetOfficeNameByIdAsync(long officeId);
        Task<string?> GetCollectorNameByIdAsync(long collectorId);
    }
}
