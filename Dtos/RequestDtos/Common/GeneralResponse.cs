using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace NexgenCosysReport.Dtos.RequestDtos.Common
{
    public class GeneralResponse<T>
    {
        public bool IsValid { get; set; }
        public Int32 StatusCode { get; set; }
        public string Message { get; set; } = "";
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public T? Data { get; set; }
      
        //public Pagination? Pagination { get; set; }
    }

    public class Pagination
    {
        public int? CurrentPage { get; set; }     // ? PascalCase
        public int? TotalPages { get; set; }
        public int? PageSize { get; set; }
        public int? TotalRecord { get; set; }
        public bool? HasNextPage { get; set; }    // ? PascalCase
        public bool? HasPreviousPage { get; set; }
    }

    public class ReportResponseDtos
    {
        public string? PdfData { get; set; }
        public string? ReportName { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Pagination? Pagination { get; set; }

    }



}


//public record ApiResponse<T>
//{
//    public bool Success { get; set; }
//    public T Data { get; set; }
//} both are same