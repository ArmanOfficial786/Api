using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Repository.Common
{

    public class CalendarRepository : IComCalendar
    {
        private readonly AppDbContext _context;

        public CalendarRepository(AppDbContext context)
        {
            _context = context;
        }

        // ── Helpers (private, raw DB queries) ──────────────────────────────

        private List<int> QueryDistinctYears()
            => _context.ComCalendars
                .Select(c => c.NepaliYear)
                .Distinct()
                .OrderBy(y => y)
                .ToList();

        private Models.ComCalendar? QueryByYearAndMonth(int year, int month)
            => _context.ComCalendars
                .FirstOrDefault(c => c.NepaliYear == year && c.MonthCode == month);

        private Models.ComCalendar? QueryByEnglishDate(DateTime date)
            => _context.ComCalendars
                .FirstOrDefault(c => c.EnglishStartDate <= date.Date
                                  && c.EnglishEndDate >= date.Date);

        // ── Public methods (business logic merged in) ───────────────────────

        public YearsResponseDto GetYears()
        {
            return new YearsResponseDto { Years = QueryDistinctYears() };
        }

        public DaysResponseDto GetDays(int year, int month)
        {
            var record = QueryByYearAndMonth(year, month);

            if (record == null)
                return new DaysResponseDto { Year = year, Month = month, Days = new() };

            return new DaysResponseDto
            {
                Year = year,
                Month = month,
                Days = Enumerable.Range(1, record.DaysNo).ToList()
            };
        }

        public ConvertResponseDto ConvertDate(string direction, string date)
        {
            if (direction == "ADtoBS")
            {
                if (!DateTime.TryParse(date, out var adDate))
                    throw new ArgumentException("Invalid AD date format. Use yyyy-MM-dd.");

                var record = QueryByEnglishDate(adDate);
                if (record == null)
                    throw new Exception("No matching BS record found for the given AD date.");

                int bsDay = (adDate.Date - record.EnglishStartDate.Date).Days + 1;

                return new ConvertResponseDto
                {
                    ConvertedDate = $"{record.NepaliYear}-{record.MonthCode:D2}-{bsDay:D2}",
                    Year = record.NepaliYear,
                    Month = record.MonthCode,
                    Day = bsDay
                };
            }
            else if (direction == "BStoAD")
            {
                var parts = date.Split('-');

                if (parts.Length != 3
                    || !int.TryParse(parts[0], out int year)
                    || !int.TryParse(parts[1], out int month)
                    || !int.TryParse(parts[2], out int day))
                    throw new ArgumentException("Invalid BS date format. Use yyyy-MM-dd.");

                var record = QueryByYearAndMonth(year, month);
                if (record == null)
                    throw new Exception("No matching AD record found for the given BS date.");

                if (day < 1 || day > record.DaysNo)
                    throw new ArgumentException($"Day {day} is out of range for BS {year}-{month:D2} (max {record.DaysNo}).");

                var adDate = record.EnglishStartDate.AddDays(day - 1);

                return new ConvertResponseDto
                {
                    ConvertedDate = adDate.ToString("yyyy-MM-dd"),
                    Year = adDate.Year,
                    Month = adDate.Month,
                    Day = adDate.Day
                };
            }
            else
            {
                throw new ArgumentException("Direction must be 'ADtoBS' or 'BStoAD'.");
            }
        }
    }
}
