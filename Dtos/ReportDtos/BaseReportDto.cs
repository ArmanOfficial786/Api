namespace JsSampleProject.Dtos.ReportDtos
{
    public class BaseReportDto<T> where T : class
    {
        public CommonHeader? Header { get; set; }
        public List<T> Data { get; set; } = new();
        public Dictionary<string, object>? AdditionalParameters { get; set; } = new();
        public string ReportTitle { get; set; } = string.Empty;
        public string GeneratedOn { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
