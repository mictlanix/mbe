// 
// SalesOrderDetail.cs
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
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Mictlanix.BE.Model {
	[ActiveRecord ("sales_quote_detail")]
	public class SalesQuoteDetail : ActiveRecordLinqBase<SalesQuoteDetail> {
		[PrimaryKey (PrimaryKeyType.Identity, "sales_quote_detail_id")]
		public int Id { get; set; }

		[BelongsTo ("sales_quote", Lazy = FetchWhen.OnInvoke)]
		[Display (Name = "SalesQuote", ResourceType = typeof (Resources))]
		public virtual SalesQuote SalesQuote { get; set; }

		[BelongsTo ("product")]
		[Display (Name = "Product", ResourceType = typeof (Resources))]
		public virtual Product Product { get; set; }

		[Property]
		[DisplayFormat (DataFormatString = "{0:0.####}")]
		[Display (Name = "Quantity", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof (Resources))]
		public decimal Quantity { get; set; }

		[Property]
		[Display (Name = "Price", ResourceType = typeof (Resources))]
		[DisplayFormat (DataFormatString = "{0:C4}")]
		[Required (ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof (Resources))]
		public decimal Price { get; set; }

		[Property("price_adjustment")]
		[Display (Name = "PriceAdjustment", ResourceType = typeof (Resources))]
		[DisplayFormat (DataFormatString = "{0:C4}")]
		[Required (ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof (Resources))]
		public decimal PriceAdjustment { get; set; }

		[Display (Name = "Price", ResourceType = typeof (Resources))]
		[DisplayFormat (DataFormatString = "{0:C4}")]
		public decimal NetPrice {
			get { return ModelHelpers.NetPrice (Price + PriceAdjustment, TaxRate, IsTaxIncluded); }
		}

		[Display (Name = "Price", ResourceType = typeof (Resources))]
		[DisplayFormat (DataFormatString = "{0:C4}")]
		public decimal PriceTaxIncluded {
			get { return ModelHelpers.PriceTaxIncluded (Price + PriceAdjustment, TaxRate, IsTaxIncluded); }
		}

		[Property ("discount")]
		[DisplayFormat (DataFormatString = "{0:p}")]
		[Display (Name = "Discount", ResourceType = typeof (Resources))]
		public decimal DiscountRate { get; set; }

		[Property ("tax_rate")]
		[DisplayFormat (DataFormatString = "{0:p}")]
		[Display (Name = "TaxRate", ResourceType = typeof (Resources))]
		public decimal TaxRate { get; set; }

		[Property ("tax_included")]
		[Display (Name = "TaxIncluded", ResourceType = typeof (Resources))]
		public bool IsTaxIncluded { get; set; }

		[Property]
		[Display (Name = "Currency", ResourceType = typeof (Resources))]
		public virtual CurrencyCode Currency { get; set; }

		[Property ("exchange_rate")]
		[DisplayFormat (DataFormatString = "{0:0.00##}")]
		[Display (Name = "ExchangeRate", ResourceType = typeof (Resources))]
		public virtual decimal ExchangeRate { get; set; }

		[Property ("product_code")]
		[Display (Name = "ProductCode", ResourceType = typeof (Resources))]
		[StringLength (25, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string ProductCode { get; set; }

		[Property ("product_name")]
		[Display (Name = "ProductName", ResourceType = typeof (Resources))]
		[StringLength (250, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string ProductName { get; set; }

		[Property]
		[DataType (DataType.MultilineText)]
		[Display (Name = "Comment", ResourceType = typeof (Resources))]
		[StringLength (500, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string Comment { get; set; }

		[DataType (DataType.Currency)]
		[Display (Name = "Subtotal", ResourceType = typeof (Resources))]
		[DisplayFormat (DataFormatString = "{0:C4}")]
		public decimal Subtotal {
			get { return ModelHelpers.Subtotal (Quantity, Price + PriceAdjustment, 1m, TaxRate, IsTaxIncluded, 6); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Discount", ResourceType = typeof (Resources))]
		public decimal Discount {
			get { return ModelHelpers.Discount (Quantity, Price + PriceAdjustment, 1m, DiscountRate, TaxRate, IsTaxIncluded, 6); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Taxes", ResourceType = typeof (Resources))]
		public decimal Taxes {
			get { return Total - Subtotal + Discount; }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Total", ResourceType = typeof (Resources))]
		public decimal Total {
			get { return ModelHelpers.Total (Quantity, Price + PriceAdjustment, 1m, DiscountRate, TaxRate, IsTaxIncluded, 6); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Subtotal", ResourceType = typeof (Resources))]
		public decimal SubtotalEx {
			get { return ModelHelpers.Subtotal (Quantity, Price + PriceAdjustment, ExchangeRate, TaxRate, IsTaxIncluded, 6); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Discount", ResourceType = typeof (Resources))]
		public decimal DiscountEx {
			get { return ModelHelpers.Discount (Quantity, Price + PriceAdjustment, ExchangeRate, DiscountRate, TaxRate, IsTaxIncluded, 6); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Taxes", ResourceType = typeof (Resources))]
		public decimal TaxesEx {
			get { return TotalEx - SubtotalEx + DiscountEx; }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Total", ResourceType = typeof (Resources))]
		public decimal TotalEx {
			get { return ModelHelpers.Total (Quantity, Price + PriceAdjustment, ExchangeRate, DiscountRate, TaxRate, IsTaxIncluded, 6); }
		}

		#region Override Base Methods

		public override string ToString ()
		{
			return string.Format ("{0} [{1}, {2}, {3}]", SalesQuote, Product, Quantity, Price);
		}

		public override bool Equals (object obj)
		{
			SalesOrderDetail other = obj as SalesOrderDetail;

			if (other == null)
				return false;

			if (Id == 0 && other.Id == 0)
				return (object)this == other;
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
