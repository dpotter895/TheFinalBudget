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
using System.Data.SqlClient;
using System.Configuration;
using System.Data;
using TheFinalBudget.Windows;
using System.Collections.ObjectModel;

namespace TheFinalBudget.Windows
{
    /// <summary>
    /// Interaction logic for CategoryEditWindow.xaml
    /// </summary>
    public partial class CategoryEditWindow : Window
    {
        ObservableCollection<CategoryAmount> categoryAmountList = new ObservableCollection<CategoryAmount>();
        private string _currentMonthYear;
        private int _categoryId;
        private string _name;
        private string _goal;

        public CategoryEditWindow(string groupName, string currentMonthYear)
        {
            InitializeComponent();

            _currentMonthYear = currentMonthYear;

            goalTextBox.Visibility = Visibility.Hidden;
            goalText.Visibility = Visibility.Hidden;
            totalText.Visibility = Visibility.Hidden;
            moveText.Visibility = Visibility.Hidden;
            moveMoneyTextBox.Visibility = Visibility.Hidden;
            toText.Visibility = Visibility.Hidden;
            totalAmountText.Visibility = Visibility.Hidden;
            categoryAmountsComboBox.Visibility = Visibility.Hidden;
            btnTransfer.Visibility = Visibility.Hidden;
            goalTextBox.Visibility = Visibility.Hidden;

            Application.Current.MainWindow = this;
            Application.Current.MainWindow.Height = 80;
        }

        public CategoryEditWindow(DataRowView currentRow, string currentMonthYear, decimal toBeBudgeted)
        {
            InitializeComponent();

            _currentMonthYear = currentMonthYear;

            _categoryId = Convert.ToInt32(currentRow.Row["CategoryId"]);
            _name = currentRow["Category"].ToString();
            CategoryNameTextBox.Text = _name;
            _goal = currentRow["Goal"].ToString();
            goalTextBox.Text = _goal;
            totalAmountText.Text = "$" + (Convert.ToDecimal(currentRow["Accumulated"]).ToString("0.00").Replace("$",""));

            var categoryAmountsDT = Helper.getDBData("Categories", new List<string>() { "Name", "RunningTotal" }, null, "GroupId ASC");
            foreach(DataRow amount in categoryAmountsDT.Rows)
            {
                if ((string)amount["Name"] != _name)
                {
                    categoryAmountList.Add(new CategoryAmount() { Name = amount["Name"].ToString(), RunningTotal = amount["RunningTotal"].ToString() });
                }
            }

            categoryAmountsComboBox.DataContext = categoryAmountList;
        }

        private class CategoryAmount
        {
            public string Name { get; set; }
            public string RunningTotal { get; set; }
        }

        private void btnTransfer_Click(object sender, RoutedEventArgs e)
        {
            bool isDecimal = decimal.TryParse(moveMoneyTextBox.Text.Replace("$", ""), out decimal moveAmount);
            if (isDecimal)
            {
                decimal total = Convert.ToDecimal(totalAmountText.Text.Replace("$", ""));

                string newTotal = (total - moveAmount).ToString("0.00");
                totalAmountText.Text = "$" + newTotal;
                Helper.updateDBValue("Categories", new KeyValuePair<string, object>("RunningTotal", newTotal), 
                    new Dictionary<string, string>() { { "CategoryId", _categoryId.ToString() } });

                CategoryAmount selectedCategory = (CategoryAmount)categoryAmountsComboBox.SelectedValue;

                decimal selectedTotal = Convert.ToDecimal(selectedCategory.RunningTotal);
                Helper.updateDBValue("Categories", new KeyValuePair<string, object>("RunningTotal", (selectedTotal + moveAmount).ToString("0.00")), 
                    new Dictionary<string, string>() { { "Name", selectedCategory.Name } });

                //refresh the list with the correct amounts
                categoryAmountList = new ObservableCollection<CategoryAmount>();

                var categoryAmountsDT = Helper.getDBData("Categories", new List<string>() { "Name", "RunningTotal" }, null, "GroupId ASC");
                foreach (DataRow amount in categoryAmountsDT.Rows)
                {
                    if ((string)amount["Name"] != _name)
                    {
                        categoryAmountList.Add(new CategoryAmount() { Name = amount["Name"].ToString(), RunningTotal = amount["RunningTotal"].ToString() });
                    }
                }

                categoryAmountsComboBox.DataContext = categoryAmountList;

                moveMoneyTextBox.Text = "";

                MessageBox.Show(this, "Transfer was successful.", "Successful Transfer", MessageBoxButton.OK);
            }
            else
            {
                MessageBox.Show(this, "Move amount must be a decimal.", "Move Amount Error", MessageBoxButton.OK);
            }
        }

        private void CategoryNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            updateName();
        }

        private void CategoryNameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                updateName();
            }
        }

        private void goalTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            updateGoal();
        }

        private void goalTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                updateGoal();
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow(_currentMonthYear);
            mainWindow.Owner = this;
            mainWindow.Show();
            mainWindow.Owner = null;
            this.Close();
        }

        private void updateName()
        {
            if (CategoryNameTextBox.Text != _name)
            {
                _name = CategoryNameTextBox.Text;
                if (Helper.updateDBValue("Categories", new KeyValuePair<string, object>("Name", _name), 
                    new Dictionary<string, string>() { { "CategoryId", _categoryId.ToString() } }))
                {
                    MessageBox.Show(this, "Category name update was successful.", "Successful Update", MessageBoxButton.OK);
                }
                else
                {
                    MessageBox.Show(this, "Category name was not updated.", "Error", MessageBoxButton.OK);
                }
            }
        }

        private void updateGoal()
        {
            if (goalTextBox.Text != _goal)
            {
                _goal = goalTextBox.Text;
                if (Helper.updateDBValue("Categories", new KeyValuePair<string, object>("GoalAmount", _goal), 
                    new Dictionary<string, string>() { { "CategoryId", _categoryId.ToString() } }))
                {
                    MessageBox.Show(this, "Goal update was successful.", "Successful Update", MessageBoxButton.OK);
                }
                else
                {
                    MessageBox.Show(this, "Goal was not updated.", "Error", MessageBoxButton.OK);
                }
            }
        }
    }
}
