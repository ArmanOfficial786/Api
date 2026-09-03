using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport
{
    public interface ISavingTransferRepository
    {
        Task<SavingTransferData> GetReportDataAsync(SavingTransferRequestDto request);

    }
}