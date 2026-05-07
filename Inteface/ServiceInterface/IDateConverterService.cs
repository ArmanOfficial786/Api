namespace NexgenCosysReport.Inteface.ServiceInterface
{
    public interface IDateConverterService
    {
        Task<DateTime> NepaliToEnglishAsync(string nepaliDate); // "2081/01/15" ? DateTime
        Task<string> EnglishToNepaliAsync(DateTime englishDate); // DateTime ? "2081/01/15"
        Task<string> BsToAdStringAsync(string nepaliDate);    // "2081/01/15" ? "2024-04-28"
    }
}
