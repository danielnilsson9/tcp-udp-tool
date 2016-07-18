using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpUdpTool.ViewModel.Reusable;

namespace TcpUdpTool.ViewModel
{
    public class TcpServerViewModel : ObservableObject
    {

        private string _test;
        public string Test
        {
            get { return _test; }
            set { _test = value; }
        }


        public TcpServerViewModel()
        {
            Test = "hej hej";
        }


    }
}
