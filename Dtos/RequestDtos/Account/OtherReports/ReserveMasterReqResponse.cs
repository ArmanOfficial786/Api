// Dtos/RequestDtos/Account/OtherReports/ReserveMasterRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports
{
    public class ReserveMasterRequestDto
    {
        public string FromDate { get; set; } = string.Empty;
        public string ToDate { get; set; } = string.Empty;
        public string? BranchId { get; set; }
        public string OrderBy { get; set; } = "Title";
        public bool VisualReport { get; set; }
    }

    public class ReserveMasterRowDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public decimal? Percentage { get; set; }
        public bool? IsEditable { get; set; }
        public decimal? Amount { get; set; }
    }

    public class ReserveMasterData
    {
        public List<ReserveMasterRowDto> Rows { get; set; } = new List<ReserveMasterRowDto>();
        public int TotalRecords { get; set; }
        public decimal TotalReserveAmount { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetProfit { get; set; }
        public decimal GeneralReserve { get; set; }
        public decimal RemainingReserve { get; set; }
        public decimal ReservePercentage { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchNames { get; set; }
        public string? OrderBy { get; set; }
    }
}