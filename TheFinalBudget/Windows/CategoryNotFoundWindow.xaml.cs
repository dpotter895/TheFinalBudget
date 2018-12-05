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
using System.Data;
using TheFinalBudget.Model;
using Newtonsoft.Json;

namespace TheFinalBudget.Windows
{
    /// <summary>
    /// Interaction logic for MessageWindow.xaml
    /// </summary>
    public partial class CategoryNotFoundWindow : Window
    {
        private string _currentMonthYear;
        private Transaction _transaction;
        public string returnedCategoryId = null;

        public CategoryNotFoundWindow(string currentMonthYear, Transaction transaction)
        {
            InitializeComponent();
            categoryTextBlock.Visibility = Visibility.Hidden;
            categoryComboBox.Visibility = Visibility.Hidden;
            categoryGroupTextBlock.Visibility = Visibility.Hidden;
            categoryGroupComboBox.Visibility = Visibility.Hidden;
            btnAdd.Visibility = Visibility.Hidden;

            _transaction = transaction;
            var AllGroups = Helper.getDBData("CategoryGroups", new List<string>() { "GroupName" });
            if(AllGroups.Rows.Count == 0)
            {
                MessageBox.Show(this, "Must create a Category Group before adding a new category.", "Error", MessageBoxButton.OK);
                this.Close();
                return;
            }
            foreach (DataRow row in AllGroups.Rows)
            {
                categoryGroupComboBox.Items.Add(row["GroupName"]);
            }

            var AllCategories = Helper.getDBData("Categories", new List<string>() { "Name" });
            if (AllCategories.Rows.Count == 0)
            {
                btnMapToCategory.Visibility = Visibility.Hidden;
            }
            foreach (DataRow row in AllCategories.Rows)
            {
                categoryComboBox.Items.Add(row["Name"]);
            }

            categoryComboBox.SelectedIndex = -1;
            categoryGroupComboBox.SelectedIndex = -1;

            _currentMonthYear = currentMonthYear;
            categoryInQuestion.Text = transaction.Category;

            transactionDetails.Text = JsonConvert.SerializeObject(transaction);

            Application.Current.MainWindow = this;
            Application.Current.MainWindow.Width = 330;
        }

        private void btnCreateNew_Click(object sender, RoutedEventArgs e)
        {
            categoryTextBlock.Visibility = Visibility.Hidden;
            categoryComboBox.Visibility = Visibility.Hidden;
            categoryGroupTextBlock.Visibility = Visibility.Visible;
            categoryGroupComboBox.Visibility = Visibility.Visible;
            btnAdd.Visibility = Visibility.Visible;

            categoryComboBox.SelectedIndex = -1;
            categoryGroupComboBox.SelectedIndex = 0;

            Application.Current.MainWindow = this;
            Application.Current.MainWindow.Width = 500;
        }

        private void btnMapToCategory_Click(object sender, RoutedEventArgs e)
        {
            categoryTextBlock.Visibility = Visibility.Visible;
            categoryComboBox.Visibility = Visibility.Visible;
            categoryGroupTextBlock.Visibility = Visibility.Hidden;
            categoryGroupComboBox.Visibility = Visibility.Hidden;
            btnAdd.Visibility = Visibility.Visible;

            categoryGroupComboBox.SelectedIndex = -1;

            Application.Current.MainWindow = this;
            Application.Current.MainWindow.Width =500;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if(categoryComboBox.SelectedIndex != -1)
            {
                var categoryId = Helper.getDBData("Categories", new List<string>() { "CategoryId" },
                    new Dictionary<string, string>() { { "Name", Convert.ToString(categoryComboBox.SelectedValue) } });

                var values = new List<KeyValuePair<string, string>>();
                values.Add(new KeyValuePair<string, string>("AlternativeName", categoryInQuestion.Text));
                values.Add(new KeyValuePair<string, string>("CategoryId", categoryId.Rows[0]["CategoryId"].ToString()));
                var returnValue = Helper.addDBRow("AlternativeCategoryMap", values, 
                    new Dictionary<string, string>() { { "AlternativeName", categoryInQuestion.Text } });
                if (returnValue.Key == false)
                {
                    MessageBox.Show(this, returnValue.Value, "Error", MessageBoxButton.OK);
                    returnedCategoryId = null;
                }
                else
                {
                    MessageBox.Show(this, "Now all transaction with category name: \"" + categoryInQuestion.Text + "\" will now be associated with category: \""
                        + Convert.ToString(categoryComboBox.SelectedValue) + "\".", "Success", MessageBoxButton.OK);
                    returnedCategoryId = categoryId.Rows[0]["CategoryId"].ToString();
                }
            }
            else if (categoryGroupComboBox.SelectedIndex != -1)
            {
                var returnValue = Helper.AddCategory(categoryInQuestion.Text, Convert.ToString(categoryGroupComboBox.SelectedValue), _currentMonthYear);

                if (returnValue.Key == false)
                {
                    MessageBox.Show(this, returnValue.Value, "Error", MessageBoxButton.OK);
                    returnedCategoryId = null;
                }
                else
                {
                    MessageBox.Show(this, "Successfully added the category " + categoryInQuestion.Text + " .", "Success", MessageBoxButton.OK);
                    var categoryId = Helper.getDBData("Categories", new List<string>() { "CategoryId" },
                        new Dictionary<string, string>() { { "Name", categoryInQuestion.Text } });
                    returnedCategoryId = categoryId.Rows[0]["CategoryId"].ToString();
                }
            }
            this.Close();
        }

        private void returnBtn_Click(object sender, RoutedEventArgs e)
        {
            returnedCategoryId = "-9";

            this.Close();
        }
    }
}
