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
using System.Windows.Forms;
using System.IO;
using System.Windows.Threading;

namespace italo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window 
    {
        private Logger _log;
        private LibraryScanner _libraryScanner;
        private static NotifyIcon _notifyIcon;

        private static string _appName = "iTunes Automatic Library Organizer";

        private static System.Windows.Controls.Button _btnScan;
        private static System.Windows.Controls.TextBox _txtLog;
        private static System.Windows.Controls.ProgressBar _pgsBar;

        public MainWindow() 
        {
            InitializeComponent();   

            _btnScan = btnScan;
            _txtLog = txtLog;
            _pgsBar = pgsBar;

            chkSilentAdd.IsChecked = Properties.Settings.Default.SilentAdd;

            _log = new Logger(_txtLog);
            _libraryScanner = new LibraryScanner(_log);

            _log.LogInfo("Starting program");

            this.txtFolderPath.Text = Properties.Settings.Default.SearchPath;

            //var asd = this.addWithoutNotify.IsChecked;

            #region notifyIcon

            _notifyIcon = new NotifyIcon();
            System.Windows.Forms.ContextMenu contextMenu = new System.Windows.Forms.ContextMenu();

            try {
                _notifyIcon.Icon = Properties.Resources.icon;
            }
            catch {
                System.Windows.MessageBox.Show("Icon not found, exiting");
                this.ProgramExit();
            }

            _notifyIcon.Visible = true;

            _notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(RestoreWindow);
            _notifyIcon.BalloonTipClicked += new EventHandler(RestoreWindow);

            EventHandler restore = new EventHandler(RestoreWindow);
            contextMenu.MenuItems.Add("Restore Window", restore);

            EventHandler exit = new EventHandler(ConfirmExit);
            contextMenu.MenuItems.Add("E&xit", exit);

            _notifyIcon.ContextMenu = contextMenu;

            #endregion

            this.Hide();

            if (Properties.Settings.Default.SearchPath == "")
                ShowNotify("Please set up a scan directory in order to use " + _appName);
            else
                _libraryScanner.StartWatch(Properties.Settings.Default.SearchPath);
        }

        internal static void ShowNotify(string p)
        {
            if (!Properties.Settings.Default.SilentAdd)
                _notifyIcon.ShowBalloonTip(3000, _appName, p, ToolTipIcon.Info);
        }

        internal static void SetScanStart()
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            (Action)(() => _btnScan.IsEnabled = false));

            System.Windows.Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            (Action)(() => _btnScan.Content = "Scan in progress..."));
        }

        internal static void SetScanEnd()
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            (Action)(() => _btnScan.IsEnabled = true));

            System.Windows.Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            (Action)(() => _btnScan.Content = "Full scan now!"));
        }

        internal static void ProgressBarUpdate(int val)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            (Action)(() => _pgsBar.Value = val));
        }

        /*private*/

        private void ProgramExit()
        {
            System.Windows.Application.Current.Shutdown();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            /*Is this controversial?*/
            base.OnClosing(e);
            e.Cancel = true;
            this.Hide();
        }
        
        private void RestoreWindow(object sender, EventArgs e)
        {
            this.Show();
        }

        private void ConfirmExit(object sender, EventArgs e)
        {
            if (System.Windows.MessageBox.Show("Are you sure you want to quit?", _appName, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                this.ProgramExit();
            }
        }

        /* Controls */

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.ShowDialog();
            this.txtFolderPath.Text = dialog.SelectedPath;

            if (Directory.Exists(txtFolderPath.Text))
            {
                Properties.Settings.Default.SearchPath = txtFolderPath.Text;
                Properties.Settings.Default.Save();
                _libraryScanner.StartWatch(txtFolderPath.Text);
                _log.LogDebug("Saved default path to " + txtFolderPath.Text);
            }
            else
            {
                System.Windows.MessageBox.Show("Search directory does not exist or is unaccessible");
                txtFolderPath.Text = Properties.Settings.Default.SearchPath;
            }
        }

        private void BtnScan_Click(object sender, RoutedEventArgs e)
        {
            _libraryScanner.StartScan(Properties.Settings.Default.SearchPath, true, 0);
        }

        private void ChkSilentAdd_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SilentAdd = true;
            Properties.Settings.Default.Save();
        }

        private void ChkSilentAdd_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SilentAdd = false;
            Properties.Settings.Default.Save();
        }
    }
}
