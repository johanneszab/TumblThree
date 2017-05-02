using System;
using System.Collections.Generic;
using System.Waf.Applications;
using System.Windows;

namespace TumblThree.Applications.Views
{
    public interface IManagerView : IView
    {
        Dictionary<object, Tuple<int, double, Visibility>> DataGridColumnRestore { get; set; }
    }
}
