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
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Services;

namespace Mictlanix.BE.Web.Models {
	// What a reversal or a delete would do, shown before it is confirmed
	// (mictlanix/mbe#66 §6.10): the amount, the orders it would un-pay and their
	// follow-ups -- or why it can't be done.
	public class PaymentCorrectionConfirmation {
		public string Operation { get; set; }
		public string Action { get; set; }
		public int Id { get; set; }
		public CustomerPayment Payment { get; set; }
		public decimal Amount { get; set; }
		public PaymentCorrection Check { get; set; }

		// A refusal that doesn't come from the check.
		public string Error { get; set; }

		public bool IsRefused {
			get { return Error != null || Check == null || Check.IsBlocked; }
		}
	}
}
