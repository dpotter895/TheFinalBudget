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

namespace TheFinalBudget.Windows
{
    /// <summary>
    /// Interaction logic for AddToGridWindow.xaml
    /// </summary>
    public partial class AddToGridWindow : Window
    {
        private string _currentMonthYear;
        public AddToGridWindow(string currentMonthYear)
        {
            InitializeComponent();
            btnAddToGrid.Visibility = Visibility.Hidden;

            _currentMonthYear = currentMonthYear;

            Application.Current.MainWindow = this;
            Application.Current.MainWindow.Height = 70;
        }

        private void btnAddToGrid_Click(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrEmpty(categoryNameText.Text))
            {
                MessageBox.Show(this, "Must provide a valid name.", "Error", MessageBoxButton.OK);
            }
            else
            {
                if (groupRadio.IsChecked == true)
                {
                    var maxOrderNumberTable = Helper.getDBData("CategoryGroups", new List<string>(){ "MAX(GroupOrder)" });
                    int latestNumber = 0;
                    foreach(DataRow number in maxOrderNumberTable.Rows)
                    {
                        if (number[0].GetType() != typeof(DBNull))
                        {
                            latestNumber = (int)number[0];
                        }
                    }
                    latestNumber++;

                    var values = new List<KeyValuePair<string, string>>();
                    values.Add(new KeyValuePair<string, string>("GroupName", categoryNameText.Text));
                    values.Add(new KeyValuePair<string, string>("GroupOrder", latestNumber.ToString()));
                    var sqlReturn = Helper.addDBRow("CategoryGroups", values,
                        new Dictionary<string, string>() { { "GroupName", categoryNameText.Text } });
                    if (sqlReturn.Key == false)
                    {
                        MessageBox.Show(this, sqlReturn.Value, "Error", MessageBoxButton.OK);
                        return;
                    }

                    MessageBox.Show(this, "Successfully added the group " + categoryNameText.Text + " .", "Success", MessageBoxButton.OK);
                }
                else if (categoryRadio.IsChecked == true)
                {
                    var returnValue = Helper.AddCategory(categoryNameText.Text, Convert.ToString(categoryGroupComboBox.SelectedValue), _currentMonthYear);

                    if (returnValue.Key == false)
                    {
                        MessageBox.Show(this, returnValue.Value, "Error", MessageBoxButton.OK);
                        return;
                    }
                    else
                    {
                        MessageBox.Show(this, "Successfully added the category " + categoryNameText.Text + " .", "Success", MessageBoxButton.OK);
                    }
                }
                else
                {
                    MessageBox.Show(this, "Must select either Category Group button or Category button.", "Error", MessageBoxButton.OK);
                }
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

        private void group_Checked(object sender, RoutedEventArgs e)
        {
            btnAddToGrid.Visibility = Visibility.Visible;
            categoryGroupTextBlock.Visibility = Visibility.Hidden;
            categoryGroupComboBox.Visibility = Visibility.Hidden;
            nameTextBlock.Text = "Category Group Name:";
            categoryNameText.Text = "";
            Application.Current.MainWindow = this;
            Application.Current.MainWindow.Height = 115;
        }

        private void category_Checked(object sender, RoutedEventArgs e)
        {
            btnAddToGrid.Visibility = Visibility.Visible;
            categoryGroupTextBlock.Visibility = Visibility.Visible;
            categoryGroupComboBox.Visibility = Visibility.Visible;
            categoryGroupComboBox.Items.Clear();
            nameTextBlock.Text = "Category Name:";
            categoryNameText.Text = "";

            var AllGroups = Helper.getDBData("CategoryGroups", new List<string>(){ "GroupName" });

            foreach(DataRow row in AllGroups.Rows)
            {
                categoryGroupComboBox.Items.Add(row["GroupName"]);
            }
            categoryGroupComboBox.SelectedIndex = 0;

            Application.Current.MainWindow = this;
            Application.Current.MainWindow.Height = 145;
        }
    }
}
