using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace TcpUdpTool.ViewModel.Extension
{
    public static class WindowExtension
    {

        private static Dictionary<Window, EventHandler> _closeEventHandlers = 
            new Dictionary<Window, EventHandler>();


        public static readonly DependencyProperty CloseCommandProperty =
            DependencyProperty.RegisterAttached("CloseCommand", typeof(ICommand), typeof(WindowExtension),
            new FrameworkPropertyMetadata(null, CloseCommandChanged));

        public static ICommand GetCloseCommand(DependencyObject d)
        {
            return (ICommand)d.GetValue(CloseCommandProperty);
        }

        public static void SetCloseCommand(DependencyObject d, ICommand cmd)
        {
            d.SetValue(CloseCommandProperty, cmd);
        }


        private static void CloseCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var window = d as Window;
            if(window == null)
            {
                return;
            }

            window.Unloaded += OnWindowUnloaded;

            var newCmd = args.NewValue as ICommand;
            var oldCmd = args.OldValue as ICommand;

            if(oldCmd != null)
            {
                UnsubscribeCloseEvent(window);
            }

            if (newCmd != null)
            {
                SubscribeCloseEvent(window, newCmd);
            }
        }

        private static void OnWindowUnloaded(object sender, RoutedEventArgs args)
        {
            // prevent window from leaking.
            var window = sender as Window;
            if (window != null)
            {
                UnsubscribeCloseEvent(window);
            }
        }


        private static void SubscribeCloseEvent(Window window, ICommand cmd)
        {
            var handler = new EventHandler((s, e) => cmd.Execute(null));
            _closeEventHandlers.Add(window, handler);
            window.Closed += handler;
        }

        private static void UnsubscribeCloseEvent(Window window)
        {
            EventHandler handler;
            if (_closeEventHandlers.TryGetValue(window, out handler))
            {
                window.Closed -= handler;
                _closeEventHandlers.Remove(window);
            }
        }

    }

}
