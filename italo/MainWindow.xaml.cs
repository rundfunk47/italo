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
        private static System.Windows.Controls.Button _btnDeadRefs;
        private static System.Windows.Controls.TextBox _txtLog;
        private static System.Windows.Controls.ProgressBar _pgsBar;

        public MainWindow() 
        {
            InitializeComponent();   

            _btnScan = btnScan;
            _btnDeadRefs = btnDeadRefs;
            _txtLog = txtLog;
            _pgsBar = pgsBar;
            _log = new Logger(_txtLog);
            _libraryScanner = new LibraryScanner(_log);
            _log.LogInfo("Starting program");

            //Initialize from settings
            chkSilentAdd.IsChecked = Properties.Settings.Default.stngSilentAdd;
            chkScanAtStartup.IsChecked = Properties.Settings.Default.stngScanAtStartup;
            txtScanPath.Text = Properties.Settings.Default.stngScanPath;

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

            //Hide main window
            this.Hide();

            if (Properties.Settings.Default.stngScanPath == "")
                ShowNotify("Please set up a scan directory in order to use " + _appName);
            else
                _libraryScanner.StartWatch(Properties.Settings.Default.stngScanPath);

            if (Properties.Settings.Default.stngScanAtStartup == true && (!(Properties.Settings.Default.stngScanPath == "")))
                _libraryScanner.StartScan(Properties.Settings.Default.stngScanPath, true, 0);
        }

        internal static void ShowNotify(string p)
        {
            if (!Properties.Settings.Default.stngSilentAdd)
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
            try
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                (Action)(() => _pgsBar.Value = val));
            }
            catch {}
        }

        internal static void DeadRefsDisable()
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                (Action)(() => _btnDeadRefs.Content = "No dead references"));

                System.Windows.Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                (Action)(() => _btnDeadRefs.IsEnabled = false));
            }
            catch {}
        }

        internal static void DeadRefsUpdate(int number)
        {
            try
            {
                string s = "Remove " + number + " dead references...";

                System.Windows.Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                (Action)(() => _btnDeadRefs.Content = s));
            }
            catch {}
        }

        internal static void DeadRefsEnable()
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                (Action)(() => _btnDeadRefs.IsEnabled = true));
            }
            catch {}
        }

        /*private*/

        private void ProgramExit()
        {
            App.Current.Shutdown();
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
                _notifyIcon.Visible = false;
                this.ProgramExit();
            }
        }

        /* Controls */

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.ShowDialog();
            this.txtScanPath.Text = dialog.SelectedPath;

            if (Directory.Exists(txtScanPath.Text))
            {
                Properties.Settings.Default.stngScanPath = txtScanPath.Text;
                Properties.Settings.Default.Save();
                _libraryScanner.StartWatch(txtScanPath.Text);
                _log.LogDebug("Saved default path to " + txtScanPath.Text);
            }
            else
            {
                System.Windows.MessageBox.Show("Search directory does not exist or is unaccessible");
                txtScanPath.Text = Properties.Settings.Default.stngScanPath;
            }
        }

        private void BtnScan_Click(object sender, RoutedEventArgs e)
        {
            _libraryScanner.StartScan(Properties.Settings.Default.stngScanPath, true, 0);
        }

        private void ChkSilentAdd_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.stngSilentAdd = true;
            Properties.Settings.Default.Save();
        }

        private void ChkSilentAdd_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.stngSilentAdd = false;
            Properties.Settings.Default.Save();
        }

        private void ChkScanAtStartup_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.stngScanAtStartup = true;
            Properties.Settings.Default.Save();
        }

        private void ChkScanAtStartup_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.stngScanAtStartup = false;
            Properties.Settings.Default.Save();
        }

        private void BtnDeadRefs_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.MessageBox.Show("Are you sure you want delete references to " + _libraryScanner.GetDeadRefsCount() + " files?", _appName, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                //FIXME: Thread? Progress? DO WE CARE?
                _libraryScanner.DeleteDeadRefs();
            }
        }
    }
}
