using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UdpTcpTool.Model;

namespace UdpTcpTool.ViewModel
{
    class TcpServerPresenter : ObservableObject
    {

        private string _test;
        public string Test
        {
            get { return _test; }
            set { _test = value; }
        }


        public TcpServerPresenter()
        {
            Test = "hej hej";
        }





    }
}
