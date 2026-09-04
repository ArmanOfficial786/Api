// Interfaces/ServiceInterface/MemberAccount/FixedDepositCertificateSchedule/IFixedDepositCertificateScheduleRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestPayableReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.InterestPayableReport
{
    public interface IFixedDepositCertificateScheduleRepository
    {
        Task<FixedDepositCertificateScheduleData> GetCertificateDataAsync(
            FixedDepositCertificateScheduleRequestDto request, long userId);

        Task<FixedDepositCertificateScheduleData> GetScheduleDataAsync(
            FixedDepositCertificateScheduleRequestDto request, long userId);
    }


}