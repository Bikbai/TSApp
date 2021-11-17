using System.Globalization;

namespace TSApp
{
    public static class ParserUtility
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
    }
}
