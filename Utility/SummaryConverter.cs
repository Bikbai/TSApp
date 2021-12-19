using Syncfusion.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace TSApp.Utility
{
    public class SummaryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            var data = value != null ? value as SummaryRecordEntry : null;
            if (data != null)
            {
                SfDataGridExt dataGrid = (SfDataGridExt)parameter;
                var unitPrice = SummaryCreator.GetSummaryDisplayValue(data, "WorkDbl", "Sum");                
                return "Суммарные трудозатраты: " + unitPrice.ToString();
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
