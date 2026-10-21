//
// PaymentCorrection.cs
//
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
//
// Copyright (C) 2026 Eddy Zavaleta, Mictlanix, and contributors.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using NHibernate;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mictlanix.BE.Web.Services {
	// What a payment correction would do to the orders it touches, worked out
	// without writing anything (mictlanix/mbe#66 §6.2, §6.3, §6.7): the balance each
	// order would be left with, the CFDI that would refuse it, and the follow-ups to
	// record. The confirmation shows it; the correction itself repeats it under lock.
	public sealed class PaymentCorrection {
		// Accounts Receivable's test for a paid order.
		public const decimal PaidTolerance = 0.01m;

		const string TimeFormat = "yyyy-MM-dd HH:mm";

		public IList<OrderChange> Orders { get; private set; }
		public IList<string> Blocks { get; private set; }

		// Follow-ups (§6.7), null when there is nothing to report.
		public string Invoiced { get; private set; }
		public string Delivered { get; private set; }
		public string Commission { get; private set; }

		public bool IsBlocked {
			get { return Blocks.Count > 0; }
		}

		public IEnumerable<OrderChange> Unpaid {
			get { return Orders.Where (x => x.Unpays); }
		}

		// changes: each allocation the correction touches, with how much its amount
		// moves -- negative when money comes off the order. An allocation's change
		// never counts toward the order's balance, so it is left out.
		//
		// The balance after is worked out from the current one rather than by
		// changing the data: SalesOrder.Balance re-queries refunds and credit notes on
		// every read.
		public static PaymentCorrection Check (IEnumerable<KeyValuePair<SalesOrder, decimal>> changes)
		{
			var item = new PaymentCorrection {
				Orders = new List<OrderChange> (),
				Blocks = new List<string> ()
			};
			var invoiced = new List<string> ();

			foreach (var group in changes.GroupBy (x => x.Key.Id)) {
				var order = group.First ().Key;
				var balance = order.Balance;
				var change = new OrderChange (order, balance, balance - group.Sum (x => x.Value));

				item.Orders.Add (change);

				foreach (var invoice in Invoices (order)) {
					var receipts = invoice.Terms == PaymentTerms.NetD ? Receipts (invoice) : new List<FiscalDocument> ();

					if (change.Unpays && invoice.Terms == PaymentTerms.Immediate) {
						item.Blocks.Add (string.Format (Resources.PaymentCorrectionInvoiceActive, order.Id, Serial (invoice)));
					} else if (change.Unpays && receipts.Any ()) {
						item.Blocks.Add (string.Format (Resources.PaymentCorrectionReceiptsActive, order.Id, Serial (invoice),
									       string.Join (", ", receipts.Select (Serial))));
					} else {
						invoiced.Add (receipts.Any ()
							      ? string.Format ("{0}: {1} ({2})", order.Id, Serial (invoice), string.Join (", ", receipts.Select (Serial)))
							      : string.Format ("{0}: {1}", order.Id, Serial (invoice)));
					}
				}
			}

			if (item.IsBlocked) {
				return item;
			}

			item.Invoiced = Join (invoiced);
			item.Delivered = Join (item.Unpaid.Select (x => Deliveries (x.Order)));
			item.Commission = Join (item.Unpaid.Select (x => string.Format ("{0}: {1} {2}", x.Order.Id,
										      x.Order.ModificationTime.ToString (TimeFormat, CultureInfo.InvariantCulture),
										      x.Order.SalesPerson == null ? string.Empty : x.Order.SalesPerson.Name).Trim ()));

			return item;
		}

		// Whether a correction un-paid this order (§6.9), from the correction records.
		// Ids are compared exactly: a LIKE on 123 would match 1234.
		public static bool WasUnpaid (int order)
		{
			var id = order.ToString (CultureInfo.InvariantCulture);
			// The LIKE only narrows the rows; the exact comparison is below.
			var records = Incidence.Queryable
				.Where (x => x.SourceType == SourceType.CustomerPayment && x.PreviousState.Contains ("\"Unpaid\"") &&
					x.PreviousState.Contains (id))
				.Select (x => x.PreviousState)
				.ToList ();

			foreach (var record in records) {
				JObject json;

				try {
					json = JObject.Parse (record);
				} catch (JsonReaderException) {
					continue;
				}

				if (json ["Operation"] != null && ((string) json ["Unpaid"] ?? string.Empty).Split (',').Any (x => x.Trim () == id)) {
					return true;
				}
			}

			return false;
		}

		// Loads a row and holds it until the surrounding TransactionScope ends, so two
		// corrections of the same payment run one after the other. Re-reads it from the
		// database even when the session already holds it. MySQL: SELECT ... FOR UPDATE.
		public static T Lock<T> (int id) where T : class
		{
			var holder = ActiveRecordMediator.GetSessionFactoryHolder ();
			var session = holder.CreateSession (typeof (T));

			try {
				var item = session.Get<T> (id);

				if (item == null) {
					return null;
				}

				// Gone by the time the lock was granted: deleted by the transaction it
				// waited on. NHibernate has already dropped it from the session.
				try {
					session.Refresh (item, LockMode.Upgrade);
				} catch (UnresolvableObjectException) {
					return null;
				}

				return item;
			} finally {
				holder.ReleaseSession (session);
			}
		}

		// Each touched order whose paid state the correction changes (§6.4). An order
		// that stays paid keeps its modification time: the commissions report pays
		// orders by paid = 1 and modification_time in the period, so moving it would
		// pay its commission again. Delivery mode is left alone.
		public void Apply (Employee updater, DateTime now)
		{
			foreach (var item in Orders.Where (x => x.Order.IsPaid != x.IsPaid)) {
				var order = item.Order;

				order.IsPaid = item.IsPaid;
				order.BalanceZeroedTime = item.IsPaid ? now : (DateTime?) null;
				order.Updater = updater;
				order.ModificationTime = now;
				order.Update ();
			}
		}

		// As ModelHelpers.InvoiceSerials finds them, invoices only.
		static IList<FiscalDocument> Invoices (SalesOrder order)
		{
			var query = from x in FiscalDocument.Queryable
				    from y in x.Details
				    where x.Type == FiscalDocumentType.Invoice && x.IsCompleted && !x.IsCancelled &&
					  y.OrderDetail.SalesOrder.Id == order.Id
				    select x;

			return query.ToList ().Distinct ().ToList ();
		}

		static IList<FiscalDocument> Receipts (FiscalDocument invoice)
		{
			return FiscalDocumentRelation.Queryable
				.Where (x => x.Relation.Id == invoice.Id && x.Document.Type == FiscalDocumentType.PaymentReceipt &&
					x.Document.IsCompleted && !x.Document.IsCancelled)
				.Select (x => x.Document)
				.ToList ().Distinct ().ToList ();
		}

		// As PaymentsController.Cancel finds them.
		static string Deliveries (SalesOrder order)
		{
			var ids = DeliveryOrderDetail.Queryable
				.Where (x => x.DeliveryOrder.IsCompleted && !x.DeliveryOrder.IsCancelled &&
					x.OrderDetail.SalesOrder.Id == order.Id)
				.Select (x => x.DeliveryOrder.Id)
				.ToList ().Distinct ().ToList ();

			return ids.Any () ? string.Format ("{0}: {1}", order.Id, string.Join (", ", ids)) : null;
		}

		static string Serial (FiscalDocument document)
		{
			return string.Format ("{0}{1:D6}", document.Batch, document.Serial);
		}

		static string Join (IEnumerable<string> items)
		{
			var list = items.Where (x => !string.IsNullOrEmpty (x)).ToList ();
			return list.Any () ? string.Join ("; ", list) : null;
		}

		public sealed class OrderChange {
			public SalesOrder Order { get; private set; }
			public decimal BalanceBefore { get; private set; }
			public decimal BalanceAfter { get; private set; }

			public OrderChange (SalesOrder order, decimal before, decimal after)
			{
				Order = order;
				BalanceBefore = before;
				BalanceAfter = after;
			}

			public bool IsPaid {
				get { return BalanceAfter <= PaidTolerance; }
			}

			public bool Unpays {
				get { return Order.IsPaid && !IsPaid; }
			}
		}
	}
}
