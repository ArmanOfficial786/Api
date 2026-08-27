using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport
{
    public interface IDataEdited
    {
        Task<DataEditedReportData> GetReportDataAsync(DataEditedReportRequestDto request);
    }
}
