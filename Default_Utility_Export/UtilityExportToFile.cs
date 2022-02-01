using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Default_Utility_Export
{
    class UtilityExportToFile
    {
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool FreeConsole();

        [STAThread]
        private static int Main(string[] args)
        {
            if (ConfigurationManager.AppSettings["AutoRun"] == "1")
            {
                if (Directory.Exists(Path.GetDirectoryName(ConfigurationManager.AppSettings["LogFile"])) == false)
                {
                    Console.WriteLine("Log File Derectory Doesn't Exist. Please Edit The Config File");
                    return 0;
                }
                LogFileManager.AppendtoLog("Starting Export");

                var selectedDate = DateTime.Now.Date;

                if (selectedDate == null)
                {
                    LogFileManager.AppendtoLog("Pick A Date Please");
                    Console.WriteLine("Date Set To Null");
                    LogFileManager.AppendtoLog("Ending Export");
                    return 0;
                }
                else
                {
                    LogFileManager.AppendtoLog("Automated Date: " + selectedDate.ToShortDateString());
                    DateTime Date = selectedDate;

                    try
                    {
                        UtilityExportToFile.getGLandExportFile(ConfigurationManager.AppSettings["FolderToWriteFile"], Date);
                    }
                    catch (Exception e2)
                    {
                        LogFileManager.AppendtoLog(e2.Message);
                        Console.WriteLine(e2.Message);
                        LogFileManager.AppendtoLog("Ending Export");
                        return 0;
                    }
                }
                LogFileManager.AppendtoLog("Ending Export");
                return 1;
            }
            else
            {
                FreeConsole();
                var app = new MainWindow();
                app.ShowDialog();
                return 1;
            }
        }

        private static DataTable getGLInformation(DateTime Date)
        {
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings["EnerGov_Prod"];

            SqlDataAdapter adapter = new SqlDataAdapter();
            DataTable results = new DataTable();

            using (SqlConnection sqlConnection = new SqlConnection(settings.ConnectionString))
            {
                // rename to get correct stored procedure
                using (SqlCommand sqlcmd = new SqlCommand("uspGetUtilityBilling", sqlConnection))
                {
                    sqlConnection.Open();
                    sqlcmd.CommandType = CommandType.StoredProcedure;
                    sqlcmd.Parameters.Add("@TransactionDate", SqlDbType.DateTime).Value = Date;
                    adapter.SelectCommand = sqlcmd;
                    adapter.Fill(results);
                }
            }
            
            return results;
        }

        private static void exportFileFromDataTable(string fullFilePath, DataTable ExportMe, DateTime selectedDate)
        {
            using (StreamWriter sw = new StreamWriter(fullFilePath))
            {
                string fileDelimiter = ConfigurationManager.AppSettings["FileDelimiter"];

                IEnumerable<string> columnNames = ExportMe.Columns.Cast<DataColumn>().
                                  Select(column => column.Caption);
                var columns = columnNames.ToList();
               
                if (Convert.ToBoolean(ConfigurationManager.AppSettings["FileFirstRowColumnNames"]))
                {
                    sw.WriteLine(string.Join(fileDelimiter, columns.ToArray()));
                }
                
                foreach (DataRow row in ExportMe.Rows)
                {
                    string rowValue = "";
                    for (int i = 0; i < ExportMe.Columns.Count; i++)
                    {
                        string value = "";
                        value = row[i].ToString();

                        // adding delimter to all columns except the last column
                        if (i < ExportMe.Columns.Count - 1)
                        {
                            value += fileDelimiter;
                        }
                        rowValue += value;
                    }
                    sw.WriteLine(rowValue);
                }
            }

            // if want to relocate to archieve folder
            if (Convert.ToBoolean(ConfigurationManager.AppSettings["RelocateToArchieveFolder"]))
            {
                archiveFile(fullFilePath, selectedDate);
            }

            if (Convert.ToBoolean(ConfigurationManager.AppSettings["EnableSFTP"]))
            {
                if (SFTPFile(fullFilePath) == true)
                {
                    LogFileManager.AppendtoLog("File successfully SFTP.");
                }
                else
                {
                    Console.WriteLine("File was not successfully transfered.");
                    LogFileManager.AppendtoLog("File was not successfully transfered.");
                    LogFileManager.AppendtoLog("Ending Export");
                }
            }

            

        }

        public static void archiveFile(string fullFilePath, DateTime selectedDate)
        {
            string archiveFilePath = ConfigurationManager.AppSettings["ArchiveLocation"];

            string archiveFileName = fullFilePath.Substring(fullFilePath.LastIndexOf(@"\") + 1);
            archiveFileName = archiveFileName.Substring(0, archiveFileName.LastIndexOf("."));

            string fileExtension = ConfigurationManager.AppSettings["fileExtension"];

            string FullArchiveFilePath = String.Empty;

            if (Convert.ToBoolean(ConfigurationManager.AppSettings["addArchiveDate"]))
            {
                archiveFileName += selectedDate.ToString(ConfigurationManager.AppSettings["ArchiveDateFormat"]);
            }

            FullArchiveFilePath = archiveFilePath + archiveFileName + fileExtension;

            if (File.Exists(FullArchiveFilePath))
            {
                for (int i = 2; ; i++)
                {
                    if (File.Exists(archiveFilePath + archiveFileName + "_" + i.ToString() + fileExtension) == false)
                    {
                        FullArchiveFilePath = archiveFilePath + archiveFileName + "_" + i.ToString() + fileExtension;
                        break;
                    }
                }
            }

            File.Copy(fullFilePath, FullArchiveFilePath);
        }


        public static void getGLandExportFile(string directorypath, DateTime Date)
        {
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings["EnerGov_Prod"];

            if (TestSqlServerConnection(settings.ConnectionString) == false)
            {
                throw new Exception("Failed To Connect To Sql Instance Configured In The App Config");
            }
            if (Directory.Exists(directorypath) == false)
            {
                throw new Exception("Directory Path Defined In The Configuration Does Not Exist");
            }

            string fullFilePath = defineFullFilePath(directorypath, Date);
            DataTable dt = getGLInformation(Date);
            exportFileFromDataTable(fullFilePath, dt, Date);

        }

        public static string defineFullFilePath(string directorypath, DateTime date)
        {
            string FileName = ConfigurationManager.AppSettings["fileName"];
            string combinedpath = directorypath + FileName;

            if (Convert.ToBoolean(ConfigurationManager.AppSettings["fileNameIncludeDate"]))
            {
                combinedpath += date.ToString(ConfigurationManager.AppSettings["FileNameDateFormat"]);
            }

            string fileExtension = ConfigurationManager.AppSettings["fileExtension"];

            if (File.Exists(combinedpath + fileExtension) == false)
            {
                return combinedpath + fileExtension;
            }
            else
            {
                if (Convert.ToBoolean(ConfigurationManager.AppSettings["DeleteDuplicateFile"]))
                {
                    File.Delete(combinedpath + fileExtension);
                }

                if (Convert.ToBoolean(ConfigurationManager.AppSettings["AddNumberToDuplicates"]))
                {
                    for (int i = 2; ; i++)
                    {
                        if (File.Exists(combinedpath + "_" + i.ToString() + fileExtension) == false)
                        {
                            return combinedpath + "_" + i.ToString() + fileExtension;
                        }
                    }
                }
                else
                {
                    return combinedpath + fileExtension;
                }
                
            }
        }

        private static bool SFTPFile(string fullFilePath)
        {
            var sftpConfig = new SFTPConfig
            {
                Host = ConfigurationManager.AppSettings["SFTPHost"],
                Port = Convert.ToInt32(ConfigurationManager.AppSettings["SFTPPort"]),
                UserName = ConfigurationManager.AppSettings["SFTPUserName"],
                Password = ConfigurationManager.AppSettings["SFTPPassword"]
            };

            SftpClient sftpClient = new SftpClient(sftpConfig.Host, sftpConfig.Port, sftpConfig.UserName, sftpConfig.Password);

            try
            {
                sftpClient.Connect();
                FileInfo transferFile = new FileInfo(fullFilePath);
                var remoteFilePath = ConfigurationManager.AppSettings["RemoteFileLocation"];
                sftpClient.UploadFile(transferFile.OpenRead(), remoteFilePath + "/" + transferFile.Name, true);

            }
            catch (Exception ex)
            {
                LogFileManager.AppendtoLog(ex.Message);
                Console.WriteLine(ex.Message);
                LogFileManager.AppendtoLog("Ending Export");
                return false;
            }
            finally
            {
                sftpClient.Disconnect();
            }

            return true;
        }

        private static bool TestSqlServerConnection(string connectionString)
        {
            bool testPassed;

            try
            {
                SqlConnection sqlConnectionTest = new SqlConnection(connectionString);

                sqlConnectionTest.Open();
                sqlConnectionTest.Close();
                testPassed = true;
            }
            catch (Exception)
            {
                testPassed = false;
            }
            return testPassed;
        }
    }
}

