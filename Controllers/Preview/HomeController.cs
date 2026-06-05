using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.RequestDtos.Member;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Member;

namespace NexgenCosysReport.Controllers.Preview
{
    public class HomeController : Controller
    {
        private readonly IMemberDetail _memberDetail;
        private readonly ICommonHeaderRepository _commonHeaderRepository;

        public HomeController(IMemberDetail memberDetail, ICommonHeaderRepository commonHeaderRepository)
        {
            _memberDetail = memberDetail;
            _commonHeaderRepository = commonHeaderRepository;
        }

        [HttpGet("preview/member-report")]
        public async Task<IActionResult> MemberReport()
        {
            var allMemberData = await _memberDetail.GetMemberRegistrationDetail(
                new MemberDetailRequest
                {
                    fromDate = "2024-01-01",
                    toDate = "2024-12-31",
                    memberGroupId = 0,

                });

            var headerData = await _commonHeaderRepository.GetCommonHeaders();

            var model = new Dictionary<string, object>
            {
                { "StudentDataSet", allMemberData ?? [] },
                { "HeaderDataSet",  headerData ?? [] }
            };

            //return View("~/Views/Report/MemberReport.cshtml", model);
            return View("~/Views/Report/MemberIdCard.cshtml", model);
        }
    }
}
