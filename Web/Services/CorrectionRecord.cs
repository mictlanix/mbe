//
// CorrectionRecord.cs
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
using Mictlanix.BE.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mictlanix.BE.Web.Services {
	// The audit record of one payment correction (mictlanix/mbe#66 §6.6): a flat JSON
	// object, since the incidence details view shows top-level fields only. Lists are
	// joined into strings. Never CreationTime or ModificationTime keys -- the screen
	// reads those to flag records written before mictlanix/mbe#42.
	//
	// Start it before changing anything: a second Find in the same request returns
	// the already-changed object.
	public sealed class CorrectionRecord {
		const string TimeFormat = "yyyy-MM-dd HH:mm";

		readonly JObject fields = new JObject ();
		readonly int payment;

		public CorrectionRecord (string operation, CustomerPayment payment, PaymentCorrection check)
		{
			this.payment = payment.Id;

			Set ("Operation", operation);
			Set ("CashSession", payment.CashSession == null ? (int?) null : payment.CashSession.Id);
			Set ("MethodBefore", (int) payment.Method);
			Set ("NetBefore", Net (payment));
			Set ("SalesOrders", Ids (check.Orders));
			Set ("Unpaid", Ids (check.Unpaid));
			Set ("PaidAt", string.Join ("; ", check.Unpaid
				.Where (x => x.Order.BalanceZeroedTime.HasValue)
				.Select (x => string.Format ("{0}: {1}", x.Order.Id,
							     x.Order.BalanceZeroedTime.Value.ToString (TimeFormat, CultureInfo.InvariantCulture)))));
			Set ("Invoiced", check.Invoiced);
			Set ("Delivered", check.Delivered);
			Set ("Commission", check.Commission);
		}

		// The payment's net amount -- what the cash count adds up.
		public static decimal Net (CustomerPayment payment)
		{
			return payment.Amount - payment.Allocations.Sum (x => x.Change);
		}

		// A delete leaves nothing: pass the method it had and a net of zero.
		public CorrectionRecord After (PaymentMethod method, decimal net)
		{
			Set ("MethodAfter", (int) method);
			Set ("NetAfter", net);
			return this;
		}

		// Empty values are left out.
		public CorrectionRecord Set (string key, object value)
		{
			if (value == null || (value is string && ((string) value).Length == 0)) {
				fields.Remove (key);
			} else {
				fields [key] = JToken.FromObject (value);
			}

			return this;
		}

		public string ToJson ()
		{
			return fields.ToString (Formatting.None);
		}

		public Incidence ToIncidence (Employee updater, string reason)
		{
			return new Incidence {
				SourceType = SourceType.CustomerPayment,
				Reference = payment,
				Updater = updater,
				Comment = reason,
				PreviousState = ToJson (),
				ModificationTime = DateTime.Now
			};
		}

		static string Ids (IEnumerable<PaymentCorrection.OrderChange> orders)
		{
			return string.Join (", ", orders.Select (x => x.Order.Id));
		}
	}
}
