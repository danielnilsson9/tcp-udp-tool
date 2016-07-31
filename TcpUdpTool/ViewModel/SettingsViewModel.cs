using System.Collections.ObjectModel;
using System.Text;
using TcpUdpTool.ViewModel.Reusable;

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


        private int _historyEntries;
        public int HistoryEntries
        {
            get { return _historyEntries; }
            set
            {
                if(_historyEntries != value)
                {
                    _historyEntries = value;
                    Properties.Settings.Default.HistoryEntries = _historyEntries;
                    OnPropertyChanged(nameof(HistoryEntries));
                }
            }
        }



        public SettingsViewModel()
        {
            Encodings = new ObservableCollection<EncodingItem>();

            Encodings.Add(new EncodingItem(Encoding.Default, true));
            foreach(EncodingInfo e in Encoding.GetEncodings())
            {
                Encodings.Add(new EncodingItem(e.GetEncoding(), false));
            }

            IPv6SupportEnabled = Properties.Settings.Default.IPv6Support;
            HistoryEntries = Properties.Settings.Default.HistoryEntries;


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
