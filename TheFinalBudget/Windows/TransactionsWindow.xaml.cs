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
using System.Configuration;
using System.IO;
using System.Reflection;
using TheFinalBudget.Model;
using Newtonsoft.Json;
using System.Data;
using System.Text.RegularExpressions;

namespace TheFinalBudget.Windows
{
    /// <summary>
    /// Interaction logic for TransactionsWindow.xaml
    /// </summary>
    public partial class TransactionsWindow : Window
    {
        private string _currentMonth;
        private bool foundFile = false;
        private string transactionFolderPath;

        private KeyValuePair<string, string> newSelectedCategory = new KeyValuePair<string, string>("", "");

        DataTable displayDT;

        private bool firstSetup = false;

        public TransactionsWindow(string currentMonth)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            _currentMonth = currentMonth;
            InitializeComponent();

            var endMonth = DateTime.Now;
            var lastMonth = new DateTime(endMonth.AddMonths(-1).Year, endMonth.AddMonths(-1).Month, 1);
            endMonth = lastMonth.AddMonths(2);
            urlDateRange.Text = "?startDate=" + lastMonth.ToShortDateString() + "&endDate=" + endMonth.ToShortDateString();

            transactionFolderPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase) + "\\Transactions";
            transactionFolderPath = transactionFolderPath.Replace("file:\\", "");

            if(!File.Exists(transactionFolderPath))
            {
                Directory.CreateDirectory(transactionFolderPath);
            }

            transactionGrid.Visibility = Visibility.Hidden;

