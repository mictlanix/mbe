// 
// SalesOrderPayment.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2016 Eddy Zavaleta, Mictlanix, and contributors.
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
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Mictlanix.BE.Model {
	[ActiveRecord ("sales_order_payment")]
	public class SalesOrderPayment : ActiveRecordLinqBase<SalesOrderPayment> {
		[PrimaryKey (PrimaryKeyType.Identity, "sales_order_payment_id")]
		public virtual int Id { get; set; }

		[BelongsTo ("sales_order")]
		[Display (Name = "SalesOrder", ResourceType = typeof (Resources))]
		public virtual SalesOrder SalesOrder { get; set; }

		[BelongsTo ("customer_payment")]
		[Display (Name = "CustomerPayment", ResourceType = typeof (Resources))]
		public virtual CustomerPayment Payment { get; set; }

		[Property]
		[DataType (DataType.Currency)]
		[Range (0.0001, double.MaxValue, ErrorMessageResourceName = "Validation_CannotBeZeroOrNegative", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "Amount", ResourceType = typeof (Resources))]
		public decimal Amount { get; set; }

		[Property ("amount_change")]
		[DataType (DataType.Currency)]
		[Display (Name = "Change", ResourceType = typeof (Resources))]
		public decimal Change { get; set; }

		#region Override Base Methods

		public override string ToString ()
		{
			return string.Format ("{0:c}", Amount);
		}

		public override bool Equals (object obj)
		{
			var other = obj as SalesOrderPayment;

			if (other == null)
				return false;

			if (Id == 0 && other.Id == 0)
				return (object) this == other;
			else
				return Id == other.Id;
		}

		public override int GetHashCode ()
		{
			return string.Format ("{0}#{1}", GetType ().FullName, Id).GetHashCode ();
		}

		#endregion
	}
}
