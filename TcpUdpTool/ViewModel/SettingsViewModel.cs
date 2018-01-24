using System.Collections.ObjectModel;
using System.Text;
using TcpUdpTool.ViewModel.Base;
using System.Linq;

namespace TcpUdpTool.ViewModel
{
    public class SettingsViewModel : ObservableObject
    {

        private ObservableCollection<EncodingItem> _encodings;
        public ObservableCollection<EncodingItem> Encodings
        {
            get { return _encodings; }
            set
            {
                _encodings = value;
                OnPropertyChanged(nameof(Encodings));
            }
        }

        private EncodingItem _selectedEncoding;
        public EncodingItem SelectedEncoding
        {
            get { return _selectedEncoding; }
            set
            {
                if(_selectedEncoding != value)
                {
                    _selectedEncoding = value;
                    Properties.Settings.Default.Encoding = _selectedEncoding.GetCodePage();
                    OnPropertyChanged(nameof(SelectedEncoding));
                }
            }
        }

        private bool _ipv6Support;
        public bool IPv6SupportEnabled
        {
            get { return _ipv6Support; }
            set
            {
                if(_ipv6Support != value)
                {
                    _ipv6Support = value;
                    Properties.Settings.Default.IPv6Support = _ipv6Support;
                    OnPropertyChanged(nameof(IPv6SupportEnabled));
                    OnPropertyChanged(nameof(IPv6SupportDisabled));
                }
            }
        }
        public bool IPv6SupportDisabled
        {
            get { return !IPv6SupportEnabled; }
        }

        private bool _scrollToEnd;
        public bool ScrollToEndEnabled
        {
            get { return _scrollToEnd; }
            set
            {
                if (_scrollToEnd != value)
                {
                    _scrollToEnd = value;
                    Properties.Settings.Default.ScrollToEnd = _scrollToEnd;
                    OnPropertyChanged(nameof(ScrollToEndEnabled));
                    OnPropertyChanged(nameof(ScrollToEndDisabled));
                }
            }
        }
        public bool ScrollToEndDisabled
        {
            get { return !ScrollToEndEnabled; }
        }

        private int _historyEntries;
        public int HistoryEntries
        {
            get { return _historyEntries; }
            set
            {
                if(_historyEntries != value)
                {
                    _historyEntries = value;
                    if(_historyEntries < 1)
                    {
                        _historyEntries = 1;
                    }
                    else if(_historyEntries > 1000)
                    {
                        _historyEntries = 1000;
                    }

                    Properties.Settings.Default.HistoryEntries = _historyEntries;
                    OnPropertyChanged(nameof(HistoryEntries));
                }
            }
        }

        private bool _historyInfoTimestamp;
        public bool HistoryInfoTimestamp
        {
            get { return _historyInfoTimestamp; }

            set
            {
                if(_historyInfoTimestamp != value)
                {
                    _historyInfoTimestamp = value;
                    Properties.Settings.Default.HistoryInfoTimestamp = _historyInfoTimestamp;
                    OnPropertyChanged(nameof(HistoryInfoTimestamp));
                }
            }
        }

        private bool _historyInfoIpAddress;
        public bool HistoryInfoIpAddress
        {
            get { return _historyInfoIpAddress; }

            set
            {
                if(_historyInfoIpAddress != value)
                {
                    _historyInfoIpAddress = value;
                    Properties.Settings.Default.HistoryInfoIpAdress = _historyInfoIpAddress;
                    OnPropertyChanged(nameof(HistoryInfoIpAddress));
                }
            }
        }
        
        public SettingsViewModel()
        {
            Encodings = new ObservableCollection<EncodingItem>();

            Encodings.Add(new EncodingItem(Encoding.Default, true));

            Encoding.GetEncodings().OrderBy(o => o.Name).ToList()
                .ForEach(o => Encodings.Add(new EncodingItem(o.GetEncoding(), false)));
 
            IPv6SupportEnabled = Properties.Settings.Default.IPv6Support;
            ScrollToEndEnabled = Properties.Settings.Default.ScrollToEnd;
            HistoryEntries = Properties.Settings.Default.HistoryEntries;
            HistoryInfoTimestamp = Properties.Settings.Default.HistoryInfoTimestamp;
            HistoryInfoIpAddress = Properties.Settings.Default.HistoryInfoIpAdress;
            
            int selected = Properties.Settings.Default.Encoding;
            foreach(var e in Encodings)
            {
                if(e.GetCodePage() == selected)
                {
                    SelectedEncoding = e;
                }
            }       
        }

    }


    public class EncodingItem
    {
        private bool _default;
        private Encoding _encoding;

        public string Name
        {
            get { return ToString(); }
        }


        public EncodingItem(Encoding encoding, bool isDefault)
        {
            _encoding = encoding;
            _default = isDefault;
        }

        public int GetCodePage()
        {
            if (_default) return 0;
            return _encoding.CodePage;
        }

        public override string ToString()
        {
            if (_default) return "Default";
            return _encoding.WebName;
        }

    }

}
