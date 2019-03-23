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
using System.Windows.Shapes;

namespace UDBM
{
    /// <summary>
    /// Interaction logic for Preferences.xaml
    /// </summary>
    public partial class Preferences : Window
    {

        public Preferences()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            fillFields();

        }

        private bool assureIsInt(TextBox sender)
        {
            try{
                int i = Int16.Parse(sender.Text);
                return true;
            }
            catch
            {
                sender.Foreground = Brushes.Red;
                return false;
            }
        }

        private void fillFields()
        {
            inLimit.Text = Properties.Settings.Default.prLimit.ToString();
            inAutoApply.IsChecked = Properties.Settings.Default.prAutoApply;
            inFitMethod.SelectedIndex = Properties.Settings.Default.prFitMethod;
            inWorkspace.Text = Properties.Settings.Default.prWorkspace;
            inAutoRefresh.IsChecked = Properties.Settings.Default.prAutoRefresh;
            inServer.Text = Properties.Settings.Default.lgHost;
            inUsername.Text = Properties.Settings.Default.lgUser;
            inDbType.SelectedIndex = Properties.Settings.Default.lgDbType;
            inLimit.Foreground = Brushes.White;
            hiddenDbs.ItemsSource = Properties.Settings.Default.prHiddenDbs;
        }

        private void ApplyBtnClick(object sender, RoutedEventArgs e)
        {
            if (!assureIsInt(inLimit)) return;
            Properties.Settings.Default.prLimit = Int16.Parse(inLimit.Text);
            Properties.Settings.Default.prAutoApply = (bool)inAutoApply.IsChecked;
            Properties.Settings.Default.prFitMethod = inFitMethod.SelectedIndex;
            Properties.Settings.Default.prWorkspace = inWorkspace.Text;
            Properties.Settings.Default.prAutoRefresh = (bool)inAutoRefresh.IsChecked;
            Properties.Settings.Default.lgHost = inServer.Text;
            Properties.Settings.Default.lgUser = inUsername.Text;
            Properties.Settings.Default.lgDbType = inDbType.SelectedIndex;
            Properties.Settings.Default.Save();
            inLimit.Foreground = Brushes.White;


        }

        private void OkBtnClick(object sender, RoutedEventArgs e)
        {
            ApplyBtnClick(sender, e);
            this.Close();
        }

        private void CloseBtnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void RemoveDbFromListOfHidden(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.prHiddenDbs.Remove((sender as Control).ToolTip as String);
            hiddenDbs.Items.Refresh();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(toHideDb.Text))
                return;
            Properties.Settings.Default.prHiddenDbs.Add(toHideDb.Text.ToLower());
            hiddenDbs.Items.Refresh();
        }
    }
}
