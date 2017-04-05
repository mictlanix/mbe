// 
// CustomerRefundDetail.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2013-2017 Eddy Zavaleta, Mictlanix, and contributors.
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
using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Mictlanix.BE.Model {
	[ActiveRecord ("customer_refund_detail")]
	public class CustomerRefundDetail : ActiveRecordLinqBase<CustomerRefundDetail> {
		[PrimaryKey (PrimaryKeyType.Identity, "customer_refund_detail_id")]
		public virtual int Id { get; set; }

		[BelongsTo ("customer_refund", Lazy = FetchWhen.OnInvoke)]
		[Display (Name = "CustomerRefund", ResourceType = typeof (Resources))]
		public virtual CustomerRefund Refund { get; set; }

		[BelongsTo ("sales_order_detail")]
		public virtual SalesOrderDetail SalesOrderDetail { get; set; }

		[BelongsTo ("product")]
		[Display (Name = "Product", ResourceType = typeof (Resources))]
		public virtual Product Product { get; set; }

		[Property ("product_code")]
		[Display (Name = "ProductCode", ResourceType = typeof (Resources))]
		[StringLength (25, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string ProductCode { get; set; }

		[Property ("product_name")]
		[Display (Name = "ProductName", ResourceType = typeof (Resources))]
		[StringLength (250, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string ProductName { get; set; }

		[Property]
		[DisplayFormat (DataFormatString = "{0:0.####}")]
		[Display (Name = "Quantity", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof (Resources))]
		public virtual decimal Quantity { get; set; }

		[Property]
		[Display (Name = "Price", ResourceType = typeof (Resources))]
		[DataType (DataType.Currency)]
		[Required (ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof (Resources))]
		public virtual decimal Price { get; set; }

		[Property ("discount")]
		[DisplayFormat (DataFormatString = "{0:p}")]
		[Display (Name = "Discount", ResourceType = typeof (Resources))]
		public virtual decimal DiscountRate { get; set; }

		[Property ("tax_rate")]
		[Display (Name = "TaxRate", ResourceType = typeof (Resources))]
		public virtual decimal TaxRate { get; set; }

		[Property ("tax_included")]
		[Display (Name = "TaxIncluded", ResourceType = typeof (Resources))]
		public virtual bool IsTaxIncluded { get; set; }

		[Property]
		[Display (Name = "Currency", ResourceType = typeof (Resources))]
		public virtual CurrencyCode Currency { get; set; }

		[Property ("exchange_rate")]
		[DisplayFormat (DataFormatString = "{0:0.00##}")]
		[Display (Name = "ExchangeRate", ResourceType = typeof (Resources))]
		public virtual decimal ExchangeRate { get; set; }

		[DataType (DataType.Currency)]
		[Display (Name = "Subtotal", ResourceType = typeof (Resources))]
		public virtual decimal Subtotal {
			get { return ModelHelpers.Subtotal (Quantity, Price, 1, TaxRate, IsTaxIncluded); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Discount", ResourceType = typeof (Resources))]
		public decimal Discount {
			get { return ModelHelpers.Discount (Quantity, Price, 1, DiscountRate, TaxRate, IsTaxIncluded); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Taxes", ResourceType = typeof (Resources))]
		public virtual decimal Taxes {
			get { return Total - Subtotal + Discount; }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Total", ResourceType = typeof (Resources))]
		public virtual decimal Total {
			get { return ModelHelpers.Total (Quantity, Price, 1, DiscountRate, TaxRate, IsTaxIncluded); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Subtotal", ResourceType = typeof (Resources))]
		public virtual decimal SubtotalEx {
			get { return ModelHelpers.Subtotal (Quantity, Price, ExchangeRate, TaxRate, IsTaxIncluded); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Discount", ResourceType = typeof (Resources))]
		public decimal DiscountEx {
			get { return ModelHelpers.Discount (Quantity, Price, ExchangeRate, DiscountRate, TaxRate, IsTaxIncluded); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Taxes", ResourceType = typeof (Resources))]
		public virtual decimal TaxesEx {
			get { return TotalEx - SubtotalEx - DiscountEx; }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Total", ResourceType = typeof (Resources))]
		public virtual decimal TotalEx {
			get { return ModelHelpers.Total (Quantity, Price, ExchangeRate, DiscountRate, TaxRate, IsTaxIncluded); }
		}

		#region Override Base Methods

		public override string ToString ()
		{
			return string.Format ("{0} [{1}, {2}, {3}]", Refund, Product, Quantity, Price);
		}

		public override bool Equals (object obj)
		{
			CustomerRefundDetail other = obj as CustomerRefundDetail;

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
