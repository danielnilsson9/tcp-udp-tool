using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace TcpUdpTool.ViewModel.Base
{
    public class BatchObservableCollection<T> : Collection<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private int _batchOperationCount;
        private bool _changedDuringBatchOperation;

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;


        public void BeginBatch()
        {
            _batchOperationCount++;
        }

        public void EndBatch()
        {
            if (_batchOperationCount == 0)
            {
                throw new InvalidOperationException("EndBatch() called without a matching call to BeginBatch().");
            }

            _batchOperationCount--;

            if (_batchOperationCount == 0 && _changedDuringBatchOperation)
            {
                OnCollectionReset();
                _changedDuringBatchOperation = false;
            }
        }

        public void AddRange(IEnumerable<T> items)
        {
            if (items == null)
            {
                return;
            }

            BeginBatch();
            try
            {
                var list = items as IList<T>;
                if (list != null)
                {
                    for (var i = 0; i < list.Count; i++)
                    {
                        Add(list[i]);
                    }
                }
                else
                {
                    {
                        foreach (var item in items)
                        {
                            Add(item);
                        }
                    }
                }
            }
            finally
            {
                EndBatch();
            }
        }

        protected override void ClearItems()
        {
            var hadItems = Count != 0;

            base.ClearItems();

            if (hadItems)
            {
                if (_batchOperationCount == 0)
                {
                    OnCollectionReset();
                }
                else
                {
                    _changedDuringBatchOperation = true;
                }
            }
        }

        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);

            if (_batchOperationCount == 0)
            {
                OnCountChanged();
                OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
            }
            else
            {
                _changedDuringBatchOperation = true;
            }
        }

        protected override void RemoveItem(int index)
        {
            var item = this[index];
            base.RemoveItem(index);

            if (_batchOperationCount == 0)
            {
                OnCountChanged();
                OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, index);
            }
            else
            {
                _changedDuringBatchOperation = true;
            }
        }

        protected override void SetItem(int index, T item)
        {
            var oldItem = this[index];
            base.SetItem(index, item);

            if (_batchOperationCount == 0)
            {
                OnItemsChanged();
                OnCollectionChanged(NotifyCollectionChangedAction.Replace, item, oldItem, index);
            }
            else
            {
                _changedDuringBatchOperation = true;
            }
        }

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, T item, int index)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, T item, T oldItem, int index)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, oldItem, index));
        }

        private void OnCollectionReset()
        {
            OnCountChanged();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private void OnCountChanged()
        {
            OnPropertyChanged("Count");
            OnItemsChanged();
        }

        private void OnItemsChanged()
        {
            OnPropertyChanged("Items[]");
        }

    }
}
