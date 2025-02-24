using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Models {
	public class CreditAndCollectionSummary {

		public DateTime Start { get; }
		public DateTime End { get; }

		public Customer Customer { get; }

		public CreditAndCollectionSummary (Customer customer, DateTime start, DateTime end) {
			Start = start.Date;
			End = end.Date.AddDays(1).AddMilliseconds(-1);
			var debt = customer.Debt ();
			//var credits = customer.

			var query_orders = SalesOrder.Queryable.Where (x => (x.IsCompleted && !x.IsCancelled) &&
									x.Terms == PaymentTerms.NetD
								);


			//Orders = query_orders.OrderBy (w => w.Serial).ToList();
		}

		//public IList<SalesOrder> Credits { get { return Orders.Where (x => x.Terms == PaymentTerms.NetD).ToList (); }}
		//public IList<SalesOrder> Immediates { get { return Orders.Where (x => x.Terms == PaymentTerms.Immediate).ToList (); } }

	}
}