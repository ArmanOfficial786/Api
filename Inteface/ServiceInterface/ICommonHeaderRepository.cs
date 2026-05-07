using NexgenCosysReport.Dtos.ReportDtos;

namespace NexgenCosysReport.Inteface.ServiceInterface
{
    public interface ICommonHeaderRepository
    {
        Task<List<CommonHeader>> GetCommonHeaders(string branchId = "");
    }
}
