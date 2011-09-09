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
        [Display(Name = "Code", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [UniqueProductCode(ErrorMessageResourceName = "Validation_Duplicate", ErrorMessageResourceType = typeof(Resources))]
        [ValidateIsUnique]
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
        [DisplayFormat(DataFormatString = "{0:c}")]
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

        [Property("price5")]
        [Display(Name = "Price5", ResourceType = typeof(Resources))]
        [DataType(DataType.Currency)]
        public decimal Price5 { get; set; }

        [Property("price6")]
        [Display(Name = "Price6", ResourceType = typeof(Resources))]
        [DataType(DataType.Currency)]
        public decimal Price6 { get; set; }

        [Property("price7")]
        [Display(Name = "Price7", ResourceType = typeof(Resources))]
        [DataType(DataType.Currency)]
        public decimal Price7 { get; set; }

        [UIHint("Image")]
        [Display(Name = "Photo", ResourceType = typeof(Resources))]
        public string Photo { get { return string.Format("{0}.png", Code.Trim()); } }

        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [Display(Name = "Category", ResourceType = typeof(Resources))]
        public int CategoryId { get; set; }

        [BelongsTo("category")]
        [Display(Name = "Category", ResourceType = typeof(Resources))]
        public virtual Category Category { get; set; }

        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [Display(Name = "Supplier", ResourceType = typeof(Resources))]
        public int SupplierId { get; set; }

        [BelongsTo("supplier")]
        [Display(Name = "Supplier", ResourceType = typeof(Resources))]
        public virtual Supplier Supplier { get; set; }
    }
}
