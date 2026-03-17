using JsSampleProject.Dtos.MemberDtos;
using JsSampleProject.Dtos.ReportDtos;

namespace JsSampleProject.Dtos.ReportDtos
{
    public class MemberRegistrationReportDto : BaseReportDto<MemberRegistrationDetail>
    {
        public int TotalRecords { get; set; }
        public MemberDetailRequest? FilterCriteria { get; set; }
    }
}
