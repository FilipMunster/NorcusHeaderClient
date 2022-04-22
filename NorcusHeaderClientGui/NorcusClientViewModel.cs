using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows;

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
        public ICommand FontCommand
        {
            get
            {
                if (fontCommand != null) { return fontCommand; }

                fontCommand = new RelayCommand<string>(
                    (e) => { FontSize = Convert.ToInt32(e); NotifyPropertyChanged(nameof(FontSize)); },
                    (e) => true);
                return fontCommand;
            }
        }

        private ICommand restartCommand;
        public ICommand RestartCommand
        {
            get
            {
                if (restartCommand != null) { return restartCommand; }

                restartCommand = new RelayCommand<string>(
                    (e) => 
                    {
                        System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                        Application.Current.Shutdown(); 
                    },
                    (e) => true);
                return restartCommand;
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
            set => Properties.Settings.Default.hostIp = value;
        }
        public int Port
        {
            get => Properties.Settings.Default.port;
            set => Properties.Settings.Default.port = value;
        }

        public int FontSize
        {
            get => Properties.Settings.Default.fontSize;
            set => Properties.Settings.Default.fontSize = value;
        }
        public bool OrientationIsChecked
        {
            get => Properties.Settings.Default.vertical;
            set
            {
                Properties.Settings.Default.vertical = value;
                Client.SongSeparator = value ? "\n" : ", ";
                NotifyPropertyChanged(nameof(Client));
            }
        }

        public bool AlwaysOnTop
        {
            get => Properties.Settings.Default.alwaysOnTop;
            set
            {
                Properties.Settings.Default.alwaysOnTop = value;
                NotifyPropertyChanged();
            }
        }

        public NorcusClientViewModel()
        {
            Client = new NorcusClient(HostIp, Port, Properties.Settings.Default.id);
            Client.SongSeparator = Properties.Settings.Default.vertical ? "\n" : ", ";
            Client.RunClient();
        }
    }
}
