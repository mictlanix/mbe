//
// PaymentCorrectionConfirmation.cs
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
using System.Collections.Generic;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Services;

namespace Mictlanix.BE.Web.Models {
	// The review page of a correction (mictlanix/mbe#80): the payment, what changes
	// -- method and net before and after, the orders it would un-pay, the follow-ups,
	// the amendment on a counted session -- or why it can't be done. Built without
	// writing; the confirm builds it again under lock.
	public class PaymentCorrectionConfirmation {
		// Edit, Reverse or Delete.
		public string Operation { get; set; }

		// The action the confirm form posts to.
		public string Action { get; set; }

		// The allocation for a reversal, the payment otherwise.
		public int Id { get; set; }

		public CustomerPayment Payment { get; set; }

		// The allocation a reversal takes off its order.
		public SalesOrderPayment Allocation { get; set; }

		// An edit's new values, carried to the confirm form.
		public CustomerPayment Proposed { get; set; }
		public Customer Customer { get; set; }

		public PaymentCorrection Check { get; set; }

		// An edit's new amount and change for each allocation, by allocation id.
		public IDictionary<int, KeyValuePair<decimal, decimal>> Refit { get; set; }

		public PaymentMethod MethodBefore { get; set; }
		public decimal NetBefore { get; set; }
		public PaymentMethod MethodAfter { get; set; }
		public decimal NetAfter { get; set; }

		// A refusal that doesn't come from the check.
		public string Error { get; set; }

		public bool IsRefused {
			get { return Error != null || Check == null || Check.IsBlocked; }
		}

		// A counted session: its cash count will list the correction as an amendment.
		public bool IsAmendment {
			get { return Payment != null && Payment.CashSession != null && Payment.CashSession.End.HasValue; }
		}
	}
}
