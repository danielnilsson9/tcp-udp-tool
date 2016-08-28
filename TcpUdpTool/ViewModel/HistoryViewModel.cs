using System;
using System.Windows.Documents;
using System.Windows.Input;
using TcpUdpTool.Model;
using TcpUdpTool.Model.Formatter;
using TcpUdpTool.ViewModel.Helper;
using TcpUdpTool.ViewModel.Reusable;

namespace TcpUdpTool.ViewModel
{
    public class HistoryViewModel : ObservableObject, IRichTextboxHelper
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
                OnPropertyChanged(nameof(Header));
            }
        }

        private FlowDocument _document = new FlowDocument();
        public FlowDocument Document
        {
            get
            {
                return _document;
            }
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

       

        public HistoryViewModel()
        {
            _transmissionHistory = new TransmissionHistory(Document);
            _transmissionHistory.SetFormatter(
                new PlainTextFormatter(
                    Properties.Settings.Default.HistoryInfoTimestamp, 
                    Properties.Settings.Default.HistoryInfoIpAdress
                )
            );
            _transmissionHistory.SetMaxSize(Properties.Settings.Default.HistoryEntries);
            _transmissionHistory.HistoryChanged +=
               () =>
               {
                    // trigger, but no one is listening atm.
                    OnPropertyChanged(nameof(Document));
               };

            PlainTextSelected = true;

            Properties.Settings.Default.PropertyChanged +=
                (sender, e) =>
                {
                    if(e.PropertyName == nameof(Properties.Settings.Default.HistoryEntries))
                    {
                        _transmissionHistory.SetMaxSize(Properties.Settings.Default.HistoryEntries);
                    }
                    else if(e.PropertyName == nameof(Properties.Settings.Default.HistoryInfoTimestamp) ||
                            e.PropertyName == nameof(Properties.Settings.Default.HistoryInfoIpAdress))
                    {
                        // reset formatter.
                        ViewChanged();
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
                _transmissionHistory.SetFormatter(
                     new PlainTextFormatter(
                        Properties.Settings.Default.HistoryInfoTimestamp,
                        Properties.Settings.Default.HistoryInfoIpAdress
                    )
                );
            }
            else if (HexSelected)
            {
                _transmissionHistory.SetFormatter(
                    new HexFormatter(
                        Properties.Settings.Default.HistoryInfoTimestamp,
                        Properties.Settings.Default.HistoryInfoIpAdress
                    )
                );
            }
        }

    }
}
