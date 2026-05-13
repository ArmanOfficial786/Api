using System.Text.Json.Serialization;

namespace NexgenCosysReport.Dtos.RequestDtos.Common
{
    public class GeneralResponse<T>
    {
        public bool isValid { get; set; }
        public Int32 statusCode { get; set; }
        public string message { get; set; } = "";
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public T? data { get; set; }
        //public Pagination? Pagination { get; set; }
    }

    public class Pagination
    {
        public int? currentPage { get; set; }     // ? PascalCase
        public int? totalPages { get; set; }
        public int? pageSize { get; set; }
        public int? totalRecord { get; set; }
        public bool? hasNextPage { get; set; }    // ? PascalCase
        public bool? hasPreviousPage { get; set; }
    }

    public class ReportResponseDtos
    {
        public string? pdfData { get; set; }
        public string? reportName { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Pagination? pagination { get; set; }

    }



}


//public record ApiResponse<T>
//{
//    public bool Success { get; set; }
//    public T Data { get; set; }
//} both are same