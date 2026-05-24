namespace NexgenCosysReport.Inteface.ReportInterface
{
    public interface IRazorRenderService
    {
        Task<string> RenderToStringAsync(string viewPath, object model);
    }
}
