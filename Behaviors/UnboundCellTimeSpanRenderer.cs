using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Cells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace TSApp.Behaviors
{
    public class UnboundCellTimeSpanRenderer : GridUnBoundRowCellTextBoxRenderer
    {
        public override void OnInitializeDisplayElement(DataColumnBase dataColumn, TextBlock uiElement, object dataContext)
        {
            base.OnInitializeDisplayElement(dataColumn, uiElement, dataContext);
            var cellValue = dataColumn.GridUnBoundRowEventsArgs != null && dataColumn.GridUnBoundRowEventsArgs.Value != null ?
                dataColumn.GridUnBoundRowEventsArgs.Value.ToString() :
                string.Empty;
            uiElement.Text = $"*{cellValue}";
            uiElement.Foreground = new SolidColorBrush(Colors.Orange);
        }

        public override void OnInitializeEditElement(DataColumnBase dataColumn, TextBox uiElement, object dataContext)
        {
            base.OnInitializeEditElement(dataColumn, uiElement, dataContext);

            var cellValue = (dataColumn.GridUnBoundRowEventsArgs != null && dataColumn.GridUnBoundRowEventsArgs.Value != null) ?
                                dataColumn.GridUnBoundRowEventsArgs.Value.ToString() :
                                string.Empty;

            uiElement.Text = cellValue.ToString();
        }
    }
}
