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
using Newtonsoft.Json;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Globalization;
using System.ComponentModel;
using System.Data;
using System.Timers;

namespace TheFinalBudget.Windows
{
    /// <summary>
    /// Interaction logic for ReportWindow.xaml
    /// </summary>
    public partial class ReportWindow : Window
    {
        bool _shown = false;
        public List<Bitmap> AllImages;
        DataTable _dataTable;
        static ReportWindow report;
        System.Timers.Timer aTimer;

        public ReportWindow(DataTable categoriesTable, List<Bitmap> originalImages, int categoriesTableLength)
        {
            InitializeComponent();
            this.Closing += new CancelEventHandler(OnWindowClosing);
            AllImages = originalImages;
            _dataTable = categoriesTable;
            
            CategoryDataGrid.ItemsSource = categoriesTable.DefaultView;

            aTimer = new System.Timers.Timer(2000);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            if(report != null)
            {
                if(!report.IsLoaded)
                {
                    aTimer.Stop();
                    this.Close();
                }
            }
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            if (_shown)
                return;

            _shown = true;

            AllImages.Add(Helper.CaptureAllCategories(false));

            var count = CategoryDataGrid.Items.Count;
            if (CategoryDataGrid.Items.Count > 26)
            {
                count = count - 26;
                for (int i = 0; i < 26; i++)
                {
                    _dataTable.Rows[0].Delete();
                }

                report = new ReportWindow(_dataTable, AllImages, count);
                report.Show();
            }
            else
            {
                aTimer.Stop();
                this.Close();
            }
        }

        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if(report != null)
            {
                report.Close();
            }
        }
    }
}
