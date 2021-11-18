using System;
using System.Globalization;

namespace TSApp
{
    public static class Helpers
    {
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
    
    
    }
}
