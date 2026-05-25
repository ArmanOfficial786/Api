using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface.Common
{
    public interface IComCalendar
    {
        YearsResponseDto GetYears();
        DaysResponseDto GetDays(int year, int month);
        ConvertResponseDto ConvertDate(string direction, string date);
    }
}
