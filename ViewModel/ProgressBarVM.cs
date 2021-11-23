using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace TSApp.ViewModel
{
    internal class ProgressBarVM : ObservableObject
    {
        private int maxvalue;
        private int minvalue;
        private int value;  
        public int MaxValue { get => maxvalue; set => SetProperty(ref maxvalue, value); }
        public int MinValue { get => minvalue; set => SetProperty(ref minvalue, value); }
        public int Value { get => value; set => SetProperty(ref value, value); }   

    }
}
