using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class ComCalendar
{
    public int ComCalendarId { get; set; }

    public int NepaliYear { get; set; }

    public int MonthCode { get; set; }

    public DateTime EnglishStartDate { get; set; }

    public DateTime EnglishEndDate { get; set; }

    public int DaysNo { get; set; }
}
