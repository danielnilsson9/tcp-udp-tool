using System;
using System.Windows.Input;
using TcpUdpTool.Model;
using TcpUdpTool.Model.Formatter;
using TcpUdpTool.ViewModel.Reusable;

namespace TcpUdpTool.ViewModel
{
    public class HistoryViewModel : ObservableObject
    {

        private PlainTextFormatter _plainTextFormatter;
        private HexFormatter _hexFormatter;

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
                OnPropertyChanged(nameof(Header));
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
                    OnPropertyChanged(nameof(PlainTextSelected));
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
                    OnPropertyChanged(nameof(HexSelected));
                }
            }
        }


        public ICommand ClearCommand
        {
            get { return new DelegateCommand(Clear); }
        }

        public ICommand ViewChangedCommand
        {
            get { return new DelegateCommand(ViewChanged); }
        }


        public HistoryViewModel(PlainTextFormatter plainTextFormatter, HexFormatter hexFormatter)
        {
            _plainTextFormatter = plainTextFormatter;
            _hexFormatter = hexFormatter;
            _transmissionHistory = new TransmissionHistory();
            _transmissionHistory.SetFormatter(plainTextFormatter);
            _transmissionHistory.SetMaxSize(Properties.Settings.Default.HistoryEntries);
            _transmissionHistory.HistoryChanged +=
               () =>
               {
                    // Trigger change event so view gets updated.
                    OnPropertyChanged(nameof(Conversation));
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

        private void ViewChanged()
        {
            if (PlainTextSelected)
            {
                _transmissionHistory.SetFormatter(_plainTextFormatter);
            }
            else if (HexSelected)
            {
                _transmissionHistory.SetFormatter(_hexFormatter);
            }
        }

    }
}
