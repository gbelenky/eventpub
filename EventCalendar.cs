using System;
using System.Collections.Generic;

namespace gbelenky.EventPub
{
    public class EventCalendar
    {
        public string Name { get; set; }
        public List<DateTime> EventList { get; set; }

        public int NextDay { get; set; }
        public int NextHour { get; set; }
        public int NextMinute { get; set; }
        public void SetNextSeries()
        {
            foreach(DateTime curr in EventList)
            {
                curr.AddDays(NextDay);
                curr.AddDays(NextHour);
                curr.AddDays(NextHour);
            }
        }        
    }
}