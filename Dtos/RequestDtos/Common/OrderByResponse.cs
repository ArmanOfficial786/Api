namespace JsSampleReport.Dtos.RequestDtos.Common
{
    public class OrderByResponse
    {
        public int Value { get; set; }
        public string? DisplayName { get; set; }
    }
    // ✅ Single model that holds ALL report enums
    public class AllReportOrderByResponseModel
    {
        public List<OrderByResponse>? MemberIdCard { get; set; }
        public List<OrderByResponse>? SavingTypeWiseBalance { get; set; }
        // ✅ Add more reports here as needed
    }

}
