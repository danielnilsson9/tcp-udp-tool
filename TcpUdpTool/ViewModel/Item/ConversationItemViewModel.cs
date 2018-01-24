using TcpUdpTool.Model.Data;
using TcpUdpTool.Model.Formatter;
using TcpUdpTool.ViewModel.Base;

namespace TcpUdpTool.ViewModel.Item
{
    public class ConversationItemViewModel : ObservableObject
    {
        private Transmission _message;
        private IFormatter _formatter;
        private string _contentCache;

        public string Timestamp
        {
            get { return "[" + _message.Timestamp.ToLongTimeString() + "]"; }
        }

        public bool TimestampVisible
        {
            get { return Properties.Settings.Default.HistoryInfoTimestamp; }
        }

        public string Source
        {
            get { return "[" + (IsReceived ? _message.Origin?.ToString() : _message.Destination?.ToString()) + "]"; }
        }

        public bool SourceVisible
        {
            get { return Properties.Settings.Default.HistoryInfoIpAdress; }
        }

        public bool IsReceived
        {
            get { return _message.IsReceived; }
        }

        public string Content
        {
            get { return _contentCache; }
        }

        public bool IsSelected { get; set; }

        public ConversationItemViewModel(Transmission message, IFormatter formatter)
        {
            _message = message;
            _formatter = formatter;
            _contentCache = _formatter.Format(message);
        }

        public void SetFormatter(IFormatter formatter)
        {
            _formatter = formatter;
            _contentCache = _formatter.Format(_message);
            OnPropertyChanged(nameof(Content));
            OnPropertyChanged(nameof(TimestampVisible));
            OnPropertyChanged(nameof(SourceVisible));
        }

    }
}
