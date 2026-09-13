// Inteface/ServiceInterface/Account/IBTReports/IIBTStatementBranchwiseRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Account.IBTReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.IBTReports
{
    public interface IIBTStatementBranchwiseRepository
    {
        Task<IBTStatementBranchwiseData> GetReportDataAsync(IBTStatementBranchwiseRequestDto request);
    }
}