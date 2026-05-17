// Utils/Report/CustomHeaderResponse.cs
namespace NexgenCosysReport.Utils.Report
{
    public class CustomHeaderResponse
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CustomHeaderResponse(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void SetResponseHeaders(bool isValid, int statusCode, string message)
        {
            var response = _httpContextAccessor.HttpContext?.Response;
            if (response != null)
            {
                response.Headers.Append("X-IsValid", isValid.ToString().ToLower());
                response.Headers.Append("X-StatusCode", statusCode.ToString());
                response.Headers.Append("X-Message", Uri.EscapeDataString(message));
            }
        }

        // Helper methods for common scenarios
        public void SetSuccess(string message = "Success") => SetResponseHeaders(true, 200, message);
        public void SetBadRequest(string message = "Invalid request") => SetResponseHeaders(false, 400, message);
        public void SetNotFound(string message = "Resource not found") => SetResponseHeaders(false, 404, message);
        public void SetServerError(string message = "Internal server error") => SetResponseHeaders(false, 500, message);
    }
}