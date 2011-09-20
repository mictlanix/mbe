using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Business.Essentials.Model
{
    [ActiveRecord("sales_order")]
    public class SalesOrder : ActiveRecordLinqBase<SalesOrder>
    {
        [PrimaryKey(PrimaryKeyType.Identity, "sales_order_id")]
        public int Id { get; set; }

        [BelongsTo("customer")]
        [Display(Name = "Customer", ResourceType = typeof(Resources))]
        public virtual Customer Customer { get; set; }

        [Property]
        [DataType(DataType.DateTime)]
        [Display(Name = "Date", ResourceType = typeof(Resources))]
        public DateTime? Date { get; set; }

        [BelongsTo("salesperson")]
        [Display(Name = "SalesPerson", ResourceType = typeof(Resources))]
        public virtual Employee SalesPerson { get; set; }

        [BelongsTo("point_sale")]
        [Display(Name = "Employee", ResourceType = typeof(Resources))]
        public virtual Employee Employee { get; set; }

        [Property("credit")]
        [Display(Name = "Credit", ResourceType = typeof(Resources))]
        public bool IsCredit { get; set; }

        [Property("due_date")]
        [DataType(DataType.DateTime)]
        [Display(Name = "DueDate", ResourceType = typeof(Resources))]
        public DateTime? DueDate { get; set; }

        [Property("active")]
        [Display(Name = "Active", ResourceType = typeof(Resources))]
        public bool IsActive { get; set; }

        [Property("cancelled")]
        [Display(Name = "Cancelled", ResourceType = typeof(Resources))]
        public bool IsCancelled { get; set; }
    }
}
