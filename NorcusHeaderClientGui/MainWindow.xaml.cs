using System.Windows;

namespace NorcusSetClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var props = Properties.Settings.Default;
            NorcusWindow.Height = props.windowHeight;
            NorcusWindow.Width = props.windowWidth;
            NorcusWindow.Left = props.windowLeft;
            NorcusWindow.Top = props.windowTop;
        }

        private void NorcusWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            NorcusClientViewModel vm = (NorcusClientViewModel)DataContext;
            vm.Client.SocketClose();
            vm.Database.Save();

            vm.Client.Logger?.Save();

            var props = Properties.Settings.Default;
            props.windowHeight = NorcusWindow.Height;
            props.windowWidth = NorcusWindow.Width;
            props.windowLeft = NorcusWindow.Left;
            props.windowTop = NorcusWindow.Top;

            Properties.Settings.Default.Save();
        }

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.Top = NorcusWindow.Top + 10;
            settingsWindow.Left = NorcusWindow.Left + 10;
            settingsWindow.Show();            
        }

        private void SetListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var listBox = (System.Windows.Controls.ListBox)sender;
            listBox.ScrollIntoView(listBox.SelectedItem);
        }
    }
}
