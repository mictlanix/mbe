using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Business.Essentials.Model
{
    [ActiveRecord("customer_payment")]
    public class CustomerPayment : ActiveRecordLinqBase<CustomerPayment>
    {
        [PrimaryKey(PrimaryKeyType.Identity, "customer_payment_id")]
        public int Id { get; set; }

        [BelongsTo("sales_order")]
        [Display(Name = "SalesOrder", ResourceType = typeof(Resources))]
        public virtual SalesOrder SalesOrder { get; set; }

        [Property]
        [Display(Name = "Amount", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof(Resources))]
        public decimal Amount { get; set; }

        [Property]
        [Display(Name = "Method", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof(Resources))]
        public int Method { get; set; }

        [Property]
        [DataType(DataType.DateTime)]
        [Display(Name = "Date", ResourceType = typeof(Resources))]
        public DateTime? Date { get; set; }

        [BelongsTo("cash_session")]
        [Display(Name = "CashSession", ResourceType = typeof(Resources))]
        public virtual CashSession CashSession { get; set; }

        [Property]
        [Display(Name = "Reference", ResourceType = typeof(Resources))]
        public string Reference { get; set; }

    }
}
