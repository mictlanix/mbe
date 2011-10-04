using System;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Business.Essentials.Model
{
    [ActiveRecord("sales_order")]
    public class SalesOrder : ActiveRecordLinqBase<SalesOrder>
    {
        IList<SalesOrderDetail> details = new List<SalesOrderDetail>();

        [PrimaryKey(PrimaryKeyType.Identity, "sales_order_id")]
        [Display(Name = "SalesOrderId", ResourceType = typeof(Resources))]
        public int Id { get; set; }

        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [Display(Name = "Customer", ResourceType = typeof(Resources))]
        [UIHint("CustomerSelector")]
        public int CustomerId { get; set; }

        [BelongsTo("customer")]
        [Display(Name = "Customer", ResourceType = typeof(Resources))]
        public virtual Customer Customer { get; set; }

        [Property]
        [DataType(DataType.DateTime)]
        [Display(Name = "Date", ResourceType = typeof(Resources))]
        public DateTime Date { get; set; }

        [BelongsTo("salesperson")]
        [Display(Name = "SalesPerson", ResourceType = typeof(Resources))]
        public virtual Employee SalesPerson { get; set; }

        [BelongsTo("point_sale")]
        [Display(Name = "PointOfSale", ResourceType = typeof(Resources))]
        public virtual PointSale PointOfSale { get; set; }

        [Property("credit")]
        [Display(Name = "Credit", ResourceType = typeof(Resources))]
        public bool IsCredit { get; set; }

        [Property("due_date")]
        [DataType(DataType.Date)]
        [Display(Name = "DueDate", ResourceType = typeof(Resources))]
        public DateTime DueDate { get; set; }

        [Property("completed")]
        [Display(Name = "Completed", ResourceType = typeof(Resources))]
        public bool IsCompleted { get; set; }

        [Property("cancelled")]
        [Display(Name = "Cancelled", ResourceType = typeof(Resources))]
        public bool IsCancelled { get; set; }

        [HasMany(typeof(SalesOrderDetail), Table = "sales_order_detail", ColumnKey = "sales_order")]
        public IList<SalesOrderDetail> Details
        {
            get { return details; }
            set { details = value; }
        }

        [DataType(DataType.Currency)]
        [Display(Name = "Subtotal", ResourceType = typeof(Resources))]
        public decimal Subtotal
        {
            get { return Details.Sum(x => x.Subtotal); }
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
            get { return Details.Sum(x => x.Total); }
        }
    }
}
