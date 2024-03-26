// 
// Product.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
//   Eduardo Nieto <enieto@mictlanix.com>
// 
// Copyright (C) 2017 Mictlanix SAS de CV and contributors.
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
using Mictlanix.BE.Model.Validation;

namespace Mictlanix.BE.Model {
	[ActiveRecord ("product")]
	public class Product : ActiveRecordLinqBase<Product> {
		IList<Label> labels = new List<Label> ();
		IList<ProductPrice> prices = new List<ProductPrice> ();

		public Product ()
		{
			IsSalable = true;
			IsStockable = true;
			IsPurchasable = true;
			IsInvoiceable = true;
		}

		[PrimaryKey (PrimaryKeyType.Identity, "product_id")]
		public int Id { get; set; }

		[Property]
		[ValidateIsUnique]
		[Display (Name = "Code", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[UniqueProductCode (ErrorMessageResourceName = "Validation_Duplicate", ErrorMessageResourceType = typeof (Resources))]
		[RegularExpression (@"^\S+$", ErrorMessageResourceName = "Validation_NonWhiteSpace", ErrorMessageResourceType = typeof (Resources))]
		[StringLength (25, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string Code { get; set; }

		[Property]
		[Display (Name = "Name", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[StringLength (250, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string Name { get; set; }

		[Property]
		[Display (Name = "Model", ResourceType = typeof (Resources))]
		[StringLength (100, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string Model { get; set; }

		[Property]
		[Display (Name = "Brand", ResourceType = typeof (Resources))]
		[StringLength (100, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string Brand { get; set; }

		[Property]
		[Display (Name = "SKU", ResourceType = typeof (Resources))]
		[StringLength (25, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string SKU { get; set; }

		[Property]
		[Display (Name = "Location", ResourceType = typeof (Resources))]
		[StringLength (50, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string Location { get; set; }

		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[Property ("unit_of_measurement", Update = false, Insert = false)]
		[Display (Name = "UnitOfMeasurement", ResourceType = typeof (Resources))]
		[UIHint ("UnitOfMeasurement")]
		public string UnitOfMeasurementId { get; set; }

		[BelongsTo ("unit_of_measurement")]
		[Display (Name = "UnitOfMeasurement", ResourceType = typeof (Resources))]
		public SatUnitOfMeasurement UnitOfMeasurement { get; set; }

		[Property ("stockable")]
		[Display (Name = "Stockable", ResourceType = typeof (Resources))]
		public bool IsStockable { get; set; }

		[Property ("perishable")]
		[Display (Name = "Perishable", ResourceType = typeof (Resources))]
		public bool IsPerishable { get; set; }

		[Property ("seriable")]
		[Display (Name = "Seriable", ResourceType = typeof (Resources))]
		public bool IsSeriable { get; set; }

		[Property ("purchasable")]
		[Display (Name = "Purchasable", ResourceType = typeof (Resources))]
		public bool IsPurchasable { get; set; }

		[Property ("salable")]
		[Display (Name = "Salable", ResourceType = typeof (Resources))]
		public bool IsSalable { get; set; }

		[Property ("invoiceable")]
		[Display (Name = "Invoiceable", ResourceType = typeof (Resources))]
		public bool IsInvoiceable { get; set; }

		[Property ("tax_rate")]
		[DisplayFormat (DataFormatString = "{0:p}")]
		[Display (Name = "TaxRate", ResourceType = typeof (Resources))]
		public decimal TaxRate { get; set; }

		[Property ("tax_included")]
		[Display (Name = "TaxIncluded", ResourceType = typeof (Resources))]
		public bool IsTaxIncluded { get; set; }

		[Property ("price_type")]
		[Display (Name = "PriceType", ResourceType = typeof (Resources))]
		public PriceType PriceType { get; set; }

		[Display (Name = "VariablePricing", ResourceType = typeof (Resources))]
		public bool HasVariablePricing {
			get { return PriceType != PriceType.Fixed; }
		}

		[Property]
		[Display (Name = "Currency", ResourceType = typeof (Resources))]
		public virtual CurrencyCode Currency { get; set; }

		[Property ("min_order_qty")]
		[DisplayFormat (DataFormatString = "{0:0.####}")]
		[Display (Name = "MinimumOrderQuantity", ResourceType = typeof (Resources))]
		public virtual decimal MinimumOrderQuantity { get; set; }

		[Property]
		[UIHint ("Image")]
		[Display (Name = "Photo", ResourceType = typeof (Resources))]
		public string Photo { get; set; }

		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "Supplier", ResourceType = typeof (Resources))]
		[UIHint ("SupplierSelector")]
		public int SupplierId { get; set; }

		[BelongsTo ("supplier")]
		[Display (Name = "Supplier", ResourceType = typeof (Resources))]
		public virtual Supplier Supplier { get; set; }

		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[Property ("`key`", Update = false, Insert = false)]
		[Display (Name = "ProductServiceKey", ResourceType = typeof (Resources))]
		[UIHint ("ProductServiceSelector")]
		public string ProductServiceId { get; set; }

		[BelongsTo ("`key`")]
		[Display (Name = "ProductServiceKey", ResourceType = typeof (Resources))]
		public SatProductService ProductService { get; set; }

		[Property]
		[DataType (DataType.MultilineText)]
		[Display (Name = "Comment", ResourceType = typeof (Resources))]
		public string Comment { get; set; }

		[Property ("deactivated")]
		[Display (Name = "Deactivated")]
		public bool IsDeactivated { get; set; }

		[HasAndBelongsToMany (typeof (Label), Table = "product_label", ColumnKey = "product", ColumnRef = "label", Lazy = true)]
		public virtual IList<Label> Labels {
			get { return labels; }
			set { labels = value; }
		}

		[HasMany (typeof (ProductPrice), Table = "product_price", ColumnKey = "product", Lazy = true)]
		public IList<ProductPrice> Prices {
			get { return prices; }
			set { prices = value; }
		}

		#region Override Base Methods

		public override string ToString ()
		{
			return string.Format ("{0} [{1}, {2}, {3}]", Code, Name, Brand, Model);
		}

		public override bool Equals (object obj)
		{
			Product other = obj as Product;

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
