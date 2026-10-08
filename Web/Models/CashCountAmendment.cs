//
// CashCountAmendment.cs
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
using System.ComponentModel.DataAnnotations;
using Mictlanix.BE.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mictlanix.BE.Web.Models {
	// A payment correction made after its cash session was counted
	// (mictlanix/mbe#66 §6.8). Its effect on the count: NetBefore taken out of
	// MethodBefore, NetAfter put into MethodAfter.
	public class CashCountAmendment {
		[DataType (DataType.DateTime)]
		[Display (Name = "Date", ResourceType = typeof (Resources))]
		public DateTime Time { get; set; }

		[Display (Name = "Updater", ResourceType = typeof (Resources))]
		public Employee Updater { get; set; }

		[DisplayFormat (DataFormatString = "{0:D8}")]
		[Display (Name = "Payment", ResourceType = typeof (Resources))]
		public int Payment { get; set; }

		[Display (Name = "Operation", ResourceType = typeof (Resources))]
		public string Operation { get; set; }

		[Display (Name = "Comment", ResourceType = typeof (Resources))]
		public string Comment { get; set; }

		public int Session { get; set; }
		public PaymentMethod MethodBefore { get; set; }
		public decimal NetBefore { get; set; }
		public PaymentMethod MethodAfter { get; set; }
		public decimal NetAfter { get; set; }

		// Null for anything but a correction record.
		public static CashCountAmendment Parse (Incidence incidence)
		{
			JObject json;

			try {
				json = JObject.Parse (incidence.PreviousState ?? string.Empty);
			} catch (JsonReaderException) {
				return null;
			}

			if (json ["Operation"] == null || json ["CashSession"] == null) {
				return null;
			}

			return new CashCountAmendment {
				Time = incidence.ModificationTime,
				Updater = incidence.Updater,
				Payment = incidence.Reference,
				Operation = (string) json ["Operation"],
				Comment = incidence.Comment,
				Session = (int) json ["CashSession"],
				MethodBefore = (PaymentMethod) (int) json ["MethodBefore"],
				NetBefore = (decimal) json ["NetBefore"],
				MethodAfter = (PaymentMethod) (int) json ["MethodAfter"],
				NetAfter = (decimal) json ["NetAfter"]
			};
		}
	}
}
