using JsSampleReport.Dtos.RequestDtos;
using JsSampleReport.Inteface.ServiceInterface;
using Microsoft.AspNetCore.Mvc;

namespace JsSampleReport.Controllers.Preview
{
    public class HomeController : Controller
    {
        private readonly IMemberDetail _memberDetail;

        public HomeController(IMemberDetail memberDetail)
        {
            _memberDetail = memberDetail;
        }

        [HttpGet("preview/member-report")]
        public async Task<IActionResult> MemberReport()
        {
            var allMemberData = await _memberDetail.GetMemberRegistrationDetail(
                new MemberDetailRequest
                {
                    fromDate = "2024-01-01",
                    toDate = "2024-12-31",
                    branchId = 1,
                    memberGroupId = 0,
                    currentPage = 0,
                    pageSize = 0
                });

            var headerData = await _memberDetail.GetCommonHeaders();

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
