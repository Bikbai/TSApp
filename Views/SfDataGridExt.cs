using Syncfusion.UI.Xaml.Grid;
using System.Linq;
using System.Windows.Input;
using TSApp.Behaviors;

namespace TSApp { 
    public class SfDataGridExt : SfDataGrid
    {
        public SfDataGridExt()
        : base()
        {
        }

        protected override void OnTextInput(TextCompositionEventArgs e)
        {

            if (!SelectionController.CurrentCellManager.HasCurrentCell)
            {
                base.OnTextInput(e);
                return;
            }

            //Get the Current Row and Column index from the CurrentCellManager
            var rowColumnIndex = SelectionController.CurrentCellManager.CurrentRowColumnIndex;
            RowGenerator rowGenerator = this.RowGenerator;

            //Get the row from the Row index
            var dataRow = rowGenerator.Items.FirstOrDefault(item => item.RowIndex == rowColumnIndex.RowIndex);

            //Check whether the dataRow is null or not and the type as DataRow

            if (dataRow != null && dataRow is DataRow)
            {

                //Get the column from the VisibleColumn collection based on the column index
                var dataColumn = dataRow.VisibleColumns.FirstOrDefault(column => column.ColumnIndex == rowColumnIndex.ColumnIndex);

                //Convert the input text to char type 
                char text;
                char.TryParse(e.Text, out text);

                //Skip if the column is GridTemplateColumn and the column is not already in editing 

                //Allow Editing only pressed letters digits and Minus sign key 

                if (dataColumn != null && !(dataColumn.GridColumn is GridTemplateColumn) && !dataColumn.IsEditing && SelectionController.CurrentCellManager.BeginEdit() && 
                    (e.Text.Equals("-") || e.Text.Equals("+") || char.IsLetterOrDigit(text)))
                    dataColumn.Renderer.PreviewTextInput(e);
            }
            base.OnTextInput(e);
        }
    }

}