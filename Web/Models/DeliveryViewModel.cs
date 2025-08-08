using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Models {
	public class CustomerDebtViewModel {
		public Customer Customer { get; set; }
		public IList<dynamic> SalesOrders { get; set; }
		public List<SalesOrderPayment> Payments { get; set; }
		public DateRange DateRange { get; set; }
		public bool OnlyCredits { get; set; }
		public bool OnlyDebts { get; set; }
		
	}
}