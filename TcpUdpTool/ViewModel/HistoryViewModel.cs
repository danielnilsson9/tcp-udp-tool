using System;
using System.Windows.Input;
using TcpUdpTool.Model;
using TcpUdpTool.Model.Formatter;
using TcpUdpTool.ViewModel.Reusable;

namespace TcpUdpTool.ViewModel
{
    public class HistoryViewModel : ObservableObject
    {

        private TransmissionHistory _transmissionHistory;
        public TransmissionHistory Transmissions
        {
            get { return _transmissionHistory; }
        }


        private string _header;
        public string Header
        {
            get { return _header; }
            set
            {
                _header = value;
                OnPropertyChanged("Header");
            }
        }

        public string Conversation
        {
            get { return _transmissionHistory.Get(); }
            set { }
        }

        private bool _plainTextSelected;
        public bool PlainTextSelected
        {
            get { return _plainTextSelected; }
            set
            {
                if (value != _plainTextSelected)
                {
                    _plainTextSelected = value;
                    OnPropertyChanged("PlainTextSelected");
                }
            }
        }

        private bool _hexSelected;
        public bool HexSelected
        {
            get { return _hexSelected; }
            set
            {
                if (value != _hexSelected)
                {
                    _hexSelected = value;
                    OnPropertyChanged("HexSelected");
                }
            }
        }


        public ICommand ClearCommand
        {
            get { return new DelegateCommand(Clear); }
        }

        public ICommand SaveCommand
        {
            get { return new DelegateCommand(Save); }
        }

        public ICommand ViewChangedCommand
        {
            get { return new DelegateCommand(ViewChanged); }
        }


        public HistoryViewModel()
        {
            _transmissionHistory = new TransmissionHistory();
            _transmissionHistory.SetMaxSize(Properties.Settings.Default.HistoryEntries);
            _transmissionHistory.HistoryChanged +=
               () =>
               {
                    // Trigger change event so view gets updated.
                    OnPropertyChanged("Conversation");
               };

            PlainTextSelected = true;

            Properties.Settings.Default.SettingChanging +=
                (sender, e) =>
                {
                    if(e.SettingName == nameof(Properties.Settings.Default.HistoryEntries))
                    {
                        _transmissionHistory.SetMaxSize((int)e.NewValue);
                    }
                };

        }

        private void Clear()
        {
            Transmissions.Clear();
        }

        private void Save()
        {
            throw new NotImplementedException();
        }

        private void ViewChanged()
        {
            if (PlainTextSelected)
            {
                _transmissionHistory.SetFormatter(new PlainTextFormatter());
            }
            else if (HexSelected)
            {
                _transmissionHistory.SetFormatter(new HexFormatter());
            }
        }

    }
}
