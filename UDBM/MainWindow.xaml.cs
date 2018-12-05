using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UDBM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        System.Windows.Forms.NotifyIcon trayIcon;
        System.Windows.Forms.ContextMenu trayMenu;
        public MainWindow()
        {
            InitializeComponent();

            inHost.Text = Properties.Settings.Default.lgHost;
            inUser.Text= Properties.Settings.Default.lgUser;
            inType.SelectedIndex = Properties.Settings.Default.lgDbType;

            trayMenu = new System.Windows.Forms.ContextMenu();
            trayMenu.MenuItems.Add("Open", showToolStripMenuItem_Click);
            trayMenu.MenuItems.Add("Exit", exitToolStripMenuItem_Click);


            trayIcon = new System.Windows.Forms.NotifyIcon();
            trayIcon.Text = "User's Database Manager Tray";
            trayIcon.Icon = UDBM.Properties.Resources.Icon1;
            trayIcon.ContextMenu = trayMenu;


        }

        private void btnLogIn_Click(object sender, RoutedEventArgs e)
        {

            if (inRemember.IsChecked == true)
            {
                Properties.Settings.Default.lgHost = inHost.Text;
                Properties.Settings.Default.lgUser = inUser.Text;
                Properties.Settings.Default.lgDbType = inType.SelectedIndex;
                Properties.Settings.Default.Save();
            }

            Central f = new Central(inHost.Text, inUser.Text, inPassword.Password, (string)((ComboBoxItem)inType.SelectedItem).Content);
            f.Owner = this;
            f.Show();
            this.Hide();
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Show();
            trayIcon.Visible = false;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void btnHide_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            trayIcon.Visible = true;
        }
    }
}
