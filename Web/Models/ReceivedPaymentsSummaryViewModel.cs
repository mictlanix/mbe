using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Models {
	public class ReceivedPaymentsSummaryViewModel {
		public Store Store { get; set; }
		public IList<SalesOrder> SalesOrders { get; set; }
		public IList<CustomerPayment> CustomerPayments { get; set; }
		public IList<ExpenseVoucher> Expenses { get; set; }
		public IList<CashSession> CashSession { get; set; }
	}
}