// 
// InventoryReceiptDetail.cs
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
	[ActiveRecord ("purchase_clearance_entry")]
	public class PurchaseClearanceDetailEntry : ActiveRecordLinqBase<PurchaseClearanceDetailEntry> {
		[PrimaryKey (PrimaryKeyType.Identity, "purchase_clearance_entry_id")]
		public int Id { get; set; }

		[BelongsTo ("purchase_clearance_detail", Lazy = FetchWhen.OnInvoke)]
		[Display (Name = "PurchaseClearance", ResourceType = typeof (Resources))]
		public virtual PurchaseClearanceDetail PurchaseClearanceDetail { get; set; }

		[Property]
		[DisplayFormat (DataFormatString = "{0:0.####}")]
		[Display (Name = "Quantity", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof (Resources))]
		public decimal Quantity { get; set; }

		[Property]
		[DisplayFormat (DataFormatString = "{0:c}")]
		[Display (Name = "Price", ResourceType = typeof (Resources))]
		[DataType (DataType.Currency)]
		[Required (ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof (Resources))]
		public decimal Price { get; set; }

		[Property ("discount")]
		[DisplayFormat (DataFormatString = "{0:p}")]
		[Display (Name = "Discount", ResourceType = typeof (Resources))]
		public decimal DiscountRate { get; set; }

		[Property ("tax_rate")]
		[Display (Name = "TaxRate", ResourceType = typeof (Resources))]
		public decimal TaxRate { get; set; }

		[Property ("product_name")]
		[Display (Name = "ProductName", ResourceType = typeof (Resources))]
		[DataType (DataType.Text)]
		public string ProductName { get; set; }

		[Property ("exchange_rate")]
		[DisplayFormat (DataFormatString = "{0:0.00##}")]
		[Display (Name = "ExchangeRate", ResourceType = typeof (Resources))]
		public virtual decimal ExchangeRate { get; set; }

		[Property]
		[Display (Name = "Currency", ResourceType = typeof (Resources))]
		public virtual CurrencyCode Currency { get; set; }

		[Property ("tax_included")]
		[Display (Name = "TaxIncluded", ResourceType = typeof (Resources))]
		public bool IsTaxIncluded { get; set; }
		#region Override Base Methods

		public override string ToString ()
		{
			return string.Format ("{0} [{1}, {2}, {3}]", Id, PurchaseClearanceDetail, ProductName, Quantity);
		}

		public override bool Equals (object obj)
		{
			var other = obj as DeliveryOrderDetail;

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

		[Display (Name = "Price", ResourceType = typeof (Resources))]
		[DataType (DataType.Currency)]
		public decimal NetPrice {
			get { return ModelHelpers.NetPrice (Price, TaxRate, IsTaxIncluded); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Total", ResourceType = typeof (Resources))]
		public decimal Total {
			get { return ModelHelpers.Total (Quantity, Price, 1, DiscountRate, TaxRate, IsTaxIncluded); }
		}
	}
}
