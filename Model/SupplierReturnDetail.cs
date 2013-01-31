// 
// SupplierReturnDetail.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.org>
//   Eduardo Nieto <enieto@mictlanix.org>
// 
// Copyright (C) 2011 Eddy Zavaleta, Mictlanix, and contributors.
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

namespace Mictlanix.BE.Model
{
    [ActiveRecord("supplier_return_detail")]
    public class SupplierReturnDetail : ActiveRecordLinqBase<SupplierReturnDetail>
    {
        [PrimaryKey(PrimaryKeyType.Identity, "supplier_return_detail_id")]
        public int Id { get; set; }

        [BelongsTo("supplier_return", Lazy = FetchWhen.OnInvoke)]
        [Display(Name = "SupplierReturn", ResourceType = typeof(Resources))]
        public virtual SupplierReturn Order { get; set; }

        [BelongsTo("product")]
        [Display(Name = "Product", ResourceType = typeof(Resources))]
        public virtual Product Product { get; set; }

        [BelongsTo("purchase_order_detail")]
        [Display(Name = "PurchaseOrderDetail", ResourceType = typeof(Resources))]
        public virtual PurchaseOrderDetail PurchaseOrderDetail { get; set; }

        [BelongsTo("warehouse")]
        [Display(Name = "Warehouse", ResourceType = typeof(Resources))]
        public virtual Warehouse Warehouse { get; set; }

        [Property]
        [DisplayFormat(DataFormatString = "{0:0.####}")]
        [Display(Name = "Quantity", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof(Resources))]
        public decimal Quantity { get; set; }

        [Property]
        [DisplayFormat(DataFormatString = "{0:c}")]
        [Display(Name = "Price", ResourceType = typeof(Resources))]
        [DataType(DataType.Currency)]
        [Required(ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof(Resources))]
        public decimal Price { get; set; }

        [Property]
        [DisplayFormat(DataFormatString = "{0:p}")]
        [Display(Name = "Discount", ResourceType = typeof(Resources))]
        public decimal Discount { get; set; }

        [Property("tax_rate")]
        [Display(Name = "TaxRate", ResourceType = typeof(Resources))]
        public decimal TaxRate { get; set; }
		
		[Property]
		[Display(Name = "Currency", ResourceType = typeof(Resources))]
		public virtual CurrencyCode Currency { get; set; }
		
		[Property("exchange_rate")]
		[DisplayFormat(DataFormatString = "{0:0.0000}")]
		[Display(Name = "ExchangeRate", ResourceType = typeof(Resources))]
		public virtual decimal ExchangeRate { get; set; }

        [Property("product_code")]
        [Display(Name = "ProductCode", ResourceType = typeof(Resources))]
        [StringLength(25, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string ProductCode { get; set; }

        [Property("product_name")]
        [Display(Name = "ProductName", ResourceType = typeof(Resources))]
        [StringLength(250, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string ProductName { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "Subtotal", ResourceType = typeof(Resources))]
        public decimal Subtotal
        {
            get { return Math.Round(Total / (1 + TaxRate), 2, MidpointRounding.AwayFromZero); }
        }

        [DataType(DataType.Currency)]
        [Display(Name = "Taxes", ResourceType = typeof(Resources))]
        public decimal Taxes
        {
            get { return Total - Subtotal; }
        }

        [DataType(DataType.Currency)]
        [Display(Name = "Total", ResourceType = typeof(Resources))]
        public decimal Total
        {
            get { return Math.Round(Quantity * Price * (1 - Discount), 2, MidpointRounding.AwayFromZero); }
        }


        #region Override Base Methods

        public override string ToString()
        {
            return string.Format("{0} [{1}, {2}, {3}]", Id, Order, Product, Quantity);
        }

        public override bool Equals(object obj)
        {
            SupplierReturnDetail other = obj as SupplierReturnDetail;

            if (other == null)
                return false;

            if (Id == 0 && other.Id == 0)
                return (object)this == other;
            else
                return Id == other.Id;
        }

        public override int GetHashCode()
        {
            if (Id == 0)
                return base.GetHashCode();

            return string.Format("{0}#{1}", GetType().FullName, Id).GetHashCode();
        }

        #endregion
    }
}
