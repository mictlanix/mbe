﻿// 
// SalesOrder.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
//   Eduardo Nieto <enieto@mictlanix.com>
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
//

using System;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Mictlanix.BE.Model {
	[ActiveRecord ("sales_quote", Lazy = true)]
	public class SalesQuote : ActiveRecordLinqBase<SalesQuote> {
		IList<SalesQuoteDetail> details = new List<SalesQuoteDetail> ();

		[PrimaryKey (PrimaryKeyType.Identity, "sales_quote_id")]
		[Display (Name = "Id", ResourceType = typeof (Resources))]
		[DisplayFormat (DataFormatString = "{0:D8}")]
		public virtual int Id { get; set; }

		[BelongsTo ("store")]
		[Display (Name = "Store", ResourceType = typeof (Resources))]
		public virtual Store Store { get; set; }

		[Property ("serial")]
		[Display (Name = "Serial", ResourceType = typeof (Resources))]
		[DisplayFormat (DataFormatString = "{0:D8}")]
		public virtual int Serial { get; set; }

		[BelongsTo ("customer", NotNull = true, Fetch = FetchEnum.Join)]
		[Display (Name = "Customer", ResourceType = typeof (Resources))]
		public virtual Customer Customer { get; set; }

		[BelongsTo ("contact", Lazy = FetchWhen.OnInvoke)]
		[Display (Name = "Contact", ResourceType = typeof (Resources))]
		public virtual Contact Contact { get; set; }

		[BelongsTo ("ship_to", Lazy = FetchWhen.OnInvoke)]
		[Display (Name = "ShipTo", ResourceType = typeof (Resources))]
		public virtual Address ShipTo { get; set; }

		[Property]
		[DataType (DataType.DateTime)]
		[Display (Name = "Date", ResourceType = typeof (Resources))]
		public virtual DateTime Date { get; set; }

		[BelongsTo ("salesperson")]
		[Display (Name = "SalesPerson", ResourceType = typeof (Resources))]
		public virtual Employee SalesPerson { get; set; }

		[Property ("payment_terms")]
		[Display (Name = "PaymentTerms", ResourceType = typeof (Resources))]
		public virtual PaymentTerms Terms { get; set; }

		[Display (Name = "Credit", ResourceType = typeof (Resources))]
		public virtual bool IsCredit {
			get { return Terms != PaymentTerms.Immediate; }
		}

		[Property]
		[Display (Name = "Currency", ResourceType = typeof (Resources))]
		public virtual CurrencyCode Currency { get; set; }

		[Property ("exchange_rate")]
		[DisplayFormat (DataFormatString = "{0:0.00##}")]
		[Display (Name = "ExchangeRate", ResourceType = typeof (Resources))]
		public virtual decimal ExchangeRate { get; set; }

		[Property ("due_date")]
		[DataType (DataType.Date)]
		[Display (Name = "DueDate", ResourceType = typeof (Resources))]
		public virtual DateTime DueDate { get; set; }

		[Property ("completed")]
		[Display (Name = "Completed", ResourceType = typeof (Resources))]
		public virtual bool IsCompleted { get; set; }

		[Property ("cancelled")]
		[Display (Name = "Cancelled", ResourceType = typeof (Resources))]
		public virtual bool IsCancelled { get; set; }

		[Property]
		[DataType (DataType.MultilineText)]
		[Display (Name = "Comment", ResourceType = typeof (Resources))]
		[StringLength (500, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string Comment { get; set; }

		[HasMany (typeof (SalesQuoteDetail), Table = "sales_quote_detail", ColumnKey = "sales_quote", Lazy = true)]
		public virtual IList<SalesQuoteDetail> Details {
			get { return details; }
			set { details = value; }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Subtotal", ResourceType = typeof (Resources))]
		public virtual decimal Subtotal {
			get { return ModelHelpers.TotalRounding (Details.Sum (x => x.Subtotal)); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Discount", ResourceType = typeof (Resources))]
		public virtual decimal Discount {
			get { return ModelHelpers.TotalRounding (Details.Sum (x => x.Discount)); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Taxes", ResourceType = typeof (Resources))]
		public virtual decimal Taxes {
			get { return ModelHelpers.TotalRounding (Details.Sum (x => x.Taxes)); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Total", ResourceType = typeof (Resources))]
		public virtual decimal Total {
			get { return ModelHelpers.TotalRounding (Details.Sum (x => x.Total)); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Subtotal", ResourceType = typeof (Resources))]
		public virtual decimal SubtotalEx {
			get { return ModelHelpers.TotalRounding (Details.Sum (x => x.SubtotalEx)); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Discount", ResourceType = typeof (Resources))]
		public virtual decimal DiscountEx {
			get { return ModelHelpers.TotalRounding (Details.Sum (x => x.DiscountEx)); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Taxes", ResourceType = typeof (Resources))]
		public virtual decimal TaxesEx {
			get { return ModelHelpers.TotalRounding (Details.Sum (x => x.TaxesEx)); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Total", ResourceType = typeof (Resources))]
		public virtual decimal TotalEx {
			get { return ModelHelpers.TotalRounding (Details.Sum (x => x.TotalEx)); }
		}

		[BelongsTo ("creator", Lazy = FetchWhen.OnInvoke)]
		[Display (Name = "Creator", ResourceType = typeof (Resources))]
		public virtual Employee Creator { get; set; }

		[Property ("creation_time")]
		[DataType (DataType.DateTime)]
		[Display (Name = "CreationTime", ResourceType = typeof (Resources))]
		public virtual DateTime CreationTime { get; set; }

		[BelongsTo ("updater", Lazy = FetchWhen.OnInvoke)]
		[Display (Name = "Updater", ResourceType = typeof (Resources))]
		public virtual Employee Updater { get; set; }

		[Property ("modification_time")]
		[DataType (DataType.DateTime)]
		[Display (Name = "ModificationTime", ResourceType = typeof (Resources))]
		public virtual DateTime ModificationTime { get; set; }

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
