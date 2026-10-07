// 
// CashCut.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
//   Eduardo Nieto <enieto@mictlanix.com>
// 
// Copyright (C) 2011-2013 Eddy Zavaleta, Mictlanix, and contributors.
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
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Web.Mvc;
using System.Linq;
using System.Web.Security;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Models {
	public class CashCountReport {
		// A payment is classified by type *and* sign (mictlanix/mbe#60). The database
		// holds four conventions: type 0 (N/A) for sales and, negative, for refunds
		// before 2024; credit notes recorded negative (2025-04 to 2025-07) and
		// positive (since). A refund is a credit note or any negative payment, and is
		// reported as the amount paid out; any other positive payment is a sale.
		public static bool IsRefund (CustomerPayment payment)
		{
			return payment.PaymentType == PaymentType.CreditNote || payment.Amount < 0;
		}

		public static bool IsSale (CustomerPayment payment)
		{
			return !IsRefund (payment) && payment.Amount > 0;
		}

		[DisplayFormat (DataFormatString = "{0:000000}")]
		public int SessionId { get; set; }

		[Display (Name = "Cashier", ResourceType = typeof (Resources))]
		public Employee Cashier { get; set; }

		[Display (Name = "CashDrawer", ResourceType = typeof (Resources))]
		public CashDrawer CashDrawer { get; set; }

		[DataType (DataType.DateTime)]
		[Display (Name = "StartDate", ResourceType = typeof (Resources))]
		public DateTime Start { get; set; }

		[DataType (DataType.DateTime)]
		[Display (Name = "End", ResourceType = typeof (Resources))]
		public DateTime? End { get; set; }

		[DataType (DataType.Currency)]
		[Display (Name = "StartingCash", ResourceType = typeof (Resources))]
		public decimal StartingCash { get; set; }

		[DataType (DataType.Currency)]
		[Display (Name = "CashSales", ResourceType = typeof (Resources))]
		public decimal CashSales {
			get {
				return PaymentsReceivedInCash;
			}
		}

		[DataType (DataType.Currency)]
		[Display (Name = "CashSales", ResourceType = typeof (Resources))]
		public List<MoneyCount> PaymentsReceivedByMethod {
			get {
				var items = (from y in Payments
					    group y by y.Method into g
					    select new MoneyCount { Method = g.Key, Amount = g.Sum(x => x.Amount - x.Allocations.Sum (y => (decimal?) y.Change) ?? 0) }).ToList();

				return items;
			}
		}

		public decimal PaymentsReceivedInCash {
			get {
				return PaymentsReceivedByMethod.Where (x => x.Method == PaymentMethod.Cash).Sum (x => (decimal?) x.Amount) ?? 0;
			}
		}

		public decimal ExpensesInCash {
			get {
				return Expenses.Where (x => x.Method == PaymentMethod.Cash).Sum (x => (decimal?) x.Amount) ?? 0;
			}
		}

		public decimal RefundsInCash {
			get {
				return RefundsByMethod.Where (x => x.Method == PaymentMethod.Cash).Sum (x => (decimal?) x.Amount) ?? 0;
			}
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Expenses", ResourceType = typeof (Resources))]
		public List<MoneyCount> ExpensesByMethod {
			get {
				var types = new List<PaymentType> { PaymentType.Expense };
				var items = (from y in Expenses.Where (x => types.Contains (x.PaymentType))
					     group y by y.Method into g
					     select new MoneyCount { Method = g.Key, Amount = g.Sum (x => x.Amount - x.Allocations.Sum (y => (decimal?) y.Change) ?? 0) }).ToList ();

				return items;
			}
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Refunds", ResourceType = typeof (Resources))]
		public List<MoneyCount> RefundsByMethod {
			get {
				var items = (from y in Refunds
					     group y by y.Method into g
					     select new MoneyCount { Method = g.Key, Amount = g.Sum (x => (decimal?) Math.Abs (x.Amount)) ?? 0 }).ToList ();

				return items;
			}
		}

		[DataType (DataType.Currency)]
		[Display (Name = "CashInDrawer", ResourceType = typeof (Resources))]
		public decimal CashInDrawer {
			get {
				return StartingCash + PaymentsReceivedInCash - ExpensesInCash - RefundsInCash;
			}
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Balance", ResourceType = typeof (Resources))]
		public decimal Balance {
			get {
				return Math.Abs (CountedCash - CashInDrawer);
			}
		}

		[DataType (DataType.Currency)]
		[Display (Name = "CountedCash", ResourceType = typeof (Resources))]
		public decimal CountedCash {
			get { return (decimal?)CashCounts.Where (x => x.Type == CashCountType.CountedCash).Sum (x => x.Total) ?? 0; }
		}

		public IList<CashCount> CashCounts { get; set; }
		public IList<CustomerPayment> Expenses { get; set; }
		// See IsRefund and IsSale.
		public IList<CustomerPayment> Refunds { get; set; }
		public IList<CustomerPayment> Payments { get; set; }
	}

}