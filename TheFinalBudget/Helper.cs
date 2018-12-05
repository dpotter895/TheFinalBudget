using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TheFinalBudget.Windows;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Globalization;

namespace TheFinalBudget
{
    public class Helper
    {
        public static string getGlobalValue(string name)
        {
            using (SqlConnection toBeBudgetedData = new SqlConnection(ConfigurationManager.ConnectionStrings["TheFinalBudget"].ConnectionString))
            {
                toBeBudgetedData.Open();

                string query = "Select Value FROM GlobalValues WHERE Name='" + name + "'";

                SqlCommand cmd = new SqlCommand(query, toBeBudgetedData);

                string value = cmd.ExecuteScalar().ToString();

                toBeBudgetedData.Close();
                return value;
            }
        }

        public static DataTable GetCategoriesByMonth(string monthYear)
        {
            using (SqlConnection categoriesData = new SqlConnection(ConfigurationManager.ConnectionStrings["TheFinalBudget"].ConnectionString))
            {
                categoriesData.Open();

                // get CategoryGroups data
                string query = "Select * FROM CategoryGroups Order By GroupOrder ASC";

                SqlCommand cmd = new SqlCommand(query, categoriesData);

                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable categoryGroupsDT = new DataTable("CategoryGroups");
                adapter.Fill(categoryGroupsDT);

                DataTable displayDT = new DataTable("DisplayDT");
                displayDT.Columns.Add("BudgetStyle");
                displayDT.Columns.Add("ActivityStyle");
                displayDT.Columns.Add("AccumulatedStyle");
                displayDT.Columns.Add("CategoryId");
                displayDT.Columns.Add("Goal");
                displayDT.Columns.Add("Category");
                displayDT.Columns.Add("Budgeted");
                displayDT.Columns.Add("Activity");
                displayDT.Columns.Add("Accumulated");

                foreach (DataRow group in categoryGroupsDT.Rows)
                {
                    int groupCount = 0;
                    var groupId = group["GroupId"].ToString();                    

                    // get Categories data
                    query = "Select * FROM Categories WHERE GroupId=" + groupId + " Order By GroupOrder ASC";

                    cmd = new SqlCommand(query, categoriesData);

                    adapter = new SqlDataAdapter(cmd);
                    DataTable categoriesDT = new DataTable("Categories");
                    adapter.Fill(categoriesDT);
                    decimal groupBudgeted = 0;
                    decimal groupactivity = 0;
                    decimal groupAccumulated = 0;

                    foreach (DataRow category in categoriesDT.Rows)
                    {
                        var categoryId = category["CategoryId"].ToString();

                        // get CategoryMonths data
                        query = "Select * FROM CategoryMonths WHERE Month=" + monthYear + " AND CategoryId=" + categoryId;

                        cmd = new SqlCommand(query, categoriesData);

                        adapter = new SqlDataAdapter(cmd);
                        DataTable categoriesByMonthDT = new DataTable("CategoryMonths");
                        adapter.Fill(categoriesByMonthDT);

                        foreach (DataRow categoryMonth in categoriesByMonthDT.Rows)
                        {
                            var ss = category["Name"];


                            DataRow newRow = displayDT.NewRow();
                            string goalStyle = "";
                            string activityStyle = "";
                            string accumulatedStyle = "";
                            if (Convert.ToDecimal(categoryMonth["Budget"]) < Convert.ToDecimal(category["GoalAmount"]))
                            {
                                goalStyle = "NotMet";
                            }
                            else if (Convert.ToDecimal(categoryMonth["Budget"]) > Convert.ToDecimal(category["GoalAmount"]))
                            {
                                goalStyle = "Met";
                            }

                            if (Convert.ToDecimal(categoryMonth["Budget"]) < (Convert.ToDecimal(categoryMonth["Activity"]) * -1))
                            {
                                activityStyle = "Over";
                            }
                            else if (Convert.ToDecimal(categoryMonth["Budget"]) > (Convert.ToDecimal(categoryMonth["Activity"]) * -1))
                            {
                                activityStyle = "Under";
                            }

                            if ((Convert.ToDecimal(categoryMonth["Activity"]) * -1) <= Convert.ToDecimal(category["GoalAmount"]) &&
                                Convert.ToDecimal(category["RunningTotal"]) != 0)
                            {
                                accumulatedStyle = "UnderMonthGoal";
                            }
                            else if ((Convert.ToDecimal(categoryMonth["Activity"]) * -1) > Convert.ToDecimal(category["GoalAmount"]))
                            {
                                accumulatedStyle = "OverMonthGoal";
                            }

                            if (Convert.ToDecimal(category["RunningTotal"]) < 0)
                            {
                                accumulatedStyle = "Negative";
                            }

                            newRow["BudgetStyle"] = goalStyle;
                            newRow["ActivityStyle"] = activityStyle;
                            newRow["AccumulatedStyle"] = accumulatedStyle;
                            newRow["CategoryId"] = category["CategoryId"].ToString();
                            newRow["Goal"] = category["GoalAmount"].ToString();
                            newRow["Category"] = "    " + category["Name"].ToString();
                            newRow["Budgeted"] = categoryMonth["Budget"].ToString();
                            groupBudgeted += Convert.ToDecimal(newRow["Budgeted"]);
                            newRow["Activity"] = categoryMonth["Activity"].ToString();
                            groupactivity += Convert.ToDecimal(newRow["Activity"]);
                            newRow["Accumulated"] = category["RunningTotal"].ToString();
                            var t = category["RunningTotal"];
                            groupAccumulated += Convert.ToDecimal(newRow["Accumulated"]);
                            displayDT.Rows.Add(newRow);
                            groupCount++;
                        }
                    }
                    DataRow groupRow = displayDT.NewRow();
                    groupRow["BudgetStyle"] = "";
                    groupRow["ActivityStyle"] = "";
                    groupRow["AccumulatedStyle"] = "";
                    groupRow["CategoryId"] = "";
                    groupRow["Category"] = group["GroupName"].ToString();
                    groupRow["Budgeted"] = "$" + groupBudgeted.ToString("0.00");
                    groupRow["Activity"] = "$" + groupactivity.ToString("0.00");
                    groupRow["Accumulated"] = "$" + groupAccumulated.ToString("0.00");

                    if (groupCount == 0)
                    {
                        displayDT.Rows.Add(groupRow);
                    }
                    else
                    {
                        displayDT.Rows.InsertAt(groupRow, displayDT.Rows.Count - groupCount);
                    }
                }
                categoriesData.Close();
                return displayDT;
            }
        }

