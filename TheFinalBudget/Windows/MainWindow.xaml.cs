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
using System.Data.SqlClient;
using System.Configuration;
using System.Data;
using TheFinalBudget.Windows;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Globalization;
using System.Timers;
using System.Reflection;
using System.Net.Mail;
using System.Net.Mime;
using System.Net;

namespace TheFinalBudget
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        decimal toBeBudgeted;
        public string MonthDisplayed = "";

        private DataTable _categoriesTable;
        private DataTable _accountsTable;
        private string _beforeBudgetEdit;
        private string _beforeAccountNameEdit;
        //private string _beforeAccountTotalEdit;
        private bool _pressedEnter;
        static ReportWindow report;
        System.Timers.Timer aTimer;

        public MainWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
            Setup(null);

            if (DateTime.Now.Hour >= 23)
            {
                aTimer = new System.Timers.Timer(2000);
                // Hook up the Elapsed event for the timer. 
                aTimer.Elapsed += OnTimedEventMidNight;
                aTimer.AutoReset = false;
                aTimer.Enabled = true;
            }
        }

        private void OnTimedEventMidNight(Object source, ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                sendReport();
            });
        }

        public MainWindow(string monthYear)
        {
            InitializeComponent();
            Setup(monthYear);
        }

        private void Setup(string monthYear)
        {
            toBeBudgeted = Convert.ToDecimal(Helper.getGlobalValue("ToBeBudgeted"));
            toBeBudgetedValueText.Text = "$" + toBeBudgeted.ToString("0.00");
            selectedRowGoalText.Visibility = Visibility.Hidden;

            if (monthYear == null)
            {
                DateTime dateToday = DateTime.Now;
                MonthDisplayed = dateToday.Month.ToString().PadLeft(2, '0') + dateToday.Year;
            }
            else
            {
                MonthDisplayed = monthYear;
            }

            int listMonth = Convert.ToInt32(MonthDisplayed.Remove(2));
            int listyear = Convert.ToInt32(MonthDisplayed.Remove(0, 2));
            navigateMonthsText.Text = new DateTime(listyear, listMonth, 1).ToString("MMMM");

            var monthFound = Helper.getDBData("CategoryMonths", new List<string>() { "Month" },
                new Dictionary<string, string>() { { "Month", MonthDisplayed } });

            if (monthFound.Rows.Count == 0)
            {
                var allCategories = Helper.getDBData("Categories", new List<string>() { "CategoryId" });

                foreach (DataRow categories in allCategories.Rows)
                {
                    var values = new List<KeyValuePair<string, string>>();
                    values.Add(new KeyValuePair<string, string>("Month", MonthDisplayed));
                    values.Add(new KeyValuePair<string, string>("Budget", "0.00"));
                    values.Add(new KeyValuePair<string, string>("Activity", "0.00"));
                    values.Add(new KeyValuePair<string, string>("CategoryId", categories["CategoryId"].ToString()));
                    var sqlReturn = Helper.addDBRow("CategoryMonths", values, 
                        new Dictionary<string, string>() { { "Month", MonthDisplayed }, { "CategoryId", categories["CategoryId"].ToString() } });
                    if (sqlReturn.Key == false)
                    {
                        MessageBox.Show(this, sqlReturn.Value, "Error", MessageBoxButton.OK);
                        return;
                    }
                }
            }

            _categoriesTable = Helper.GetCategoriesByMonth(MonthDisplayed);

            var displayedDateTime = new DateTime(listyear, listMonth, 1);
            var currentDateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            if (displayedDateTime < currentDateTime)
            {
                var previousMonth = displayedDateTime.AddMonths(-1).Month.ToString().PadLeft(2, '0') + displayedDateTime.AddMonths(-1).Year;

                monthFound = Helper.getDBData("CategoryMonths", new List<string>() { "Month" },
                new Dictionary<string, string>() { { "Month", previousMonth } });

                if (monthFound.Rows.Count == 0)
                {
                    btnLastMonth.IsEnabled = false;
                    btnLastMonth.Opacity = 0.5;

                    foreach(DataRow row in _categoriesTable.Rows)
                    {
                        if((string)row["CategoryId"] != "")
                        {
                            row["BudgetStyle"] = "ReadOnly";
                        }
                    }
                }
            }
            else
            {
                btnLastMonth.IsEnabled = true;
                btnLastMonth.Opacity = 1;
            }

            CategoryDataGrid.ItemsSource = _categoriesTable.DefaultView;

            _accountsTable = Helper.GetAccounts();

            AccountDataGrid.ItemsSource = _accountsTable.DefaultView;
        }

        private void btnCategoryEdit_Click(object sender, RoutedEventArgs e)
        {
            DataRowView currentRow = (DataRowView)((Button)e.Source).DataContext;
            CategoryEditWindow categoryEditWindow;

            if (string.IsNullOrEmpty(currentRow["Category"].ToString()))
            {
                categoryEditWindow = new CategoryEditWindow(currentRow["Category"].ToString(), MonthDisplayed);
            }
            else
            {
                currentRow["Category"] = currentRow["Category"].ToString().TrimStart(' ');
                categoryEditWindow = new CategoryEditWindow(currentRow, MonthDisplayed, toBeBudgeted);
            }
            categoryEditWindow.Owner = this;
            categoryEditWindow.Show();
            categoryEditWindow.Owner = null;

            this.Close();
        }        

        private void btnbtnAddToGrid_Click(object sender, RoutedEventArgs e)
        {
            AddToGridWindow addWindow = new AddToGridWindow(MonthDisplayed);
            addWindow.Owner = this;
            addWindow.Show();
            addWindow.Owner = null;
            this.Close();
        }

        private void budgetUpdatingTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (e.Source.GetType() != typeof(TextBox))
            {
                return;
            }
            
            DataRowView currentRow = (DataRowView)((TextBox)e.Source).DataContext;
            if ((string)currentRow["CategoryId"] != "")
            {
                _beforeBudgetEdit = (string)currentRow["Budgeted"];

                selectedRowGoalText.Visibility = Visibility.Visible;
                selectedRowGoalValue.Text = "$" + (string)currentRow["Goal"];

                int i = CategoryDataGrid.Items.IndexOf(currentRow);
                _categoriesTable.Rows[i]["Budgeted"] = "";
                _pressedEnter = false;
            }
        }

        private void budgetUpdatingTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Source.GetType() != typeof(TextBox))
            {
                return;
            }
            DataRowView currentRow = (DataRowView)((TextBox)e.Source).DataContext;

            if ((string)currentRow["CategoryId"] != "")
            {
                if (e.Key == Key.Enter)
                {
                    _pressedEnter = true;
                    //Budget value
                    string afterEdit = ((TextBox)(e.Source)).Text;
                    decimal afterEditDecimal = 0;

                    decimal updatedValue;

	                try
	                {	                
						if (!Decimal.TryParse(afterEdit, out afterEditDecimal))
						{
							MessageBox.Show(this, "Budget amount must be a decimal.", "Budget Amount Error", MessageBoxButton.OK);
							var currentRowIndexError = CategoryDataGrid.Items.IndexOf(CategoryDataGrid.CurrentItem);
							_categoriesTable.Rows[currentRowIndexError]["Budgeted"] = _beforeBudgetEdit;

							selectedRowGoalText.Visibility = Visibility.Hidden;
							selectedRowGoalValue.Text = "";
							_beforeBudgetEdit = "";
							CategoryDataGrid.Focus();
							return;
						}
	                }
	                catch (Exception)
	                {
						MessageBox.Show(this, "Budget amount must be a decimal.", "Budget Amount Error", MessageBoxButton.OK);
		                var currentRowIndexError = CategoryDataGrid.Items.IndexOf(CategoryDataGrid.CurrentItem);
						_categoriesTable.Rows[currentRowIndexError]["Budgeted"] = _beforeBudgetEdit;

						selectedRowGoalText.Visibility = Visibility.Hidden;
		                selectedRowGoalValue.Text = "";
		                _beforeBudgetEdit = "";
		                CategoryDataGrid.Focus();
						return;
					}
					updatedValue = Convert.ToDecimal(_beforeBudgetEdit) + afterEditDecimal;

                    Helper.updateDBValue("CategoryMonths", new KeyValuePair<string, object>("Budget", updatedValue),
                        new Dictionary<string, string>() { { "CategoryId", (string)currentRow["CategoryId"] }, { "Month", MonthDisplayed } });

                    var currentRowIndex = CategoryDataGrid.Items.IndexOf(CategoryDataGrid.CurrentItem);
                    _categoriesTable.Rows[currentRowIndex]["Budgeted"] = updatedValue.ToString("0.00");
                    _beforeBudgetEdit = "";

                    //Accumulated value
                    var accumulatedTotal = Convert.ToDecimal(_categoriesTable.Rows[currentRowIndex]["Accumulated"].ToString().Replace("$", "")) + afterEditDecimal;

                    Helper.updateDBValue("Categories", new KeyValuePair<string, object>("RunningTotal", accumulatedTotal.ToString("0.00")),
                        new Dictionary<string, string>() { { "CategoryId", (string)currentRow["CategoryId"] } });
                    _categoriesTable.Rows[currentRowIndex]["Accumulated"] = "$" + (accumulatedTotal).ToString("0.00");

                    for (int i = currentRowIndex - 1; i >= 0; i--)
                    {
                        if ((string)_categoriesTable.Rows[i]["CategoryId"] == "")
                        {
                            var groupBudgetTotal = Convert.ToDecimal(_categoriesTable.Rows[i]["Budgeted"].ToString().Replace("$", ""));
                            _categoriesTable.Rows[i]["Budgeted"] = "$" + (groupBudgetTotal + afterEditDecimal).ToString("0.00");

                            var groupAccumulatedTotal = Convert.ToDecimal(_categoriesTable.Rows[i]["Accumulated"].ToString().Replace("$", ""));
                            _categoriesTable.Rows[i]["Accumulated"] = "$" + (groupAccumulatedTotal + afterEditDecimal).ToString("0.00");
                            break;
                        }
                    }

                    //ToBeBudgeted value
                    var toBeBudgeted = Convert.ToDecimal(toBeBudgetedValueText.Text.Replace("$", "")) - afterEditDecimal;

                    Helper.updateDBValue("GlobalValues", new KeyValuePair<string, object>("Value", toBeBudgeted.ToString("0.00")),
                        new Dictionary<string, string>() { { "Name", "ToBeBudgeted" } });

                    toBeBudgetedValueText.Text = "$" + toBeBudgeted.ToString("0.00");
                }
                else
                {
                    _pressedEnter = false;
                    if (e.Key == Key.Escape)
                    {
                        CategoryDataGrid.Focus();
                    }
                }
            }
        }

        private void budgetUpdatingTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (e.Source.GetType() != typeof(TextBox))
            {
                return;
            }

            DataRowView currentRow = (DataRowView)((TextBox)e.Source).DataContext;

            if ((string)currentRow["CategoryId"] != "")
            {
                if (!_pressedEnter)
                {
                    int i = CategoryDataGrid.Items.IndexOf(currentRow);
                    _categoriesTable.Rows[i]["Budgeted"] = _beforeBudgetEdit;
                }
                selectedRowGoalText.Visibility = Visibility.Hidden;
                selectedRowGoalValue.Text = "";
                _beforeBudgetEdit = "";
            }
        }

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CategoryDataGrid.Focus();
        }

        private void accountNameUpdatingTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            

            DataRowView currentRow = (DataRowView)((TextBox)e.Source).DataContext;
            _beforeAccountNameEdit = (string)currentRow["AccountName"];
            _pressedEnter = false;
        }

        private void accountNameUpdatingTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _pressedEnter = true;
                DataRowView currentRow = (DataRowView)AccountDataGrid.CurrentItem;
                string afterEdit = ((TextBox)(e.Source)).Text;

                Helper.updateDBValue("Accounts", new KeyValuePair<string, object>("AccountName", afterEdit),
                    new Dictionary<string, string>() { { "AccountId", (string)currentRow["AccountId"] } });                
            }
            else
            {
                _pressedEnter = false;
                if (e.Key == Key.Escape)
                {
                    CategoryDataGrid.Focus();
                }
            }
        }

        private void accountNameUpdatingTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!_pressedEnter)
            {
                DataRowView currentRow = (DataRowView)AccountDataGrid.CurrentItem;
                int i = AccountDataGrid.Items.IndexOf(currentRow);
                _accountsTable.Rows[i]["AccountName"] = _beforeAccountNameEdit;
            }
            _beforeAccountNameEdit = "";
        }

        private void btnNextMonth_Click(object sender, RoutedEventArgs e)
        {
            int listMonth = Convert.ToInt32(MonthDisplayed.Remove(2));
            int listyear = Convert.ToInt32(MonthDisplayed.Remove(0, 2));

            var goToMonth = new DateTime(listyear, listMonth, 1).AddMonths(1);

            MonthDisplayed = goToMonth.Month.ToString().PadLeft(2, '0') + goToMonth.Year;

            Setup(MonthDisplayed);
        }

        private void btnLastMonth_Click(object sender, RoutedEventArgs e)
        {
            int listMonth = Convert.ToInt32(MonthDisplayed.Remove(2));
            int listyear = Convert.ToInt32(MonthDisplayed.Remove(0, 2));

            var goToMonth = new DateTime(listyear, listMonth, 1).AddMonths(-1);

            MonthDisplayed = goToMonth.Month.ToString().PadLeft(2, '0') + goToMonth.Year;

            Setup(MonthDisplayed);
        }

        private void btnAddAccount_Click(object sender, RoutedEventArgs e)
        {
            var accountName = addAccountName.Text;
            var accountAmount = addAccountAmount.Text;
            bool accountCredit = (bool)addAccountCreditCard.IsChecked;
            var credit = accountCredit ? 1 : 0;

            decimal amount;
            if(string.IsNullOrEmpty(accountName))
            {
                MessageBox.Show(this, "Account name must be provided.", "Account Name Error", MessageBoxButton.OK);
                return;
            }
            else if (!Decimal.TryParse(accountAmount, out amount))
            {
                MessageBox.Show(this, "Account amount must be a decimal.", "Account Amount Error", MessageBoxButton.OK);
                return;
            }

            var values = new List<KeyValuePair<string, string>>();
            values.Add(new KeyValuePair<string, string>("AccountName", accountName));
            values.Add(new KeyValuePair<string, string>("IsCreditCard", credit.ToString()));
            values.Add(new KeyValuePair<string, string>("AccountTotal", amount.ToString("0.00")));
            var sqlReturn = Helper.addDBRow("Accounts", values, new Dictionary<string, string>() { { "AccountName", accountName } });
            if (sqlReturn.Key == false)
            {
                MessageBox.Show(this, sqlReturn.Value, "Error", MessageBoxButton.OK);
                return;
            }

            var toBeBugetedValue = Helper.getGlobalValue("ToBeBudgeted");

            Helper.updateDBValue("GlobalValues", new KeyValuePair<string,
                object>("Value", (Convert.ToDecimal(toBeBugetedValue) + amount)),
                new Dictionary<string, string>() { { "Name", "ToBeBudgeted" } });

            var account = Helper.getDBData("Accounts", new List<string>() { "AccountId" },
                new Dictionary<string, string>() { { "AccountName", accountName } });

            var accountId = (int)account.Rows[0]["AccountId"];

            //DataTable tempTable = ((DataView)AccountDataGrid.ItemsSource).ToTable();
            DataRow accountRow = _accountsTable.NewRow();
            accountRow["AccountId"] = accountId.ToString();
            accountRow["AccountName"] = accountName;
            accountRow["AccountTotal"] = "$" + amount.ToString("0.00");
            if (accountCredit)
            {
                _accountsTable.Rows.Add(accountRow);
            }
            else
            {
                _accountsTable.Rows.InsertAt(accountRow, 1);
            }

            //AccountDataGrid.ItemsSource = _accountsTable.DefaultView;

            addAccountName.Text = "";
            addAccountAmount.Text = "";
            addAccountCreditCard.IsChecked = false;
        }

        private class Account
        {
            public string AccountId;
            public string AccountName;
            public string AccountTotal;

            public Account(string accountId, string accountName, string accountTotal)
            {
                AccountId = accountId;
                AccountName = accountName;
                AccountTotal = accountTotal;
            }
        }

        private void btnTransactions_Click(object sender, RoutedEventArgs e)
        {
            TransactionsWindow transactionWindow = new TransactionsWindow(MonthDisplayed);
            transactionWindow.Owner = this;
            transactionWindow.Show();
            transactionWindow.Owner = null;
            this.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            sendReport();
        }

        private void sendReport()
        {
            List<Bitmap> AllImages = new List<Bitmap>();

            AllImages.Add(Helper.CaptureAllCategories(true));

            var count = CategoryDataGrid.Items.Count;

            if (CategoryDataGrid.Items.Count > 20)
            {
                count = count - 20;
                for (int i = 0; i < 20; i++)
                {
                    _categoriesTable.Rows[0].Delete();
                }

                report = new ReportWindow(_categoriesTable, AllImages, count);
                report.Show();

                aTimer = new System.Timers.Timer(2000);
                // Hook up the Elapsed event for the timer. 
                aTimer.Elapsed += OnTimedEvent;
                aTimer.AutoReset = true;
                aTimer.Enabled = true;

                var test = report.AllImages;
            }
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (!report.IsLoaded)
                {
                    aTimer.Stop();
                    var newHeight = 717 + (650 * report.AllImages.Count);
                    var currentHeight = 0;

                    using (Bitmap newMap = new Bitmap(1100, newHeight))
                    {
                        using (var g = Graphics.FromImage(newMap))
                        {
                            foreach (Bitmap image in report.AllImages)
                            {
                                g.DrawImage(image, 0, currentHeight);
                                if (currentHeight == 0)
                                {
                                    currentHeight = 717;
                                }
                                else
                                {
                                    currentHeight += 650;
                                }
                                image.Dispose();
                            }
                        }
                        var imageFilePath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase) + "\\finalReport.jpg";
                        imageFilePath = imageFilePath.Replace("file:\\", "");
                        newMap.Save(imageFilePath);
                    }

                    sendEmail();
                }
            });
        }

        private void sendEmail()
        {
            SmtpClient oSmtp = new SmtpClient();
            oSmtp.Port = 587;
            oSmtp.EnableSsl = true;
            oSmtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            oSmtp.UseDefaultCredentials = false;
            oSmtp.Credentials = new NetworkCredential("davidpotter895@gmail.com", "drpmjp1234!");
            oSmtp.Host = "smtp.gmail.com";
            string htmlBody = "<html><body><h1>Picture</h1><br><img src=\"cid:FINALREPORT\"></body></html>";
            AlternateView avHtml = AlternateView.CreateAlternateViewFromString
               (htmlBody, null, MediaTypeNames.Text.Html);

            LinkedResource inline = new LinkedResource("finalReport.jpg", MediaTypeNames.Image.Jpeg);
            inline.ContentId = "FINALREPORT";
            avHtml.LinkedResources.Add(inline);

            using (var mail = new MailMessage("davidpotter895@gmail.com", "meganjtrupiano@gmail.com"))
            {
                mail.AlternateViews.Add(avHtml);

                var imageFilePath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase) + "\\finalReport.jpg";
                imageFilePath = imageFilePath.Replace("file:\\", "");
                Attachment att = new Attachment(imageFilePath);
                att.ContentDisposition.Inline = true;

                mail.Subject = "Budget Report";
                mail.Body = String.Format(
                           @"<img src=""cid:{0}"" />", inline.ContentId);

                mail.IsBodyHtml = true;
                mail.Attachments.Add(att);
                oSmtp.Send(mail);

                this.Close();
            }
        }



        //private void accountTotalUpdatingTextBox_GotFocus(object sender, RoutedEventArgs e)
        //{
        //    _beforeAccountTotalEdit = ((TextBox)e.Source).Text;
        //}

        //private void accountTotalUpdatingTextBox_KeyDown(object sender, KeyEventArgs e)
        //{
        //    if (e.Key == Key.Enter)
        //    {
        //        _pressedEnter = true;
        //        DataRowView currentRow = (DataRowView)AccountDataGrid.CurrentItem;
        //        string afterEdit = ((TextBox)(e.Source)).Text;
        //        decimal afterEditDecimal = 0;

        //        decimal updatedValue;

        //        if (!Decimal.TryParse(afterEdit, out afterEditDecimal))
        //        {
        //            MessageBox.Show("Account Total amount must be a decimal.", "Account Total Amount Error", MessageBoxButton.OK);
        //            return;
        //        }

        //        Helper.updateDBValue("Accounts", new KeyValuePair<string, object>("AccountTotal", afterEditDecimal.ToString("0.00")),
        //            new Dictionary<string, string>() { { "AccountId", (string)currentRow["AccountId"] } });

        //        var currentRowIndex = AccountDataGrid.Items.IndexOf(AccountDataGrid.CurrentItem);
        //        for (int i = currentRowIndex - 1; i >= 0; i--)
        //        {
        //            if ((string)_accountsTable.Rows[i]["AccountId"] == "")
        //            {
        //                //must get the difference between before the edit and after to update the group total
        //                var beforeEditTotal = Convert.ToDecimal(_beforeAccountTotalEdit.Replace("$", ""));
        //                var groupAccountTotal = Convert.ToDecimal(_accountsTable.Rows[i]["AccountTotal"].ToString().Replace("$", ""));
        //                _accountsTable.Rows[i]["AccountTotal"] = "$" + (groupAccountTotal + (afterEditDecimal - beforeEditTotal)).ToString("0.00");
        //                break;
        //            }
        //        }
        //    }
        //    else
        //    {
        //        _pressedEnter = false;
        //        if (e.Key == Key.Escape)
        //        {
        //            CategoryDataGrid.Focus();
        //        }
        //    }
        //}

        //private void accountTotalUpdatingTextBox_LostFocus(object sender, RoutedEventArgs e)
        //{
        //    if (!_pressedEnter)
        //    {
        //        DataRowView currentRow = (DataRowView)AccountDataGrid.CurrentItem;
        //        int i = AccountDataGrid.Items.IndexOf(currentRow);
        //        _accountsTable.Rows[i]["AccountTotal"] = _beforeAccountTotalEdit;                
        //    }
        //    _beforeAccountTotalEdit = "";
        //}
    }
}
