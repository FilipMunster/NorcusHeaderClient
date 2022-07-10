using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;

namespace NorcusSetClient
{
    class NorcusClientViewModel : INotifyPropertyChanged
    {
        private readonly string databaseFile = System.IO.Path.GetDirectoryName(
            Application.ResourceAssembly.Location) + "\\NorcusDatabase.xml";

        private SetListManager _setListManager;

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

        public NorcusClient Client { get; private set; }
        public Database Database { get; set; }

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

        public bool AlwaysOnTop
        {
            get => Properties.Settings.Default.alwaysOnTop;
            set
            {
                Properties.Settings.Default.alwaysOnTop = value;
                NotifyPropertyChanged();
            }
        }

        public string[] SetList
        {
            get
            {
                if (Client.SetList is null)
                    return new string[] { Client.Message };
                return Client.SetList;
            }
        }
        public int SongIndex => Client.CurrentSongIndex;

        public string PauseDuration => new DateTime(_setListManager.PauseDuration.Ticks).ToString("mm:ss");
        public string SetListDuration
        {
            get
            {
                string dur = new DateTime(_setListManager.SetListDuration.Ticks).ToString("mm");
                if (dur[0] == '0')
                    dur = dur.Remove(0, 1);
                return dur + "min";
            }
        }
        public string SetEndTime => _setListManager.SetEndTime.ToString("HH:mm");

        public NorcusClientViewModel()
        {
            Database = new Database(databaseFile);
            Database.Load();

            _setListManager = new SetListManager(Database);

            Client = new NorcusClient(HostIp, Port, Properties.Settings.Default.id);
            Client.SelectionChanged += Client_SelectionChanged;
            Client.SelectionChanged += _setListManager.SelectionChanged;
            Client.RunClient();

            if (Properties.Settings.Default.logging)
                Client.Logger = new Logger();

            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;

            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Start();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            NotifyPropertyChanged(nameof(PauseDuration));
            NotifyPropertyChanged(nameof(SetListDuration));
            NotifyPropertyChanged(nameof(SetEndTime));
        }

        private void Client_SelectionChanged(object sender, NorcusClient.SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged(nameof(SetList));
            NotifyPropertyChanged(nameof(SongIndex));
        }

        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                Client.Logger.Log("System resumed from suspended state. Restarting application...");
                // Restart aplikace:            
                System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                Application.Current.Shutdown();
            }
        }

    }
}
