using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheFinalBudget.Model
{
    public class Transaction
    {
        public DateTime Date { get; }
        public string Producer { get; }
        public string Description { get; }
        public decimal Amount { get; set; }
        public bool IsCredit { get; }
        public string Category { get; }
        public string AccountName { get; }
        
        public Transaction(string date, string producer, string desc, string amount, string tranType, string category, string accountName)
        {
            var dateSplit = date.Split('/');
            Date = new DateTime(Convert.ToInt32(dateSplit[2]), Convert.ToInt32(dateSplit[0]), Convert.ToInt32(dateSplit[1]));

            Producer = producer;
            Description = desc;
            decimal decAmount;
            decimal.TryParse(amount, out decAmount);
            Amount = decAmount;
            if(tranType == "credit")
            {
                IsCredit = true;
            }
            else if(tranType == "debit")
            {
                IsCredit = false;
            }
            Category = category;
            AccountName = accountName;
        }
    }
}
