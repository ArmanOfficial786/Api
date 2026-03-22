using JsSampleProject.Interface;
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
        public IActionResult MemberReport()
        {
            var allMemberData = _memberDetail.GetMemberRegistrationDetail(
                new JsSampleProject.Dtos.MemberDtos.MemberDetailRequest
                {
                    fromDate = "2024-01-01",
                    toDate = "2024-12-31",
                    branchId = 1,
                    memberGroupId = 0,
                    currentPage = 0,
                    pageSize = 0
                });

            var headerData = _memberDetail.GetCommonHeaders();

            var model = new Dictionary<string, object>
            {
                { "StudentDataSet", allMemberData ?? [] },
                { "HeaderDataSet",  headerData ?? [] }
            };

            return View("~/Views/Report/MemberReport.cshtml", model);
        }
    }
}
