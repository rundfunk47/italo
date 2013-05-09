using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Windows.Threading; 

namespace italo
{
    public class Logger
    {
        private System.Windows.Controls.TextBox _textLog;

        /*
        private string _filename = "test.log";

        private string GetTempPath()
        {
            string path = System.Environment.GetEnvironmentVariable("TEMP");
            if (!path.EndsWith("\\")) path += "\\";
                return path;
        }
        */

        private void Log(string msg)
        {

            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)delegate() {

                _textLog.AppendText(System.String.Format("{0:G}: {1}.", System.DateTime.Now, msg) + Environment.NewLine);
                _textLog.ScrollToEnd();
            });

            /*
            System.IO.StreamWriter sw = System.IO.File.AppendText(GetTempPath() + _filename); ;

            try
            {
                string logLine = System.String.Format("{0:G}: {1}.", System.DateTime.Now, msg);
                sw.WriteLine(logLine);
            }
            finally
            {
                sw.Close();
            }
            */
        }

        /*private*/

        /*public string GetLogfilePath()
        {
            string path = System.Environment.GetEnvironmentVariable("TEMP");
            if (!path.EndsWith("\\")) path += "\\";
                return (path + _filename);
        }
        */

        public Logger(System.Windows.Controls.TextBox tl)
        {
            _textLog = tl;
            
        }

        public void LogError(string msg)
        {
            Log("ERROR: " + (msg));
        }

        public void LogDebug(string msg)
        {
            #if DEBUG
            {
                Log("DEBUG: " + (msg));
            }
            #endif
        }

        public void LogInfo(string msg)
        {
            Log("INFO: " + (msg));
        }
    }
}
