using System;
using System.Globalization;

namespace TSApp
{
    public static class Helpers
    {
        public static double TimespanSum(TimeSpan[] timeSpans)
        {
            double retval = 0;
            foreach (var t in timeSpans)
            {
                retval += t.TotalHours;
            }
            return retval;
        }
        public static double GetDouble(string value, double defaultValue, out bool success)
        {
            double result;

            //Try parsing in the current culture
            success = double.TryParse(value, System.Globalization.NumberStyles.Any, CultureInfo.CurrentCulture, out result);
            if (!success)
            {
                //Then try in US english
                success = double.TryParse(value, System.Globalization.NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out result);
                //Then in neutral language
                if (!success)
                {
                    success = double.TryParse(value, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out result);
                    if (!success)
                        result = defaultValue;
                }
            }
            return result;
        }

        public static int CalcRank(string state)
        {
            if (state == null) return 4;
            switch (state)
            {
                case "Active": return 1;
                case "Resolved": return 2;
                case "Proposed": return 3;
                case "Closed": return 3;
                default: return 4;
            }
        }
    
        public static int GetWeekNumber(DateTime dt)
        {
            CultureInfo myCI = new CultureInfo("ru-RU");
            Calendar myCal = myCI.Calendar;
            CalendarWeekRule myCWR = myCI.DateTimeFormat.CalendarWeekRule;
            DayOfWeek myFirstDOW = myCI.DateTimeFormat.FirstDayOfWeek;
            return myCal.GetWeekOfYear(dt, myCWR, myFirstDOW);            
        }

        public static int CurrentWeekNumber()
        {
            return GetWeekNumber(DateTime.Now);
        }

        public static DateTime WeekBoundaries(int weekNum, bool start)
        {
            // copy-paste
            // https://stackoverflow.com/questions/662379/calculate-date-from-week-number
            //
            DateTime jan1 = new DateTime(DateTime.Now.Year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;

            DateTime firstThursday = jan1.AddDays(daysOffset);
            int firstWeek = GetWeekNumber(firstThursday);

            // As we're adding days to a date in Week 1,
            // we need to subtract 1 in order to get the right date for week #1
            if (firstWeek == 1)
            {
                weekNum -= 1;
            }

            var result = firstThursday.AddDays(weekNum * 7);

            // Subtract 3 days from Thursday to get Monday, which is the first weekday in ISO8601
            return start? result.AddDays(-3): result.AddDays(4);
        }
    
    
    }
}
