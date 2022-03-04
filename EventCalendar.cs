using System;
using System.Collections.Generic;

namespace gbelenky.EventPub
{
    public class EventCalendar
    {
        public string Name { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public int NextDay { get; set; } = 0;
        public int NextHour { get; set; } = 0;
        public int NextMinute { get; set; } = 0;
        public int NextSecond { get; set; } = 0;
        public void SetNextStartTime()
        {
                DateTimeOffset nextStartTime = StartTime.AddDays(NextDay);
                nextStartTime = StartTime.AddHours(NextHour);
                nextStartTime = StartTime.AddMinutes(NextMinute);
                nextStartTime = StartTime.AddSeconds(NextSecond);
                StartTime = nextStartTime;
        }    
    }
}