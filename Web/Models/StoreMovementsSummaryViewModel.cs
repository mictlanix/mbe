using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Models {
	public class StoreMovementsSummaryViewModel {
		public IEnumerable<dynamic> SalesOrder { get; set; }
		public IEnumerable<ExpenseVoucher> Expenses { get; set; }
		public IEnumerable<CustomerPayment> Payments { get; set; }
	}
}