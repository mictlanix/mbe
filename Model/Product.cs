// 
// Product.cs
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
using Business.Essentials.Model.Validation;

namespace Business.Essentials.Model
{
    [ActiveRecord("product")]
    public class Product : ActiveRecordLinqBase<Product>
    {
        [PrimaryKey(PrimaryKeyType.Identity, "product_id")]
        public int Id { get; set; }

        [Property]
        [ValidateIsUnique]
        [Display(Name = "Code", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [UniqueProductCode(ErrorMessageResourceName = "Validation_Duplicate", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(25, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Code { get; set; }

        [Property]
        [Display(Name = "SKU", ResourceType = typeof(Resources))]
        [StringLength(25, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string SKU { get; set; }

        [Property]
        [Display(Name = "Name", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(250, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Name { get; set; }

        [Property]
        [Display(Name = "Brand", ResourceType = typeof(Resources))]
        [StringLength(100, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Brand { get; set; }

        [Property]
        [Display(Name = "Model", ResourceType = typeof(Resources))]
        [StringLength(100, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Model { get; set; }

        [Property]
        [Display(Name = "Location", ResourceType = typeof(Resources))]
        [StringLength(50, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Location { get; set; }

        [Property]
        [Display(Name = "Cost", ResourceType = typeof(Resources))]
        [DataType(DataType.Currency)]
        [Required(ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof(Resources))]
        public decimal Cost { get; set; }

        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [Property("unit_of_measurement")]
        [Display(Name = "UnitOfMeasurement", ResourceType = typeof(Resources))]
        public string UnitOfMeasurement { get; set; }

        [Property("perishable")]
        [Display(Name = "Perishable", ResourceType = typeof(Resources))]
        public bool IsPerishable { get; set; }

        [Property("seriable")]
        [Display(Name = "Seriable", ResourceType = typeof(Resources))]
        public bool IsSeriable { get; set; }

        [Property("invoiceable")]
        [Display(Name = "Invoiceable", ResourceType = typeof(Resources))]
        public bool IsInvoiceable { get; set; }

        [Property]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Comment", ResourceType = typeof(Resources))]
        public string Comment { get; set; }

        [Property("price1")]
        [Display(Name = "Price1", ResourceType = typeof(Resources))]
        [DataType(DataType.Currency)]
        [Required(ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof(Resources))]
        public decimal Price1 { get; set; }

        [Property("price2")]
        [Display(Name = "Price2", ResourceType = typeof(Resources))]
        [DataType(DataType.Currency)]
        [Required(ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof(Resources))]
        public decimal Price2 { get; set; }

        [Property("price3")]
        [Display(Name = "Price3", ResourceType = typeof(Resources))]
        [DataType(DataType.Currency)]
        [Required(ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof(Resources))]
        public decimal Price3 { get; set; }

        [Property("price4")]
        [Display(Name = "Price4", ResourceType = typeof(Resources))]
        [DataType(DataType.Currency)]
        [Required(ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof(Resources))]
        public decimal Price4 { get; set; }

        [Property("tax_rate")]
        [Display(Name = "TaxRate", ResourceType = typeof(Resources))]
        public decimal TaxRate { get; set; }
		
        [Property("tax_included")]
        [Display(Name = "TaxIncluded", ResourceType = typeof(Resources))]
        public bool IsTaxIncluded { get; set; }
		
		[Property]
        [UIHint("Image")]
        [Display(Name = "Photo", ResourceType = typeof(Resources))]
        public string Photo { get; set; }

        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [Display(Name = "Category", ResourceType = typeof(Resources))]
        [UIHint("CategorySelector")]
        public int CategoryId { get; set; }

        [BelongsTo("category")]
        [Display(Name = "Category", ResourceType = typeof(Resources))]
        public virtual Category Category { get; set; }

        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [Display(Name = "Supplier", ResourceType = typeof(Resources))]
        [UIHint("SupplierSelector")]
		public int SupplierId { get; set; }

        [BelongsTo("supplier")]
        [Display(Name = "Supplier", ResourceType = typeof(Resources))]
        public virtual Supplier Supplier { get; set; }

        #region Override Base Methods

        public override string ToString()
        {
            return string.Format("{0} [{1}, {2}, {3}]", Code, Name, Brand, Model);
        }

        public override bool Equals(object obj)
        {
            Product other = obj as Product;

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
