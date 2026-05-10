using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Services.CommonService
{
    public class DateConverterService : IDateConverterService
    {
        private readonly AppDbContext _context;

        public DateConverterService(AppDbContext context)
        {
            _context = context;
        }

        // ----------------------------------------------------
        // BS ? AD  (mirrors CComCalender.NepaliToEnglish)
        // Input:  "2081/01/15"  or  "2081-01-15"
        // Output: DateTime (AD)
        // ----------------------------------------------------
        public async Task<DateTime> NepaliToEnglishAsync(string nepaliDate)
        {
            var fallback = new DateTime(1970, 1, 1);

            if (string.IsNullOrEmpty(nepaliDate))
                return fallback;

            // ? support both "2081/01/15" and "2081-01-15"
            nepaliDate = nepaliDate.Replace('-', '/');

            var parts = nepaliDate.Split('/');
            if (parts.Length != 3) return fallback;

            int nepYear = int.Parse(parts[0]);
            int nepMonth = int.Parse(parts[1]);
            int nepDay = int.Parse(parts[2]);

            // ? mirrors: GetByNepaliYearAndMonthCode(nepyear, nepmonth)
            var comCalendar = await _context.ComCalendars
                .Where(e => e.NepaliYear == nepYear && e.MonthCode == nepMonth)
                .SingleOrDefaultAsync();

            if (comCalendar == null) return fallback;

            // ? mirrors: stDate.AddSeconds((nepday - 1) * 24 * 60 * 60)
            DateTime startDate = comCalendar.EnglishStartDate;
            DateTime finalDate = startDate.AddDays(nepDay - 1);

            return finalDate;
        }

        // ----------------------------------------------------
        // AD ? BS  (mirrors CComCalender.EnglishToNepali)
        // Input:  DateTime (AD)
        // Output: "2081/01/15"
        // ----------------------------------------------------
        public async Task<string> EnglishToNepaliAsync(DateTime englishDate)
        {
            // ? mirrors: GetByEnglishDate(englishDate)
            var comCalendar = await _context.ComCalendars
                .Where(e => e.EnglishStartDate <= englishDate.Date
                         && e.EnglishEndDate >= englishDate.Date)
                .SingleOrDefaultAsync();

            if (comCalendar == null) return "0000/00/00";

            // ? mirrors: offset = englishDate.Subtract(comCalendar.EnglishStartDate)
            int totalDays = (englishDate.Date - comCalendar.EnglishStartDate.Date).Days;

            string nYear = comCalendar.NepaliYear.ToString();
            string nMonth = comCalendar.MonthCode < 10
                            ? "0" + comCalendar.MonthCode
                            : comCalendar.MonthCode.ToString();
            string nDay = (totalDays + 1) < 10
                            ? "0" + (totalDays + 1)
                            : (totalDays + 1).ToString();

            return $"{nYear}/{nMonth}/{nDay}";
        }

        // ----------------------------------------------------
        // Convenience: BS string ? AD string
        // Input:  "2081/01/15" or "2081-01-15"
        // Output: "2024-04-28"
        // ----------------------------------------------------
        public async Task<string> BsToAdStringAsync(string nepaliDate)
        {
            var adDate = await NepaliToEnglishAsync(nepaliDate);
            return adDate == new DateTime(1970, 1, 1)
                ? string.Empty
                : adDate.ToString("yyyy-MM-dd");
        }
    }
}
