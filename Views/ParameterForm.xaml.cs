using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TSApp.ViewModel;

namespace TSApp
{
    /// <summary>
    /// Interaction logic for ParameterForm.xaml
    /// </summary>
    public partial class ParameterForm : Window
    {
        private ParameterModel model;
        public ParameterForm()
        {
            InitializeComponent();
            model = new ParameterModel();
            this.DataContext = model;
        }

        private void button_PreviewKeyUp(object sender, KeyEventArgs e)
        {

        }
    }
}
