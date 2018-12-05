using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace TheFinalBudget.Model
{
    public class CategoryList : List<string>
    {
        public CategoryList()
        {
            var categoryTable = Helper.getDBData("Categories", new List<string>() { "Name" });

            foreach(DataRow row in categoryTable.Rows)
            {
                this.Add((string)row["Name"]);
            }
        }
    }
}
