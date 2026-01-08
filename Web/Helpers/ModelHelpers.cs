// 
// ModelHelpers.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2011-2017 Eddy Zavaleta, Mictlanix, and contributors.
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
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Mvc;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Helpers {
	public static class ModelHelpers {
		public static string GetDisplayName (this Enum member)
		{
			string display_name = Enum.GetName (member.GetType (), member);

			var prop_info = member.GetType ().GetField (display_name);
			var attrs = prop_info.GetCustomAttributes (typeof (DisplayAttribute), false);

			if (attrs.Length > 0)
				display_name = ((DisplayAttribute) attrs [0]).GetName ();

			return display_name;
		}

		public static string FormattedValueFor<TModel, TResult> (this TModel entity, Expression<Func<TModel, TResult>> expression)
		{
			var view_data = new ViewDataDictionary<TModel> (entity);
			var metadata = ModelMetadata.FromLambdaExpression (expression, view_data);
			var format_string = metadata.DisplayFormatString ?? "{0}";

			if (format_string != null)
				return string.Format (format_string, metadata.Model);

			return string.Empty;
		}

		public static decimal Debt (this Customer entity)
		{
			IQueryable<decimal> query;

			query = from x in SalesOrder.Queryable
				from y in x.Payments
				where x.Terms == PaymentTerms.NetD &&
				      x.IsCompleted && !x.IsCancelled && !x.IsPaid &&
				      x.Customer.Id == entity.Id
				select y.Amount * x.ExchangeRate;
			var paid = query.Count () > 0 ? query.ToList ().Sum () : 0;

			query = from x in SalesOrder.Queryable
				from y in x.Details
				where x.Terms == PaymentTerms.NetD &&
				      x.IsCompleted && !x.IsCancelled && !x.IsPaid &&
				      x.Customer.Id == entity.Id
				select (y.Quantity - (CustomerRefundDetail.Queryable.Where (
						z => z.SalesOrderDetail == y && !z.Refund.IsCancelled && z.Refund.IsCompleted)
					.Sum (w => (decimal?) w.Quantity) ?? 0)
					) * y.Price * y.ExchangeRate * (1 - y.DiscountRate) * (y.IsTaxIncluded || y.TaxRate <= 0m ? 1m : (1m + y.TaxRate));
			var bought = query.Count () > 0 ? query.ToList ().Sum () : 0;

			return bought - paid;
		}

		public static decimal PrepaymentsBalance (this Customer entity, SalesOrder salesOrder)
		{
			var query = (from x in CustomerPayment.Queryable
				     where x.PaymentType == PaymentType.PaymentInAdvance
				     && x.Customer == entity && !x.Allocations.Any(y => y.SalesOrder == salesOrder)
				     select x).ToArray ();

			return query.Sum(x => (decimal?)x.Amount - x.Allocated)??0;
		}

		public static decimal RefundBalance (this Customer entity, SalesOrder salesOrder) {

			if (entity.Id == WebConfig.DefaultCustomer) {
				return 0;
			}

			var credits = entity.GetCreditNotes()
				.Where(x => !x.CustomerPayment.Allocations.Any(y => y.SalesOrder == salesOrder))
				.Select(x => x.CustomerPayment).ToArray();
			return credits.Sum (x => (decimal?)x.Balance)??0;

		}

		public static List<CreditNote> GetCreditNotes (this Customer customer) {
			if(customer.Id == WebConfig.DefaultCustomer) return new List<CreditNote> ();

			return CreditNote.Queryable.Where (x =>
				x.Customer == customer
				//&& !x.IsRefundedToCustomer
				&& x.CashSession == null)
				.ToList ();
		}

		public static bool HasExpiredCredits (this Customer customer) {
			var expired = SalesOrder.Queryable.Where (x => x.Terms == PaymentTerms.NetD && !x.IsPaid
				&& !x.IsCancelled && x.IsCompleted
				&& x.Customer == customer && x.DueDate.Date < DateTime.Today).ToArray();
			return expired.Any (x => x.Balance > 0.01m);
		}

		public static bool IsOverCreditLimit (this SalesOrder entity)
		{
			return (entity.Customer.Debt () + entity.Balance) > entity.Customer.CreditLimit;
		}
		public static bool IsOverCreditLimit (this SalesQuote entity)
		{
			return (entity.Customer.Debt () + entity.TotalEx) > entity.Customer.CreditLimit;
		}

		public static decimal AmountOverCreditLimit (this SalesOrder entity)
		{
			return entity.Customer.Debt () + entity.TotalEx - entity.Customer.CreditLimit;
		}

		public static string InvoiceSerials (this SalesOrder entity)
		{
			var query = from x in FiscalDocument.Queryable
				    from y in x.Details
				    where x.IsCompleted && !x.IsCancelled &&
					    y.OrderDetail.SalesOrder.Id == entity.Id
				    select new { x.Batch, x.Serial };

			return string.Join (",", query.ToList ().Distinct ().Select (x => string.Format ("{0}{1:D6}", x.Batch, x.Serial)));
		}


		public static decimal AmountOverCreditLimit (this SalesQuote entity)
		{
			return entity.Customer.Debt () + entity.TotalEx - entity.Customer.CreditLimit;
		}

		public static string InvoiceSerials (this SalesQuote entity)
		{
			var query = from x in FiscalDocument.Queryable
				    from y in x.Details
				    where x.IsCompleted && !x.IsCancelled &&
					y.OrderDetail.SalesOrder.Id == entity.Id
				    select new { x.Batch, x.Serial };

			return string.Join (",", query.ToList ().Distinct ().Select (x => string.Format ("{0}{1:D6}", x.Batch, x.Serial)));
		}

		public static string ValidataionUrl (this FiscalDocument item)
		{
			var data = string.Format (Resources.FiscalDocumentQRCode33FormatString,
						  item.Issuer.Id, item.Recipient, item.Total, item.StampId,
						  item.IssuerDigitalSeal?.Substring (item.IssuerDigitalSeal.Length - 8));

			return data;
		}

		public static decimal GetRefundableQuantity (this SalesOrderDetail detail) {
			var refunded = CustomerRefundDetail.Queryable.Where (x => !x.Refund.IsCancelled
					&& x.Refund.IsCompleted
					&& x.SalesOrderDetail == detail)
				.Sum (x => (decimal?) x.Quantity) ?? 0;

			return detail.Quantity- refunded;
		}

		public static decimal GetCancellableQuantity (this SalesOrderDetail detail) {
			var delivered = DeliveriesItineraryDetail.Queryable.Where (x => !x.DeliveriesItinerary.IsCancelled
					&& x.DeliveriesItinerary.IsCompleted
					&& x.DeliveryOrderDetail.OrderDetail == detail)
				.Sum (x => (decimal?) x.SentQuantity) ?? 0;
			var picked = DeliveryOrderDetail.Queryable.Where (x => !x.DeliveryOrder.IsCancelled
					&& x.DeliveryOrder.IsCompleted
					&& x.OrderDetail == detail
					&& x.DeliveryOrder.IsPickedUpInStore)
				.Sum (x => (decimal?) x.Quantity) ?? 0;
			return detail.Quantity - delivered - picked;
		}

		public static decimal GetDeliverableQuantity (this SalesOrderDetail detail) {
			var deliveries = DeliveryOrderDetail.Queryable.Where (x => !x.DeliveryOrder.IsCancelled
					&& x.DeliveryOrder.IsCompleted	
					&& x.OrderDetail == detail).Select(x => x.Quantity).ToList();

			var refunds = CustomerRefundDetail.Queryable.Where (x => !x.Refund.IsCancelled
				&& x.Refund.IsCompleted
				&& x.SalesOrderDetail == detail).Select(x => x.Quantity).ToList ();

			return detail.Quantity - (deliveries.Sum(x => (decimal?)x) ?? 0) - (refunds.Sum(x => (decimal?)x)??0);
		}
	}
}