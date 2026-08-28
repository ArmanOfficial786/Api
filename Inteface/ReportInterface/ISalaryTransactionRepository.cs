// Inteface/ServiceInterface/MemberAccount/OthersReport/ISalaryTransactionRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport
{
    public interface ISalaryTransactionRepository
    {
        Task<SalaryTransactionData> GetReportDataAsync(SalaryTransactionRequestDto request);
    }
}