            var firstTime = Helper.getGlobalValue("FirstSetup");
            if (firstTime == "True")
            {
                Helper.updateDBValue("GlobalValues", new KeyValuePair<string,
                        object>("Value", "False"),
                        new Dictionary<string, string>() { { "Name", "FirstSetup" } });

                firstSetup = true;
            }
        }

        private void checkFolder()
        {
            List<Transaction> transactionList = new List<Transaction>();
            try
            {
                //await Task.Run(() =>
                //{
                //    do
                //    {
                var files = Directory.GetFiles(transactionFolderPath);
                Regex CSVParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");

                foreach (var file in files)
                {
                    using (var reader = new StreamReader(File.OpenRead(file)))
                    {
                        reader.ReadLine();
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();
                            //line = line.Replace("\"", "");
                            //var lineSplit = line.Split(',');
                            String[] lineSplit = CSVParser.Split(line);
                            for (int i = 0; i < lineSplit.Length; i++)
                            {
                                lineSplit[i] = lineSplit[i].TrimStart(' ', '"');
                                lineSplit[i] = lineSplit[i].TrimEnd('"');
                            }
                            Transaction tran = new Transaction(lineSplit[0], lineSplit[1], lineSplit[2], lineSplit[3], lineSplit[4], lineSplit[5], lineSplit[6]);
                            transactionList.Add(tran);
                        }
                    }
                    foundFile = true;
                }

                //    } while (foundFile == false);
                //});
            }
            catch (Exception e)
            {

            }

            foreach (var transaction in transactionList)
            {
                var blackListedAccountsTable = Helper.getDBData("BlackListAccounts", new List<string>() { "AccountName" },
                   new Dictionary<string, string>() { { "AccountName", transaction.AccountName } });

                if (blackListedAccountsTable.Rows.Count < 1)
                {

                    //get producerId
                    var producerTable = Helper.getDBData("Producers", new List<string>() { "ProducerId, ProducerName" },
                        new Dictionary<string, string>() { { "ProducerName", transaction.Producer } });

                    string producerId;

                    if (producerTable.Rows.Count == 0)
                    {
                        var values = new List<KeyValuePair<string, string>>();
                        values.Add(new KeyValuePair<string, string>("ProducerName", transaction.Producer));
                        values.Add(new KeyValuePair<string, string>("Description", transaction.Description));
                        var sqlReturn = Helper.addDBRow("Producers", values, new Dictionary<string, string>() { { "ProducerName", transaction.Producer } });
                        if (sqlReturn.Key == false)
                        {
                            MessageBox.Show(this, sqlReturn.Value, "Error", MessageBoxButton.OK);
                            return;
                        }

                        producerTable = Helper.getDBData("Producers", new List<string>() { "ProducerId, ProducerName" },
                        new Dictionary<string, string>() { { "ProducerName", transaction.Producer } });
                    }

                    producerId = producerTable.Rows[0]["ProducerId"].ToString();
                    //if (!producerMatches.ContainsKey(producerId))
                    //{
                    //    producerMatches.Add(producerId, producerTable.Rows[0]["ProducerName"].ToString());
                    //}

                    //************************************************************************************************************
                    //check if transaction is already imported
                    var existingTransaction = Helper.getDBData("Transactions", null,
                        new Dictionary<string, string>() { { "DateCaptured", transaction.Date.ToShortDateString() },
                        { "ProducerId", producerId }, { "Amount", transaction.Amount.ToString("0.00") } });

                    //************************************************************************************************************

                    if (existingTransaction.Rows.Count == 0)
                    {
                        var credit = transaction.IsCredit ? 1 : 0;
                        string categoryId = "-5";
                        string accountId = "-5";
                        if (!firstSetup)
                        { 
                            
                            string categoryName = "";
                            

                            //get accountId
                            var accountTable = Helper.getDBData("Accounts", new List<string>() { "AccountId, AccountName, IsCreditCard" },
                                new Dictionary<string, string>() { { "AccountName", transaction.AccountName } });

                            if (accountTable.Rows.Count == 0)
                            {
                                if (MessageBox.Show(this, "Was unable to find the account for this transaction: "
                                        + JsonConvert.SerializeObject(transaction) + ". Do you want to blacklist this account?", "Missing Account", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                                {
                                    break;
                                }
                                else
                                {
                                    var values = new List<KeyValuePair<string, string>>();
                                    values.Add(new KeyValuePair<string, string>("AccountName", transaction.AccountName));
                                    var sqlReturn1 = Helper.addDBRow("BlackListAccounts", values,
                                        new Dictionary<string, string>() { { "AccountName", transaction.AccountName } });
                                    if (sqlReturn1.Key == false)
                                    {
                                        MessageBox.Show(this, sqlReturn1.Value, "Error", MessageBoxButton.OK);
                                        return;
                                    }
                                    continue;
                                }
                            }

                            accountId = accountTable.Rows[0]["AccountId"].ToString();
                            //if (!accountMatches.ContainsKey(accountId))
                            //{
                            //    accountMatches.Add(accountId, accountTable.Rows[0]["AccountName"].ToString());
                            //}

                            //determine category if payment to cash account
                            if (accountTable.Rows[0]["IsCreditCard"].ToString() == "False")
                            {
                                if (credit == 1)
                                {
                                    //ToBeBudgeted
                                    categoryId = "-2";
                                }
                                else
                                {
									//Payment for credit card
									RefundOrPaymentWindow refundOrPaymentWindow = new RefundOrPaymentWindow(transaction);
	                                refundOrPaymentWindow.Owner = this;
	                                refundOrPaymentWindow.ShowDialog();
	                                refundOrPaymentWindow.Owner = null;

	                                var debitType = refundOrPaymentWindow.noCreditCardDepositType;

	                                if (debitType == "payment")
	                                {
		                                categoryId = "-1";
	                                }

	                                transaction.Amount = transaction.Amount * -1;
                                }
                            }
                            else if (accountTable.Rows[0]["IsCreditCard"].ToString() == "True")
                            {
                                if (credit == 1)
                                {
                                    //Credit card receiving payment
                                    categoryId = "-1";                                    
                                }
                                else
                                {
                                    transaction.Amount = transaction.Amount * -1;
                                }
                            }

                            if (categoryId != "-1" && categoryId != "-2")
                            {
                                //get categoryId
                                var categoryTable = Helper.getDBData("Categories", new List<string>() { "CategoryId, Name" },
                                    new Dictionary<string, string>() { { "Name", transaction.Category } });

                                if (categoryTable.Rows.Count == 0)
                                {
                                    //get categoryId from alternative category table
                                    var altcategoryTable = Helper.getDBData("AlternativeCategoryMap", new List<string>() { "CategoryId" },
                                    new Dictionary<string, string>() { { "AlternativeName", transaction.Category } });

                                    if (altcategoryTable.Rows.Count == 0)
                                    {
                                        CategoryNotFoundWindow notfoundWindow = new CategoryNotFoundWindow(_currentMonth, transaction);
                                        notfoundWindow.Owner = this;
                                        notfoundWindow.ShowDialog();
                                        notfoundWindow.Owner = null;

                                        categoryId = notfoundWindow.returnedCategoryId;
                                    }
                                    else
                                    {
                                        categoryId = altcategoryTable.Rows[0]["CategoryId"].ToString();
                                    }

                                    if (categoryId == "-1")
                                    {
                                        MessageBox.Show(this, "Was unable to map the category. Skipping this transaction: "
                                            + JsonConvert.SerializeObject(transaction), "Error", MessageBoxButton.OK);
                                        break;
                                    }
                                    else if (categoryId == "-9")
                                    {

                                    }
                                    else
                                    {
                                        categoryTable = Helper.getDBData("Categories", new List<string>() { "CategoryId, Name" },
                                        new Dictionary<string, string>() { { "CategoryId", categoryId } });
                                        categoryName = categoryTable.Rows[0]["Name"].ToString();
                                    }
                                }
                                else
                                {
                                    categoryId = categoryTable.Rows[0]["CategoryId"].ToString();
                                    categoryName = categoryTable.Rows[0]["Name"].ToString();
                                }

                                //if (!categoryMatches.ContainsKey(categoryId) && categoryId != "-1" && categoryId != "-9")
                                //{
                                //    categoryMatches.Add(categoryId, categoryName);
                                //}
                            }
                        }
                        var preMerge = firstSetup ? "1" : "0";

                        var values1 = new List<KeyValuePair<string, string>>();
                        values1.Add(new KeyValuePair<string, string>("DateCaptured", transaction.Date.ToShortDateString()));
                        values1.Add(new KeyValuePair<string, string>("ProducerId", producerId));
                        values1.Add(new KeyValuePair<string, string>("Amount", Math.Abs(transaction.Amount).ToString("0.00")));
                        values1.Add(new KeyValuePair<string, string>("Description", transaction.Description));
                        values1.Add(new KeyValuePair<string, string>("isCredit", credit.ToString()));
                        values1.Add(new KeyValuePair<string, string>("CategoryId", categoryId));
                        values1.Add(new KeyValuePair<string, string>("AccountId", accountId));
                        values1.Add(new KeyValuePair<string, string>("Merged", preMerge));
                        var sqlReturn = Helper.addDBRow("Transactions", values1, null, true);
                        if (sqlReturn.Key == false)
                        {
                            MessageBox.Show(this, sqlReturn.Value, "Error", MessageBoxButton.OK);
                            return;
                        }
                    }
                    else if (existingTransaction.Rows[0]["Merged"].ToString() == "False")
                    {
                        producerId = existingTransaction.Rows[0]["ProducerId"].ToString();
                        string categoryId = existingTransaction.Rows[0]["CategoryId"].ToString();
                        string accountId = existingTransaction.Rows[0]["AccountId"].ToString();

                        //producerTable = Helper.getDBData("Producers", new List<string>() { "ProducerName" },
                        //    new Dictionary<string, string>() { { "ProducerId", producerId } });
                        //if (!producerMatches.ContainsKey(producerId))
                        //{
                        //    producerMatches.Add(producerId, producerTable.Rows[0]["ProducerName"].ToString());
                        //}

                        //var categoryTable = Helper.getDBData("Categories", new List<string>() { "Name" },
                        //    new Dictionary<string, string>() { { "CategoryId", categoryId } });
                        //if (!categoryMatches.ContainsKey(categoryId) && categoryId != "-1" && categoryId != "-2" && categoryId != "-9")
                        //{
                        //    categoryMatches.Add(categoryId, categoryTable.Rows[0]["Name"].ToString());
                        //}

                        //var accountIdTable = Helper.getDBData("Accounts", new List<string>() { "AccountName" },
                        //    new Dictionary<string, string>() { { "AccountId", accountId } });
                        //if (!accountMatches.ContainsKey(accountId))
                        //{
                        //    accountMatches.Add(accountId, accountIdTable.Rows[0]["AccountName"].ToString());
                        //}
                    }
                }
            }

            var unMergedTransactions = Helper.getDBData("Transactions", null, new Dictionary<string, string>() { { "Merged", "0" } });

            displayDT = new DataTable("DisplayDT");
            displayDT.Columns.Add("TransactionId");
            displayDT.Columns.Add("CategoryId");
            displayDT.Columns.Add("Date");
            displayDT.Columns.Add("Producer");
            displayDT.Columns.Add("Amount");
            displayDT.Columns.Add("TranType");
            displayDT.Columns.Add("Category");
            displayDT.Columns.Add("Account");
            displayDT.Columns.Add("Desc");

            foreach (DataRow row in unMergedTransactions.Rows)
            {
                string tranType = row["IsCredit"].ToString();
                if(tranType == "False")
                {
                    tranType = "Debit";
                } 
                else
                {
                    tranType = "Credit";
                }

                string listCategoryName;
                if(row["CategoryId"].ToString() == "-1")
                {
                    listCategoryName = "-1";
                }
                else if(row["CategoryId"].ToString() == "-2")
                {
                    listCategoryName = "-2";
                }
                else if (row["CategoryId"].ToString() == "-9")
                {
                    listCategoryName = "-9";
                }
                else
                {
                    var tempTable = Helper.getDBData("Categories", new List<string>() { "CategoryId, Name" },
                                        new Dictionary<string, string>() { { "CategoryId", row["CategoryId"].ToString() } });
                    listCategoryName = tempTable.Rows[0]["Name"].ToString();
                }

                var tempProducerTable = Helper.getDBData("Producers", new List<string>() { "ProducerName" },
                            new Dictionary<string, string>() { { "ProducerId", row["ProducerId"].ToString() } });
                var tempPorducerName = tempProducerTable.Rows[0]["ProducerName"].ToString();

                var tempAccountIdTable = Helper.getDBData("Accounts", new List<string>() { "AccountName" },
                            new Dictionary<string, string>() { { "AccountId", row["AccountId"].ToString() } });
                var tempAccountName = tempAccountIdTable.Rows[0]["AccountName"].ToString();

                    DataRow newRow = displayDT.NewRow();
                newRow["TransactionId"] = row["TransactionId"].ToString();
                newRow["CategoryId"] = row["CategoryId"].ToString();
                newRow["Date"] = row["DateCaptured"].ToString().Split(' ')[0];
                newRow["Producer"] = tempPorducerName;
                newRow["Amount"] = (string)row["Amount"];
                newRow["TranType"] = tranType;
                newRow["Category"] = listCategoryName;
                newRow["Account"] = tempAccountName;
                newRow["Desc"] = (string)row["Description"];

                displayDT.Rows.Add(newRow);
            }

            transactionGrid.ItemsSource = displayDT.DefaultView;
            transactionGrid.Visibility = Visibility.Visible;
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow(_currentMonth);
            mainWindow.Owner = this;
            mainWindow.Show();
            mainWindow.Owner = null;
            this.Close();
        }

        private void btnImportTransactions_Click(object sender, RoutedEventArgs e)
        {
            checkFolder();
        }

        private void TextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            transactionGrid.BeginEdit();
        }

        private void ComboBox_Selected(object sender, RoutedEventArgs e)
        {
            DataRowView currentRow = (DataRowView)((Button)e.Source).DataContext;
        }

        private void btnApproveTransaction_Click(object sender, RoutedEventArgs e)
        {
            DataRowView currentRow = (DataRowView)((Button)e.Source).DataContext;

            if(newSelectedCategory.Key != "")
            {
                currentRow["CategoryId"] = newSelectedCategory.Key;
                currentRow["Category"] = newSelectedCategory.Value;
            }

            var accountTable = Helper.getDBData("Accounts", null,
                new Dictionary<string, string>() { { "AccountName", currentRow["Account"].ToString() } });

            decimal tranAmount = Convert.ToDecimal(currentRow["Amount"]);

            var dateSplit = currentRow["Date"].ToString().Split('/');

            string tranMonthYear = dateSplit[0].PadLeft(2, '0') + dateSplit[2];

            if (currentRow["CategoryId"].ToString() == "-9")
            {
                decimal accountTotal = Convert.ToDecimal(accountTable.Rows[0]["AccountTotal"]);
                decimal returnedAmount = Convert.ToDecimal(currentRow["Amount"]);

                if (currentRow["TranType"].ToString() == "Debit")
                {
                    returnedAmount = returnedAmount * -1;
                }
                
                decimal afterReturn = accountTotal + returnedAmount;

                Helper.updateDBValue("Accounts", new KeyValuePair<string,
                    object>("AccountTotal", afterReturn.ToString("0.00")),
                    new Dictionary<string, string>() { { "AccountId", accountTable.Rows[0]["AccountId"].ToString() } });
            }
            else
            {
                if ((bool)accountTable.Rows[0]["IsCreditCard"] == true)
                {
                    //Charge to credit card
                    if ((string)currentRow["TranType"] == "Debit")
                    {
                        tranAmount = tranAmount * -1;
                        //Only include values to each category in the month if not payment or income
                        if (currentRow["CategoryId"].ToString() != "-1" && currentRow["CategoryId"].ToString() != "-2" 
                            && currentRow["CategoryId"].ToString() != "-9")
                        {
                            processTransactionPerMonth(tranMonthYear, currentRow["CategoryId"].ToString(), tranAmount, currentRow["Account"].ToString());
                        }
                    }
                    //Else is a credit card payment/refund (will not update monthly values since budgeting is suppose to balance the charges)

                    Helper.updateDBValue("Accounts", new KeyValuePair<string,
                        object>("AccountTotal", (Convert.ToDecimal(accountTable.Rows[0]["AccountTotal"]) + tranAmount).ToString("0.00")),
                        new Dictionary<string, string>() { { "AccountId", accountTable.Rows[0]["AccountId"].ToString() } });
                }
                else
                {
                    //Pulling money out of checking/savings account
                    if ((string)currentRow["TranType"] == "Debit")
                    {
                        tranAmount = tranAmount * -1;
                        //Only add values to each category in the month if not payment, returns, or income
                        if (currentRow["CategoryId"].ToString() != "-1" && currentRow["CategoryId"].ToString() != "-2"
                            && currentRow["CategoryId"].ToString() != "-9")
                        {
                            processTransactionPerMonth(tranMonthYear, currentRow["CategoryId"].ToString(), tranAmount, currentRow["Account"].ToString());
                        }
                    }
                    //Money was added to checking/savings account 
                    else
                    {
                        var toBeBugetedValue = Helper.getGlobalValue("ToBeBudgeted");

                        Helper.updateDBValue("GlobalValues", new KeyValuePair<string,
                            object>("Value", (Convert.ToDecimal(toBeBugetedValue) + tranAmount)),
                            new Dictionary<string, string>() { { "Name", "ToBeBudgeted" } });
                    }

                    Helper.updateDBValue("Accounts", new KeyValuePair<string,
                        object>("AccountTotal", (Convert.ToDecimal(accountTable.Rows[0]["AccountTotal"]) + tranAmount)),
                        new Dictionary<string, string>() { { "AccountId", accountTable.Rows[0]["AccountId"].ToString() } });
                }

                if (currentRow["CategoryId"].ToString() != "-1" && currentRow["CategoryId"].ToString() != "-2" 
                    && currentRow["CategoryId"].ToString() != "-9")
                {
                    var categoryTable = Helper.getDBData("Categories", null,
                        new Dictionary<string, string>() { { "CategoryId", currentRow["CategoryId"].ToString() } });

                    Helper.updateDBValue("Categories", new KeyValuePair<string,
                        object>("RunningTotal", (Convert.ToDecimal(categoryTable.Rows[0]["RunningTotal"]) + tranAmount)),
                        new Dictionary<string, string>() { { "CategoryId", currentRow["CategoryId"].ToString() } });
                }
            }

            Helper.updateDBValue("Transactions", new KeyValuePair<string,
                    object>("Merged", 1), new Dictionary<string, string>() { { "TransactionId", currentRow["TransactionId"].ToString() } });

            foreach(DataRow row in displayDT.Rows)
            {
                if(row["TransactionId"].ToString() == currentRow["TransactionId"].ToString())
                {
                    row.Delete();
                    break;
                }
            }
            newSelectedCategory = new KeyValuePair<string, string>("", "");
        }

        private void processTransactionPerMonth(string monthYear, string categoryId, decimal tranAmount, string accountName)
        {
            var categoryMonthsTable = Helper.getDBData("CategoryMonths", null,
                        new Dictionary<string, string>() { { "Month", monthYear },
                            { "CategoryId", categoryId } });

            if (categoryMonthsTable.Rows.Count == 0)
            {
                //we want to add any months that are not in the database yet
                var allCategories = Helper.getDBData("Categories", new List<string>() { "CategoryId" });

                var categoryMonthsTableNewMonth = Helper.getDBData("CategoryMonths", null,
                    new Dictionary<string, string>() { { "Month", monthYear } });


                foreach (DataRow categories in allCategories.Rows)
                {
                    bool found = false;
                    foreach (DataRow row in categoryMonthsTableNewMonth.Rows)
                    {
                        //if row got placed in the database somehow, dont add it again
                        if ((int)row["CategoryId"] == (int)categories["CategoryId"])
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        var values = new List<KeyValuePair<string, string>>();
                        values.Add(new KeyValuePair<string, string>("Month", monthYear));
                        values.Add(new KeyValuePair<string, string>("Budget", "0.00"));
                        values.Add(new KeyValuePair<string, string>("Activity", "0.00"));
                        values.Add(new KeyValuePair<string, string>("CategoryId", categories["CategoryId"].ToString()));
                        var sqlReturn = Helper.addDBRow("CategoryMonths", values,
                            new Dictionary<string, string>() { { "Month", monthYear }, { "CategoryId", categories["CategoryId"].ToString() } });
                        if (sqlReturn.Key == false)
                        {
                            MessageBox.Show(this, sqlReturn.Value, "Error", MessageBoxButton.OK);
                            return;
                        }
                    }
                }
                categoryMonthsTable = Helper.getDBData("CategoryMonths", null,
                    new Dictionary<string, string>() { { "Month", monthYear },
                        { "CategoryId", categoryId } });
            }

            Helper.updateDBValue("CategoryMonths", new KeyValuePair<string,
            object>("Activity", (Convert.ToDecimal(categoryMonthsTable.Rows[0]["Activity"]) + tranAmount)),
            new Dictionary<string, string>() { { "CategoryId", categoryId }, { "Month", monthYear } });
        }

        private void ComboBox_DropDownClosed(object sender, EventArgs e)
        {
            var combo = (ComboBox)sender;
            var newCategory = Helper.getDBData("Categories", new List<string>() { "CategoryId" },
                new Dictionary<string, string>() { { "Name", combo.SelectedValue.ToString() } });

            newSelectedCategory = new KeyValuePair<string, string>(newCategory.Rows[0]["CategoryId"].ToString(), combo.SelectedValue.ToString());
        }

        private void DataGridCell_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataRowView currentRow = (DataRowView)((TextBox)e.Source).DataContext;

            //if ((string)currentRow["CategoryId"] != "")
            //{
                //if (!_pressedEnter)
                //{
                    int i = transactionGrid.Items.IndexOf(currentRow);
                    displayDT.Rows[i]["CategoryId"] = "CardPayment";
                //}
                //selectedRowGoalText.Visibility = Visibility.Hidden;
                //selectedRowGoalValue.Text = "";
                //_beforeBudgetEdit = "";
            //}
        }

    }
}
