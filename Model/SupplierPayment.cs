// 
// SupplierPayment.cs
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
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Mictlanix.BE.Model {
	[ActiveRecord ("supplier_payment")]
	public class SupplierPayment : ActiveRecordLinqBase<SupplierPayment> {
		[PrimaryKey (PrimaryKeyType.Identity, "supplier_payment_id")]
		[Display (Name = "SupplierPaymentId", ResourceType = typeof (Resources))]
		[DisplayFormat (DataFormatString = "{0:D8}")]
		public int Id { get; set; }

		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "Supplier", ResourceType = typeof (Resources))]
		[UIHint ("SupplierSelector")]
		public int SupplierId { get; set; }

		[BelongsTo ("supplier")]
		[Display (Name = "Supplier", ResourceType = typeof (Resources))]
		public virtual Supplier Supplier { get; set; }

		[BelongsTo ("creator")]
		[Display (Name = "Creator", ResourceType = typeof (Resources))]
		public virtual Employee Creator { get; set; }

		[Property]
		[DataType (DataType.Currency)]
		[Display (Name = "Amount", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof (Resources))]
		public decimal Amount { get; set; }

		[Property]
		[Display (Name = "PaymentMethod", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		public PaymentMethod Method { get; set; }

		[Property]
		[DataType (DataType.DateTime)]
		[Display (Name = "Date", ResourceType = typeof (Resources))]
		public DateTime Date { get; set; }

		[Property]
		[Display (Name = "PaymentReference", ResourceType = typeof (Resources))]
		public string Reference { get; set; }

		[Property]
		[Display (Name = "Comment", ResourceType = typeof (Resources))]
		[DataType (DataType.MultilineText)]
		[StringLength (500, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string Comment { get; set; }

		[Property ("cancelled")]
		[Display (Name = "Cancelled", ResourceType = typeof (Resources))]
		public virtual bool IsCancelled { get; set; }

		[BelongsTo ("cash_session")]
		[Display (Name = "CashSession", ResourceType = typeof (Resources))]
		public virtual CashSession CashSession { get; set; }

		#region Override Base Methods

		public override string ToString ()
		{
			return string.Format ("{0} : {1:c} [{2:u}]", Method, Amount, Date);
		}

		public override bool Equals (object obj)
		{
			SupplierPayment other = obj as SupplierPayment;

			if (other == null)
				return false;

			if (Id == 0 && other.Id == 0)
				return (object) this == other;
			else
				return Id == other.Id;
		}

		public override int GetHashCode ()
		{
			if (Id == 0)
				return base.GetHashCode ();

			return string.Format ("{0}#{1}", GetType ().FullName, Id).GetHashCode ();
		}

		#endregion
	}
}
