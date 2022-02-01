using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Default_Utility_Export
{
    class LogFileManager
    {
        private static string _logFile;
        private static StreamWriter _myStreamWriter;
        private static Int32 _logFileSizeLimitBytes = 10485760;


        //Write a line into the log file.
        public static void AppendtoLog(string textValue)
        {
            if (_logFile == null)
            {
                _logFile = ConfigurationManager.AppSettings["LogFile"];

                try
                {
                    _logFileSizeLimitBytes = Convert.ToInt32(ConfigurationManager.AppSettings["LogFileSizeLimitBytes"]);
                }
                catch (Exception)
                {
                    //Just let it go with default size limit.
                }

                CheckLogSize();
            }

            if (_myStreamWriter == null)
            {
                _myStreamWriter = File.AppendText(_logFile);
            }

            _myStreamWriter.Write(DateTime.Now + "      " + textValue + "\r\n");
            _myStreamWriter.Flush();
        }

        //Check for file size over limit, and save it off if it is.
        public static void CheckLogSize()
        {
            try
            {
                var fileInfo = new FileInfo(_logFile);

                if (!fileInfo.Exists) return;
                if (fileInfo.Length > _logFileSizeLimitBytes)
                {

                    if (File.Exists(_logFile + ".OLD"))
                        File.Delete(_logFile + ".OLD");

                    File.Move(_logFile, _logFile + ".OLD");
                }
            }
            catch (Exception ex)
            {
                AppendtoLog("FAILED TO SAVE OFF LOG OVER SIZE LIMIT.  " + ex.Message.Trim());
            }
        }

    }
}
