using JsSampleReport.Dtos.RequestDtos;
using JsSampleReport.Inteface.ServiceInterface;
using JsSampleReport.Utils.Enum;
using static JsSampleReport.Utils.Enum.Enums;

namespace JsSampleReport.Services.CommonService
{
    public class OrderByService : IOrderBy
    {
        // ✅ Generic helper — converts any enum to list
        private List<OrderByResponse> ConvertEnumToList<TEnum>() where TEnum : Enum
        {
            return Enum.GetValues(typeof(TEnum))
                .Cast<TEnum>()
                .Select(e => new OrderByResponse
                {
                    Value = Convert.ToInt32(e),
                    //DisplayName = e.ToString()
                    DisplayName = e.GetDisplayName()
                })
                .ToList();
        }

        public AllReportOrderByResponseModel GetAllReportOrderBy()
        {
            return new AllReportOrderByResponseModel
            {
                MemberIdCard = ConvertEnumToList<MemberIdCardOrderBy>(),
                SavingTypeWiseBalance = ConvertEnumToList<SavingTypeWiseBalanceOrderBy>()
                // ✅ Add more: AccountStatement = ConvertEnumToList<AccountStatementOrderBy>()
            };
        }
    }
}
