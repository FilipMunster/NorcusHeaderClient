using System.Net;
using System.Windows;

namespace NorcusSetClient
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private const int portDefault = 21573;
        private const string hostIpDefault = "192.168.1.128";
        private const string idDefault = "UNKNOWN";

        public SettingsWindow()
        {
            InitializeComponent();
            ipTextBox.Text = Properties.Settings.Default.hostIp;
            portTextBox.Text = Properties.Settings.Default.port.ToString();
            idTextBox.Text = Properties.Settings.Default.id;
            logCheckBox.IsChecked = Properties.Settings.Default.logging;
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IPAddress.TryParse(ipTextBox.Text, out _))
            {
                MessageBox.Show("Zadaná IP adresa je neplatná!");
                return;
            }
            Properties.Settings.Default.hostIp = ipTextBox.Text;

            if (!int.TryParse(portTextBox.Text, out int portInt))
            {
                MessageBox.Show("Port musí být celé číslo!");
                return;
            }

            if (idTextBox.Text.Length > 1024)
            {
                MessageBox.Show("ID je příliš dlouhé!");
                return;
            }

            Properties.Settings.Default.port = portInt;
            Properties.Settings.Default.id = idTextBox.Text;
            Properties.Settings.Default.logging = (bool)logCheckBox.IsChecked;
            Properties.Settings.Default.Save();

            // Restart aplikace:            
            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        private void defaultButton_Click(object sender, RoutedEventArgs e)
        {
            ipTextBox.Text = hostIpDefault;
            portTextBox.Text = portDefault.ToString();
            idTextBox.Text = idDefault;
        }
    }
}
