using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Business.Essentials.Model
{
    [ActiveRecord("sales_order_detail")]
    public class SalesOrderDetail : ActiveRecordLinqBase<SalesOrderDetail>
    {
        [PrimaryKey(PrimaryKeyType.Identity, "sales_order_detail_id")]
        public int Id { get; set; }

        [BelongsTo("sales_order", Lazy = FetchWhen.OnInvoke)]
        [Display(Name = "SalesOrder", ResourceType = typeof(Resources))]
        public virtual SalesOrder SalesOrder { get; set; }

        [BelongsTo("product")]
        [Display(Name = "Product", ResourceType = typeof(Resources))]
        public virtual Product Product { get; set; }

        [Property]
        [Display(Name = "Quantity", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof(Resources))]
        public decimal Quantity { get; set; }

        [Property]
        [Display(Name = "Price", ResourceType = typeof(Resources))]
        [DataType(DataType.Currency)]
        [Required(ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof(Resources))]
        public decimal Price { get; set; }

        [Property]
        [Display(Name = "Discount", ResourceType = typeof(Resources))]
        public decimal Discount { get; set; }

        [Property("tax_rate")]
        [Display(Name = "TaxRate", ResourceType = typeof(Resources))]
        public decimal TaxRate { get; set; }

        [Property("product_code")]
        [Display(Name = "ProductCode", ResourceType = typeof(Resources))]
        [StringLength(25, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string ProductCode { get; set; }

        [Property("product_name")]
        [Display(Name = "ProductName", ResourceType = typeof(Resources))]
        [StringLength(250, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string ProductName { get; set; }

        [DataType(DataType.Currency)]
        public decimal Taxes
        {
            get { return Quantity * Price / (1 + TaxRate); }
        }

        [DataType(DataType.Currency)]
        public decimal Total
        {
            get { return Quantity * Price; }
        }
    }
}
