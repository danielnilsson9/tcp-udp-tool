using System;
using System.Windows;
using System.Windows.Threading;

namespace TcpUdpTool.ViewModel.Helper
{
    public static class DispatchHelper
    {
        public static void Invoke(Action action)
        {
            var dispatchObject = Application.Current.Dispatcher;
            if (dispatchObject == null || dispatchObject.CheckAccess())
            {
                action();
            }
            else
            {
                dispatchObject.Invoke(action);
            }
        }
    }
}
