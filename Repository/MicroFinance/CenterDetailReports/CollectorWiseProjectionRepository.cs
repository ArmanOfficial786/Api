//// Services/Common/NepaliCalendarService.cs
//using Microsoft.EntityFrameworkCore;
//using NexgenCosysReport.DbContext;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;

//namespace NexgenCosysReport.Services.Common
//{
//    public class NepaliCalendarService : INepaliCalendarService
//    {
//        private readonly AppDbContext _context;

//        public NepaliCalendarService(AppDbContext context)
//        {
//            _context = context;
//        }

//        public async Task<NepaliMonthRange> GetByNepaliYearAndMonthCodeAsync(int nepaliYear, int nepaliMonth)
//        {
//            var monthCode = nepaliMonth.ToString("D2");

//            var row = await _context.SycNepaliCalendar
//                .Where(x => x.NepaliYear == nepaliYear && x.NepaliMonthCode == monthCode)
//                .Select(x => new NepaliMonthRange
//                {
//                    EnglishStartDate = x.EnglishStartDate,
//                    EnglishEndDate = x.EnglishEndDate
//                })
//                .FirstOrDefaultAsync();

//            if (row == null)
//                throw new InvalidOperationException(
//                    $"No Nepali calendar entry for {nepaliYear}-{monthCode}");

//            return row;
//        }
//    }
//}