using System;
using System.Collections.Generic;

namespace gbelenky.EventPub
{
    public class EventCalendar
    {
        public string Name { get; set; }
        public List<DateTime> EventList { get; set; }

        public int NextDay { get; set; } = 0;
        public int NextHour { get; set; } = 0;
        public int NextMinute { get; set; } = 0;
        public void SetNextSeries()
        {
            List<DateTime> nextSeriesEventList = new List<DateTime>();
            foreach(DateTime curr in EventList)
            {
                // these methods do not change the DateTime value
                DateTime nextSeriesEvent = curr.AddDays(0);
                nextSeriesEvent = curr.AddHours(0);
                nextSeriesEvent = curr.AddMinutes(NextMinute);
                nextSeriesEventList.Add(nextSeriesEvent);
            }
            EventList = nextSeriesEventList;
        }        
    }
}