using System;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Mictlanix.BE.Model
{
    [ActiveRecord("fiscal_document_detail")]
    public class FiscalDocumentDetail : ActiveRecordLinqBase<FiscalDocumentDetail>
    {
        public FiscalDocumentDetail()
        {
        }
		
        [PrimaryKey(PrimaryKeyType.Identity, "fiscal_document_detail_id")]
        public int Id { get; set; }

        [BelongsTo("document")]
		[Display(Name = "FiscalDocument", ResourceType = typeof(Resources))]
		public virtual FiscalDocument Document { get; set; }
		
        [BelongsTo("product")]
        [Display(Name = "Product", ResourceType = typeof(Resources))]
        public virtual Product Product { get; set; }
		
        [BelongsTo("order_detail")]
        [Display(Name = "OrderDetail", ResourceType = typeof(Resources))]
        public virtual SalesOrderDetail OrderDetail { get; set; }
		
        [Property("product_code")]
        [Display(Name = "ProductCode", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(25, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string ProductCode { get; set; }

        [Property("product_name")]
        [Display(Name = "ProductName", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(250, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string ProductName { get; set; }
		
        [Property("unit_of_measurement")]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [Display(Name = "UnitOfMeasurement", ResourceType = typeof(Resources))]
        public string UnitOfMeasurement { get; set; }
		
        [Property]
        [DisplayFormat(DataFormatString = "{0:0.####}")]
        [Display(Name = "Quantity", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof(Resources))]
        public decimal Quantity { get; set; }

        [Property]
        [Display(Name = "Price", ResourceType = typeof(Resources))]
        [DisplayFormat(DataFormatString = "{0:C4}")]
        [Required(ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof(Resources))]
        public decimal Price { get; set; }

        [Property]
        [DisplayFormat(DataFormatString = "{0:p}")]
        [Display(Name = "Discount", ResourceType = typeof(Resources))]
        public decimal Discount { get; set; }

        [Property("tax_rate")]
		[DisplayFormat(DataFormatString = "{0:p}")]
        [Display(Name = "TaxRate", ResourceType = typeof(Resources))]
        public decimal TaxRate { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "Subtotal", ResourceType = typeof(Resources))]
        public decimal Subtotal
        {
			get { return Math.Round(Quantity * Price * (1m - Discount), 2, MidpointRounding.AwayFromZero); }
        }

		[DataType(DataType.Currency)]
		[Display(Name = "Taxes", ResourceType = typeof(Resources))]
		public decimal Taxes {
			get { return Total - Subtotal; }
		}

        [DataType(DataType.Currency)]
		[Display(Name = "Total", ResourceType = typeof(Resources))]
        public decimal Total {
            get { return Math.Round(Quantity * Price * (1m - Discount) * (1 + TaxRate), 2, MidpointRounding.AwayFromZero); }
		}
    }
}
