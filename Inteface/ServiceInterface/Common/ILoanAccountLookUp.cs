using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface.Common
{
    public interface ILoanAccountLookUp
    {
        Task<Pagination<LoanAccountLookUpDtos>> GetLoanAccountListAsync(
         LoanAccountLookUpRequest request,
         long userId);

        Task<LoanAccountSelectedDto?> GetSelectedLoanAccountAsync(
            long loanIssueId,
            long userId);
    }
}
