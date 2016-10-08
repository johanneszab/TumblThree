using System;
using System.Collections.Generic;
using System.Waf.Applications;

namespace TumblThree.Applications.Views
{
    public interface IManagerView : IView
    {
        Dictionary<object, double> DataGridColumnRestore { get; set; }
    }
}
