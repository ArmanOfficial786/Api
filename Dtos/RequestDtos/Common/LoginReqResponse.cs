using System.ComponentModel.DataAnnotations;

namespace NexgenCosysReport.Dtos.RequestDtos.Common
{

    public class LoginRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public int CompanyId { get; set; }
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public long UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public long UserTypeId { get; set; }
        public string UserTypeName { get; set; } = string.Empty;
        public long GenderId { get; set; }
        public long OfficeId { get; set; }
        public string OfficeIds { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string SystemEditionName { get; set; } = string.Empty;
    }
    public class LoginReqResponse
    {
    }
}
