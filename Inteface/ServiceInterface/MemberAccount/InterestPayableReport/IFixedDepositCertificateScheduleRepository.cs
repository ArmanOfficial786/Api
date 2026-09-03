// Interfaces/ServiceInterface/MemberAccount/FixedDepositCertificateSchedule/IFixedDepositCertificateScheduleRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestPayableReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.InterestPayableReport
{
    public interface IFixedDepositCertificateScheduleRepository
    {
        Task<FixedDepositCertificateScheduleData> GetCertificateDataAsync(FixedDepositCertificateScheduleRequestDto request);
        Task<FixedDepositCertificateScheduleData> GetScheduleDataAsync(FixedDepositCertificateScheduleRequestDto request);
        Task<List<FixedDepositAccountListDto>> GetFixedDepositAccountsAsync(long userId);
    }

    public class FixedDepositAccountListDto
    {
        public long MamAccountOpeningId { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? AccountNo { get; set; }
        public string? DepositType { get; set; }
        public string? Status { get; set; }
    }
}