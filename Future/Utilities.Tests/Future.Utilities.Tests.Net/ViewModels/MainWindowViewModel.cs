using DevExpress.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Future.Utilities.Tests.Net
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            //Employees = Staff.GetStaff();
        }
        public List<Employee> Employees { get; private set; }
    }
}