        public static DataTable GetAccounts()
        {
            using (SqlConnection accountsData = new SqlConnection(ConfigurationManager.ConnectionStrings["TheFinalBudget"].ConnectionString))
            {
                accountsData.Open();

                // get CategoryGroups data
                string query = "Select * FROM Accounts Order By IsCreditCard Asc";

                SqlCommand cmd = new SqlCommand(query, accountsData);

                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable AccountsDT = new DataTable("Accounts");
                adapter.Fill(AccountsDT);

                DataTable displayDT = new DataTable("DisplayDT");
                displayDT.Columns.Add("AccountId");
                displayDT.Columns.Add("AccountName");
                displayDT.Columns.Add("AccountTotal");
                int groupCount = 0;
                decimal groupAccountTotal = 0;
                bool firstCreditCard = true;

                foreach (DataRow account in AccountsDT.Rows)
                {
                    if ((bool)account["IsCreditCard"])
                    {
                        if (firstCreditCard)
                        {
                            DataRow groupAccountRow = displayDT.NewRow();
                            groupAccountRow["AccountId"] = "";
                            groupAccountRow["AccountName"] = "CASH ACCOUNTS";
                            groupAccountRow["AccountTotal"] = "$" + groupAccountTotal.ToString("0.00");
                            displayDT.Rows.InsertAt(groupAccountRow, 0);
                            firstCreditCard = false;
                            groupCount = 0;
                            groupAccountTotal = 0;
                        }
                        

                    }

                    DataRow newRow = displayDT.NewRow();
                    newRow["AccountId"] = account["AccountId"].ToString();
                    newRow["AccountName"] = account["AccountName"].ToString();
                    newRow["AccountTotal"] = "$" + account["AccountTotal"].ToString();
                    displayDT.Rows.Add(newRow);
                    groupCount++;
                    groupAccountTotal += Convert.ToDecimal(account["AccountTotal"]);
                }

                DataRow groupCreditCardRow = displayDT.NewRow();
                groupCreditCardRow["AccountId"] = "";
                groupCreditCardRow["AccountName"] = "CREDIT CARDS";
                groupCreditCardRow["AccountTotal"] = "$" + groupAccountTotal.ToString("0.00");

                if (groupCount == 0)
                {
                    displayDT.Rows.Add(groupCreditCardRow);
                }
                else
                {
                    displayDT.Rows.InsertAt(groupCreditCardRow, displayDT.Rows.Count - groupCount);
                }
                accountsData.Close();
                return displayDT;
            }
        }

