using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
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

namespace Default_Utility_Export
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void B_Run_Click(object sender, RoutedEventArgs e)
        {

            if (Directory.Exists(System.IO.Path.GetDirectoryName(ConfigurationManager.AppSettings["LogFile"])) == false)
            {
                MessageBox.Show("Log File Directory Doesn't Exist. Please Edit The Config File");
                return;
            }

            LogFileManager.AppendtoLog("Starting Export");

            DateTime? startSelectedDate = DP_Start_Date.SelectedDate;
            DateTime? endSelectedDate = DP_End_Date.SelectedDate;

            if (ValidateDate(startSelectedDate, endSelectedDate))
            {
                DateTime dtStart = Convert.ToDateTime(startSelectedDate);
                DateTime dtEnd = Convert.ToDateTime(endSelectedDate);

                LogFileManager.AppendtoLog("Selected Start Date: " + dtStart.ToShortDateString());
                LogFileManager.AppendtoLog("Selected End Date: " + dtEnd.ToShortDateString());

                try
                {
                    for (DateTime dt = dtStart; dt.Date <= dtEnd.Date; dt = dt.AddDays(1))
                    {
                        ExportFolder.Content = UtilityExportToFile.defineFullFilePath(ConfigurationManager.AppSettings["FolderToWriteFile"], dt);
                        UtilityExportToFile.getGLandExportFile(ConfigurationManager.AppSettings["FolderToWriteFile"], dt);
                        MessageBox.Show("Export Completed Successfully", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    
                }
                catch (Exception e2)
                {
                    LogFileManager.AppendtoLog(e2.Message);
                    MessageBox.Show(e2.Message, "", MessageBoxButton.OK, MessageBoxImage.Error);
                    LogFileManager.AppendtoLog("Ending Export");
                    return;
                }
            }
            else
            {
                return;
            }
            LogFileManager.AppendtoLog("Ending Export");
        }

        private void DP_Date_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedPostingDate = DP_Start_Date.SelectedDate;
            DateTime dt = selectedPostingDate ?? DateTime.MinValue;

            ExportFolder.Content = UtilityExportToFile.defineFullFilePath(ConfigurationManager.AppSettings["FolderToWriteFile"], dt);
        }

        private bool ValidateDate(DateTime? startDate, DateTime? endDate)
        {
            bool validDate = true;
            string message = string.Empty;

            if (startDate == null && validDate)
            {
                validDate = false;
                message = "Pick A Start Date Please";
            }
            if (endDate == null && validDate)
            {
                validDate = false;
                message = "Pick A End Date Please";
            }

            if (endDate < startDate && validDate)
            {
                validDate = false;
                message = "Start date needs to be before end date.";
            }

            int MaxDays = Convert.ToInt32(ConfigurationManager.AppSettings["MaxDays"]);
            double numberOfDays = (Convert.ToDateTime(EndDate) - Convert.ToDateTime(StartDate)).TotalDays;
            if (numberOfDays < MaxDays && validDate)
            {
                validDate = false;
                message = "The maxium number of days can not exceed " + MaxDays.ToString() + " days.";
            }

            if (validDate == false)
            {
                LogFileManager.AppendtoLog(message);
                MessageBox.Show(message, "Error");
                LogFileManager.AppendtoLog("Ending Export");
            }

            return validDate;
        }

    }

}
