using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;

namespace NorcusHeaderClientGui
{
    class NorcusClientViewModel : INotifyPropertyChanged
    {
        private const int port = 21573;
        private const string hostIp = "192.168.1.124";
        private Client client;

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private void NotifySenderPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(sender.GetType().Name));
        }

        private ICommand fontCommand;
        public ICommand FontCommand {
            get
            {
                if (fontCommand != null) { return fontCommand; }
                
                fontCommand = new RelayCommand<string>(
                    (e) => { FontSize = Convert.ToInt32(e); NotifyPropertyChanged(nameof(FontSize)); },
                    (e) => true);
                return fontCommand;
            }
        }
        private ICommand orientationCommand;
        public ICommand OrientationCommand
        {
            get
            {
                if (orientationCommand != null) { return orientationCommand; }

                orientationCommand = new RelayCommand<object>(
                    (e) => SwitchSongSeparator(),
                    (e) => true);
                return orientationCommand;
            }
        }

        public Client Client
        {
            get => client;
            set
            {
                client = value;
                client.PropertyChanged += new PropertyChangedEventHandler(NotifySenderPropertyChanged);
            }
        }
        public int FontSize { get; set; }
        public NorcusClientViewModel()
        {
            Client = new Client(hostIp, port);
            Client.MainLoop();
        }

        private void SwitchSongSeparator()
        {
            string oldSeparator = Client.SongSeparator;
            Client.SongSeparator = oldSeparator == " " ? "\n" : " ";
            Client.MessageLabel = Client.MessageLabel.Replace("," + oldSeparator, "," + Client.SongSeparator);
            NotifyPropertyChanged(nameof(Client));
        }
    }
}