        public static DataTable getDBData(string table, List<string> columns = null, Dictionary<string, string> whereStatements = null, string orderBy = null)
        {
            using (SqlConnection categoriesData = new SqlConnection(ConfigurationManager.ConnectionStrings["TheFinalBudget"].ConnectionString))
            {
                categoriesData.Open();

                string query = "SELECT ";
                if (columns == null)
                {
                    query += "*";
                }
                else
                {
                    foreach (var column in columns)
                    {
                        query += column + ", ";
                    }
                    query = query.Remove(query.Length - 2);
                }
                query += " FROM " + table;

                if(whereStatements != null)
                {
                    query += " WHERE ";
                    foreach (var pair in whereStatements)
                    {
                        query += pair.Key + "=@" + pair.Key + " AND ";
                    }
                    query = query.Remove(query.Length - 5);
                }

                if(orderBy != null)
                {
                    query += " ORDER BY " + orderBy;
                }

                SqlCommand cmd = new SqlCommand(query, categoriesData);
                if (whereStatements != null)
                {
                    foreach (var pair in whereStatements)
                    {
                        if (pair.Value == null)
                        {
                            cmd.Parameters.AddWithValue("@" + pair.Key, DBNull.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@" + pair.Key, pair.Value);
                        }
                    }
                }
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable categoriesDT = new DataTable("Categories");
                adapter.Fill(categoriesDT);

                categoriesData.Close();
                return categoriesDT;
            }
        }

        public static bool updateDBValue(string table, KeyValuePair<string, object> updatedCell, Dictionary<string, string> whereStatements)
        {
            using (SqlConnection budgetDB = new SqlConnection(ConfigurationManager.ConnectionStrings["TheFinalBudget"].ConnectionString))
            {
                budgetDB.Open();

                string query = "UPDATE " + table + " SET " + updatedCell.Key + " = @updateValue WHERE ";
                foreach (var pair in whereStatements)
                {
                    query += pair.Key + "=@" + pair.Key + " AND ";
                }
                query = query.Remove(query.Length - 5);

                SqlCommand cmd = new SqlCommand(query, budgetDB);
                cmd.Parameters.AddWithValue("@updateValue", updatedCell.Value);
                foreach (var pair in whereStatements)
                {
                    if (pair.Value == null)
                    {
                        cmd.Parameters.AddWithValue("@" + pair.Key, DBNull.Value);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@" + pair.Key, pair.Value);
                    }
                }

                try
                {
                    cmd.ExecuteNonQuery();                   
                }
                catch(Exception e)
                {
                    budgetDB.Close();
                    return false;
                }
                budgetDB.Close();
                return true;
            }
        }

        public static KeyValuePair<bool, string> addDBRow(string table, List<KeyValuePair<string, string>> columnAndValues,
            Dictionary<string, string> whereStatements = null, bool skipValidation = false)
        {
            if (!skipValidation)
            {
                var ifExists = getDBData(table, null, whereStatements);

                if (ifExists.Rows.Count > 0)
                {
                    return new KeyValuePair<bool, string>(false, "Given Value already exists");
                }
            }

            using (SqlConnection budgetDB = new SqlConnection(ConfigurationManager.ConnectionStrings["TheFinalBudget"].ConnectionString))
            {
                budgetDB.Open();

                string query = "INSERT INTO " + table + " VALUES (";
                foreach (var pair in columnAndValues)
                {
                    query += "@" + pair.Key + ", ";
                }
                query = query.Remove(query.Length - 2);
                query += ")";

                SqlCommand cmd = new SqlCommand(query, budgetDB);
                foreach(var pair in columnAndValues)
                {
                    if (pair.Value == null)
                    {
                        cmd.Parameters.AddWithValue("@" + pair.Key, DBNull.Value);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@" + pair.Key, pair.Value);
                    }
                }

                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    budgetDB.Close();
                    return new KeyValuePair<bool, string>(false, "Error occured attempting to add values.");
                }
                budgetDB.Close();
                return new KeyValuePair<bool, string>(true, "");
            }
        }

