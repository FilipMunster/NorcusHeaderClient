using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
    }
}
