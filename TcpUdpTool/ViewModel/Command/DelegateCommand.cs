using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TcpUdpTool.ViewModel.Command
{
    public class DelegateCommand : ICommand
    {      
        private readonly Action _action;
        private readonly Action<object> _actionWithParam;

        public event EventHandler CanExecuteChanged;


        public DelegateCommand(Action action)
        {
            _action = action;
            _actionWithParam = null;
        }

        public DelegateCommand(Action<object> action)
        {
            _actionWithParam = action;
            _action = null;
        }

        

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _action?.Invoke();
            _actionWithParam?.Invoke(parameter);
        }
    }
}
