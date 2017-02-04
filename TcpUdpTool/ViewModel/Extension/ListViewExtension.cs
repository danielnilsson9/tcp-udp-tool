using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace TcpUdpTool.ViewModel.Extension
{
    public static class ListViewExtension
    {

        private static readonly Dictionary<ListView, ListViewCapture> AutoScrollHandlers = 
            new Dictionary<ListView, ListViewCapture>();


        public static readonly  DependencyProperty AutoScrollToEndProperty =
            DependencyProperty.RegisterAttached("AutoScrollToEnd", typeof(bool), typeof(ListViewExtension), 
                new FrameworkPropertyMetadata(false, OnAutoScrolToEndChanged));

        public static bool GetAutoScrollToEnd(DependencyObject d)
        {
            return (bool) d.GetValue(AutoScrollToEndProperty);
        }

        public static void SetAutoScrollToEnd(DependencyObject d, bool value)
        {
            d.SetValue(AutoScrollToEndProperty, value);
        }


        public static void OnAutoScrolToEndChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var listView = d as ListView;
            if (listView == null)
            {
                return;
            }

            var oldValue = (bool) e.OldValue;
            var newValue = (bool) e.NewValue;

            if (oldValue == newValue)
            {
                return;
            }

            if (newValue)
            {
                listView.Unloaded += OnListViewUnloaded;
                listView.Loaded += OnListViewLoaded;
                var desc = TypeDescriptor.GetProperties(listView)["ItemsSource"];
                desc.AddValueChanged(listView, OnListViewItemsSourceChanged);
            }
            else
            {
                listView.Unloaded -= OnListViewUnloaded;
                listView.Loaded -= OnListViewLoaded;

                ListViewCapture capture;
                if (AutoScrollHandlers.TryGetValue(listView, out capture))
                {
                    capture.Dispose();
                    AutoScrollHandlers.Remove(listView);
                }

                var desc = TypeDescriptor.GetProperties(listView)["ItemsSource"];
                desc.RemoveValueChanged(listView, OnListViewItemsSourceChanged);
            }
        }

        private static void OnListViewUnloaded(object sender, RoutedEventArgs e)
        {
            var listView = sender as ListView;
            if (listView != null && GetAutoScrollToEnd(listView))
            {
                ListViewCapture capture;
                if (AutoScrollHandlers.TryGetValue(listView, out capture))
                {
                    capture.Dispose();
                    AutoScrollHandlers.Remove(listView);
                }
            }
        }

        private static void OnListViewLoaded(object sender, RoutedEventArgs e)
        {
            var listView = sender as ListView;
            if (listView != null && GetAutoScrollToEnd(listView))
            {
                if (!AutoScrollHandlers.ContainsKey(listView))
                {
                    AutoScrollHandlers.Add(listView, new ListViewCapture(listView));
                }
            }
        }


        private static void OnListViewItemsSourceChanged(object sender, EventArgs e)
        {
            var listView = sender as ListView;
            if (listView != null && GetAutoScrollToEnd(listView))
            {
                // remove if already exist.
                ListViewCapture capture;
                if (AutoScrollHandlers.TryGetValue(listView, out capture))
                {
                    capture.Dispose();
                    AutoScrollHandlers.Remove(listView);
                }

                AutoScrollHandlers.Add(listView, new ListViewCapture(listView));
            }
        }
    }

    class ListViewCapture : IDisposable
    {
        private readonly ListView _listView;
        private readonly INotifyCollectionChanged _notifyCollectionChanged;

        public ListViewCapture(ListView listView)
        {
            _listView = listView;
            _notifyCollectionChanged = _listView.ItemsSource as INotifyCollectionChanged;
            if (_notifyCollectionChanged != null)
            {
                _notifyCollectionChanged.CollectionChanged += OnCollectionChanged;
            }
        }

        void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Reset)
            {
                _listView.ItemContainerGenerator.StatusChanged += ScrollToEnd;
            }
        }

        public void ScrollToEnd(object sender, EventArgs arg)
        {
            if (_listView.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                var item = _listView.Items[_listView.Items.Count - 1];
                if (item == null)
                {
                    return;
                }
                   
                _listView.ScrollIntoView(item);
                _listView.ItemContainerGenerator.StatusChanged -= ScrollToEnd;
            }
        }

        public void Dispose()
        {
            if (_notifyCollectionChanged != null)
                _notifyCollectionChanged.CollectionChanged -= OnCollectionChanged;

            _listView.ItemContainerGenerator.StatusChanged -= ScrollToEnd;
        }
    }

}
