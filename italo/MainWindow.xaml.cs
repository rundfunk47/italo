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

        private static System.Windows.Controls.Button _scanButton;
        private static System.Windows.Controls.ProgressBar _progressBar;
        private static System.Windows.Controls.TextBox _textLog;

        public MainWindow() 
        {
            InitializeComponent();   

            _scanButton = scanButton;
            _textLog = textLog;
            _progressBar = progressBar;

            addWithoutNotify.IsChecked = Properties.Settings.Default.SilentAdd;

            _log = new Logger(_textLog);
            _libraryScanner = new LibraryScanner(_log);

            _log.LogInfo("Starting program");

            this.folderPathTextBox.Text = Properties.Settings.Default.SearchPath;

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

        private void ProgramExit()
        {
            System.Windows.Application.Current.Shutdown();
        }

        internal static void ShowNotify(string p)
        {
            if (!Properties.Settings.Default.SilentAdd)
                _notifyIcon.ShowBalloonTip(3000, _appName, p, ToolTipIcon.Info);
        }

        /*private*/

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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.ShowDialog();
            this.folderPathTextBox.Text = dialog.SelectedPath;

            if (Directory.Exists(folderPathTextBox.Text))
            {
                Properties.Settings.Default.SearchPath = folderPathTextBox.Text;
                Properties.Settings.Default.Save();
                _libraryScanner.StartWatch(folderPathTextBox.Text);
                _log.LogDebug("Saved default path to " + folderPathTextBox.Text);
            }
            else
            {
                System.Windows.MessageBox.Show("Search directory does not exist or is unaccessible");
                folderPathTextBox.Text = Properties.Settings.Default.SearchPath;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            _libraryScanner.StartScan(Properties.Settings.Default.SearchPath, true);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        internal static void SetScanStart()
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            (Action)(() => _scanButton.IsEnabled = false));

            System.Windows.Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            (Action)(() => _scanButton.Content= "Scan in progress..."));
        }

        internal static void SetScanEnd()
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            (Action)(() => _scanButton.IsEnabled = true));

            System.Windows.Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            (Action)(() => _scanButton.Content = "Full scan now!"));
        }

        internal static void ProgressBarUpdate(int val)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            (Action)(() => _progressBar.Value = val));
        }

        private void TextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {

        }

        private void CheckBox_Checked_2(object sender, RoutedEventArgs e)
        {

        }

        private void addWithoutNotify_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SilentAdd = true;
            Properties.Settings.Default.Save();
        }

        private void addWithoutNotify_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SilentAdd = false;
            Properties.Settings.Default.Save();
        }
    }
}
