using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface.Common
{
    public interface ILedgerLookupRepository
    {
        Task<List<LedgerHeadRowDto>> GetLedgerHeadsAsync();
        Task<List<LedgerNameRowDto>> GetLedgerNamesAsync(LedgerNameRequestDto request);
        Task<List<SubLedgerNameRowDto>> GetSubLedgerNamesAsync(SubLedgerNameRequestDto request);
    }
}
