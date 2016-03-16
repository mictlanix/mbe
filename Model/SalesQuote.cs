// 
// SalesOrder.cs
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
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;
using Mictlanix.BE.Model.Validation;

namespace Mictlanix.BE.Model {
	[ActiveRecord ("sales_quote")]
	public class SalesQuote : ActiveRecordLinqBase<SalesQuote> {
		IList<SalesQuoteDetail> details = new List<SalesQuoteDetail> ();

		[PrimaryKey (PrimaryKeyType.Identity, "sales_quote_id")]
		[Display (Name = "SalesQuoteId", ResourceType = typeof (Resources))]
		[DisplayFormat (DataFormatString = "{0:D8}")]
		public int Id { get; set; }

		[BelongsTo ("store")]
		[Display (Name = "Store", ResourceType = typeof (Resources))]
		public virtual Store Store { get; set; }

		[Property ("serial")]
		[Display (Name = "Serial", ResourceType = typeof (Resources))]
		[DisplayFormat (DataFormatString = "{0:D8}")]
		public int Serial { get; set; }

		[Property]
		[DataType (DataType.DateTime)]
		[Display (Name = "Date", ResourceType = typeof (Resources))]
		public DateTime Date { get; set; }

		[BelongsTo ("salesperson")]
		[Display (Name = "SalesPerson", ResourceType = typeof (Resources))]
		public virtual Employee SalesPerson { get; set; }

		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "Customer", ResourceType = typeof (Resources))]
		[UIHint ("CustomerSelector")]
		public int CustomerId { get; set; }

		[BelongsTo ("customer")]
		[Display (Name = "Customer", ResourceType = typeof (Resources))]
		public virtual Customer Customer { get; set; }

		[Property ("due_date")]
		[DataType (DataType.Date)]
		[Display (Name = "DueDate", ResourceType = typeof (Resources))]
		[DateGreaterThan ("Date", ErrorMessageResourceName = "Validation_DateGreaterThan", ErrorMessageResourceType = typeof (Resources))]
		public DateTime DueDate { get; set; }

		[Property ("completed")]
		[Display (Name = "Completed", ResourceType = typeof (Resources))]
		public bool IsCompleted { get; set; }

		[Property ("cancelled")]
		[Display (Name = "Cancelled", ResourceType = typeof (Resources))]
		public bool IsCancelled { get; set; }

		[HasMany (typeof (SalesQuoteDetail), Table = "sales_quote_detail", ColumnKey = "sales_quote")]
		public IList<SalesQuoteDetail> Details {
			get { return details; }
			set { details = value; }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Subtotal", ResourceType = typeof (Resources))]
		public decimal Subtotal {
			get { return Details.Sum (x => x.Subtotal); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Taxes", ResourceType = typeof (Resources))]
		public decimal Taxes {
			get { return Total - Subtotal; }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Total", ResourceType = typeof (Resources))]
		public decimal Total {
			get { return Details.Sum (x => x.Total); }
		}

		#region Override Base Methods

		public override string ToString ()
		{
			return string.Format ("{0:D8} [{1}, {2}, {3}]", Id, Customer, Date, SalesPerson);
		}

		public override bool Equals (object obj)
		{
			SalesQuote other = obj as SalesQuote;

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
