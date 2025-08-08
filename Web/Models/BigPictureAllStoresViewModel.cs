using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Models {
	public class BigPictureStoreViewModel {
		public Store Store { get; set; }
		public List<SalesOrder> SalesOrders { get; set; }
		public List<CustomerPayment> CustomerPayments { get; set; }
		public List<ExpenseVoucher> Expenses { get; set; }
	}
}