using Microsoft.Win32;
using System;
using System.IO;
using System.Windows.Input;
using TcpUdpTool.Model.Parser;
using TcpUdpTool.Model.Util;
using TcpUdpTool.ViewModel.Reusable;

namespace TcpUdpTool.ViewModel
{
    public class SendViewModel : ObservableObject
    {

        #region private members

        private IParser _parser;

        #endregion

        #region public properties

        private string _message;
        public string Message
        {
            get { return _message; }
            set
            {
                if(_message != value)
                {
                    _message = value;
                    OnPropertyChanged(nameof(Message));
                }
            }
        }

        private bool _plainTextSelected;
        public bool PlainTextSelected
        {
            get { return _plainTextSelected; }
            set
            {
                if (_plainTextSelected != value)
                {
                    if (FileSelected)
                        Message = "";

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
                if (_hexSelected != value)
                {
                    if (FileSelected)
                        Message = "";

                    _hexSelected = value;
                    OnPropertyChanged(nameof(HexSelected));
                }
            }
        }

        private bool _fileSelected;
        public bool FileSelected
        {
            get { return _fileSelected; }
            set
            {
                if(_fileSelected != value)
                {
                    _fileSelected = value;
                    OnPropertyChanged(nameof(FileSelected));
                }
            }
        }

        #endregion

        #region public events

        public event Action<byte[]> SendData;

        #endregion

        #region public commands

        public ICommand TypeChangedCommand
        {
            get { return new DelegateCommand(TypeChanged); }
        }

        public ICommand BrowseCommand
        {
            get { return new DelegateCommand(Browse); }
        }

        public ICommand SendCommand
        {
            get { return new DelegateCommand(Send); }
        }

        #endregion

        #region constructors

        public SendViewModel()
        {
            PlainTextSelected = true;
            Message = "";
        }

        #endregion

        #region private functions

        private void TypeChanged()
        {
            if (PlainTextSelected)
            {
                _parser = new PlainTextParser();
            }
            else if(HexSelected)
            {
                _parser = new HexParser();
            }
        }

        private void Browse()
        {
            var dialog = new OpenFileDialog();
                
            if(dialog.ShowDialog().GetValueOrDefault())
            {
                Message = dialog.FileName;
            }
        }

        private void Send()
        {
            if(FileSelected)
            {
                var filePath = Message;

                if(!File.Exists(filePath))
                {
                    DialogUtils.ShowErrorDialog("The file does not exist.");
                    return;
                }

                if(new FileInfo(filePath).Length > 4096)
                {
                    DialogUtils.ShowErrorDialog("The file is to large to send, maximum size is 16 KB.");
                    return;
                }

                try
                {
                    byte[] file = File.ReadAllBytes(filePath);
                    SendData?.Invoke(file);
                }
                catch(Exception ex)
                {
                    DialogUtils.ShowErrorDialog("Error while reading file. " + ex.Message);
                    return;
                }
            }
            else
            {
                byte[] data = new byte[0];
                try
                {
                    data = _parser.Parse(Message, SettingsUtils.GetEncoding());
                    SendData?.Invoke(data);
                }
                catch (FormatException ex)
                {
                    DialogUtils.ShowErrorDialog(ex.Message);
                    return;
                }
            }
        }

        #endregion

    }
}
