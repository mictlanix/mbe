using System;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Business.Essentials.Model
{
    [ActiveRecord("sales_invoice_detail")]
    public class SalesInvoiceDetail : ActiveRecordLinqBase<SalesInvoiceDetail>
    {
        public SalesInvoiceDetail()
        {
        }
		
        [PrimaryKey(PrimaryKeyType.Identity, "sales_invoice_detail_id")]
        public int Id { get; set; }

        [BelongsTo("invoice")]
        [Display(Name = "Invoice", ResourceType = typeof(Resources))]
        public virtual SalesInvoice Invoice { get; set; }
		
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

		[Display(Name = "Total", ResourceType = typeof(Resources))]
        public decimal Total {
			get { return Math.Round(Quantity * Price * (1m - Discount), 2, MidpointRounding.AwayFromZero); }
		}
        
		[Display(Name = "Taxes", ResourceType = typeof(Resources))]
		public decimal Taxes {
			get { return Math.Round(Total * TaxRate, 2, MidpointRounding.AwayFromZero); } 
		}
    }
}
