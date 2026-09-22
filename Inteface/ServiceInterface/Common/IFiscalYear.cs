using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface.Common
{
    public interface IFiscalYear
    {
        Task<List<FiscalYearResponse>> GetAll();
    }
}
