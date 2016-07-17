using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpUdpTool.ViewModel.Presenter
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
