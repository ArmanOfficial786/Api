using JsSampleReport.Dtos.ReportDtos;

namespace JsSampleReport.Inteface.ServiceInterface
{
    public interface ICommonHeaderRepository
    {
        Task<List<CommonHeader>> GetCommonHeaders(string branchId = "");
    }
}
