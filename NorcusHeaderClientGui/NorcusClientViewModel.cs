using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows.Input;

namespace NorcusSetClient
{
    class NorcusClientViewModel : INotifyPropertyChanged
    {
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
        private NorcusClient client;
        public NorcusClient Client
        {
            get => client;
            set
            {
                client = value;
                client.PropertyChanged += new PropertyChangedEventHandler(NotifySenderPropertyChanged);
            }
        }

        public string HostIp
        {
            get => Properties.Settings.Default.hostIp;
            set
            {
                Properties.Settings.Default.hostIp = value;
            }
        }
        public int Port
        {
            get => Properties.Settings.Default.port;
            set
            {
                Properties.Settings.Default.port = value;
            }
        }

        public int FontSize
        {
            get => Properties.Settings.Default.fontSize;
            set
            {
                Properties.Settings.Default.fontSize = value;
            }
        }
        public bool OrientationIsChecked
        {
            get => Properties.Settings.Default.vertical;
            set
            {
                Properties.Settings.Default.vertical = value;
                NotifyPropertyChanged(nameof(Client));
            }
        }

        public NorcusClientViewModel()
        {
            Client = new NorcusClient(HostIp, Port);
            Client.RunClient();
        }
    }
}
