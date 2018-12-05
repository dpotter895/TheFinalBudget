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
using TheFinalBudget.Model;

namespace TheFinalBudget.Windows
{
	/// <summary>
	/// Interaction logic for RefundOrPaymentWindow.xaml
	/// </summary>
	public partial class RefundOrPaymentWindow : Window
	{
		public string noCreditCardDepositType = null;

		public RefundOrPaymentWindow(Transaction transaction)
		{
			InitializeComponent();

			DateBox.Text = transaction.Date.ToShortDateString();
			ProducerBox.Text = transaction.Producer;
			AmountBox.Text = transaction.Amount.ToString("0.00");
			DescBox.Text = transaction.Description;
			string tranType;
			if (transaction.IsCredit)
			{
				tranType = "Credit";
			}
			else
			{
				tranType = "Debit";
			}
			TranTypeBox.Text = tranType;
			CategoryBox.Text = transaction.Category;
			AccountBox.Text = transaction.AccountName;
		}

		private void RefundBtn_Click(object sender, RoutedEventArgs e)
		{
			noCreditCardDepositType = "refund";
			this.Close();
		}

		private void PaymentBtn_Click(object sender, RoutedEventArgs e)
		{
			noCreditCardDepositType = "payment";
			this.Close();
		}
	}
}