        public static KeyValuePair<bool, string> AddCategory(string categoryName, string categoryGroupName, string currentMonthYear)
        {
            var groupIdTable = Helper.getDBData("CategoryGroups", new List<string>() { "GroupId" },
                        new Dictionary<string, string>() { { "GroupName", Convert.ToString(categoryGroupName) } });
            string groupId = groupIdTable.Rows[0]["GroupId"].ToString();

            var maxOrderNumberTable = Helper.getDBData("Categories", new List<string>() { "MAX(GroupOrder)" },
                    new Dictionary<string, string>() { { "GroupId", groupId } });
            int latestNumber = 0;
            foreach (DataRow number in maxOrderNumberTable.Rows)
            {
                if (number[0].GetType() != typeof(DBNull))
                {
                    latestNumber = (int)number[0];
                }
            }
            latestNumber++;

            var values = new List<KeyValuePair<string, string>>();
            values.Add(new KeyValuePair<string, string>("Name", categoryName));
            values.Add(new KeyValuePair<string, string>("RunningTotal", "0.00"));
            values.Add(new KeyValuePair<string, string>("GoalAmount", "0.00"));
            values.Add(new KeyValuePair<string, string>("CreditCardName", null));
            values.Add(new KeyValuePair<string, string>("GroupId", groupId));
            values.Add(new KeyValuePair<string, string>("GroupOrder", latestNumber.ToString()));
            var sqlReturn = Helper.addDBRow("Categories", values,
                new Dictionary<string, string>() { { "Name", categoryName } });
            if (sqlReturn.Key == false)
            {
                return sqlReturn;
            }

            var categoryIddTable = Helper.getDBData("Categories", new List<string>() { "CategoryId" },
                new Dictionary<string, string>() { { "Name", categoryName } });
            string categoryId = categoryIddTable.Rows[0]["CategoryId"].ToString();

            values = new List<KeyValuePair<string, string>>();
            values.Add(new KeyValuePair<string, string>("Month", currentMonthYear));
            values.Add(new KeyValuePair<string, string>("Budget", "0.00"));
            values.Add(new KeyValuePair<string, string>("Activity", "0.00"));
            values.Add(new KeyValuePair<string, string>("CategoryId", categoryId));
            sqlReturn = Helper.addDBRow("CategoryMonths", values,
                new Dictionary<string, string>() { { "Month", currentMonthYear }, { "CategoryId", categoryId } });
            return sqlReturn;
        }

        public static Bitmap CaptureAllCategories(bool firstImage)
        {
            var proc = Process.GetProcessesByName("TheFinalBudget")[0];
            var rect = new User32.Rect();
            User32.GetWindowRect(proc.MainWindowHandle, ref rect);

            int width = rect.right - rect.left;
            int height = rect.bottom - rect.top;

            using (var bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (Graphics graphics = Graphics.FromImage(bmp))
                {
                    graphics.CopyFromScreen(rect.left, rect.top, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);

                    System.Drawing.Rectangle cropSection = new System.Drawing.Rectangle(5, 30, 1100, 717);
                    if (!firstImage)
                    {
                        cropSection = new System.Drawing.Rectangle(5, 40, 615, 650);
                    }
                    return CropImage(bmp, cropSection);
                }
            }
        }

        private class User32
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct Rect
            {
                public int left;
                public int top;
                public int right;
                public int bottom;
            }

            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);
        }

        static Bitmap CropImage(Bitmap originalImage, System.Drawing.Rectangle sourceRectangle,
            System.Drawing.Rectangle? destinationRectangle = null)
        {
            if (destinationRectangle == null)
            {
                destinationRectangle = new System.Drawing.Rectangle(System.Drawing.Point.Empty, sourceRectangle.Size);
            }

            var croppedImage = new Bitmap(destinationRectangle.Value.Width,
                destinationRectangle.Value.Height);
            using (var graphics = Graphics.FromImage(croppedImage))
            {
                graphics.DrawImage(originalImage, destinationRectangle.Value,
                    sourceRectangle, GraphicsUnit.Pixel);
            }
            return croppedImage;
        }
    }
}
