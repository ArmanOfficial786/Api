namespace NexgenCosysReport.Dtos.RequestDtos.Common
{
    public class ComCalenderReqResponse
    {
    }

    public class YearsResponseDto
    {
        public List<int> Years { get; set; } = new List<int>();
    }
    public class DaysResponseDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public List<int> Days { get; set; } = new();
    }
    public class ConvertRequestDto
    {
        public string Direction { get; set; } = "ADtoBS"; // "ADtoBS" or "BStoAD"
        public string Date { get; set; } = ""; // "yyyy-MM-dd" for AD or "yyyy-MM-dd" for BS
    }
    public class ConvertResponseDto
    {
        public string ConvertedDate { get; set; } = ""; // "yyyy-MM-dd"
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
    }
}
