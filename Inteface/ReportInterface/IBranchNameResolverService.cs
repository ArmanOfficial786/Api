namespace NexgenCosysReport.Inteface.ReportInterface
{
    public interface IBranchNameResolverService
    {
        Task<string> GetBranchNamesAsync(string? officeIdsCsv, string allLabel = "All");
    }
}
