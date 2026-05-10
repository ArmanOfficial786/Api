using NexgenCosysReport.Dtos.ReportDtos;

namespace NexgenCosysReport.Inteface.ServiceInterface.Common
{
    public interface ICommonHeaderRepository
    {
        Task<List<CommonHeader>> GetCommonHeaders(string branchId = "");
    }
}
