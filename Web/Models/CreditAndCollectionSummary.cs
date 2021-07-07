using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Models {
	public class CreditAndCollectionSummary {

		public DateTime Start { get; }
		public DateTime End { get; }
		public Store Store { get; }
		public IList<ExpenseVoucher> Expenses { get; }
		public IList<SalesOrder> Orders { get; }


		public CreditAndCollectionSummary (Store store, DateTime start, DateTime end) {
			Start = start.Date;
			Store = store;
			End = end.Date.AddDays(1).AddMilliseconds(-1);
			var query_orders = SalesOrder.Queryable.Where (x => (x.IsCompleted && !x.IsCancelled) &&
									(
				  (x.Date > Start && x.Date < End && x.Terms == PaymentTerms.Immediate && x.IsPaid)
				|| (x.ModificationTime > Start && x.ModificationTime < End && x.Terms == PaymentTerms.NetD)
				|| (x.Payments.Any (y => y.Payment.Allocations.Any (z => z.Payment.ModificationTime > Start && z.Payment.ModificationTime < End))
				|| (x.Payments.Any (y => y.Payment.ModificationTime > Start && y.Payment.ModificationTime < End)))
									 )
								);
			var query_expenses = ExpenseVoucher.Queryable.Where (x => !x.IsCancelled && x.IsCompleted && x.Date > Start && x.Date < End);
			if(Store != null) {
				query_orders = query_orders.Where(x => x.Store == Store);
				query_expenses = query_expenses.Where(x => x.Store == Store);
			}

			Orders = query_orders.OrderBy (w => w.Serial).ToList();
			Expenses = query_expenses.ToList ();
		}

		public IList<SalesOrder> Credits { get { return Orders.Where (x => x.Terms == PaymentTerms.NetD).ToList (); }}
		public IList<SalesOrder> Immediates { get { return Orders.Where (x => x.Terms == PaymentTerms.Immediate).ToList (); } }
		public decimal GetSubTotal (PaymentMethod method) {
			var immediate = Orders.Sum (x => x.Payments.Where (y => y.Payment.Method == method && y.Payment.Store == Store && y.SalesOrder.Terms == PaymentTerms.Immediate).Sum (z => z.Payment.Amount));
			var netd = Orders.SelectMany (x => x.Payments.SelectMany (y => y.Payment.Allocations.Where (z => z.Payment.Method == method && z.Payment.Date > Start && z.Payment.Date < End && z.SalesOrder.Terms == PaymentTerms.NetD).Select (w => w.Payment))).Sum (a => a.Amount);
			return immediate + netd;
		}
	}
}