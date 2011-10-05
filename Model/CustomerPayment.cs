using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Business.Essentials.Model
{
    public enum PaymentMethod : int
    {
        [Display(Name = "Cash", ResourceType = typeof(Resources))]
        Cash = 1,
        [Display(Name = "CreditCard", ResourceType = typeof(Resources))]
        CreditCard,
        [Display(Name = "DebitCard", ResourceType = typeof(Resources))]
        DebitCard,
        [Display(Name = "Check", ResourceType = typeof(Resources))]
        Check,
        [Display(Name = "WireTransfer", ResourceType = typeof(Resources))]
        WireTransfer,
        [Display(Name = "GovernmentFunding", ResourceType = typeof(Resources))]
        GovernmentFunding
    }

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
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        public PaymentMethod Method { get; set; }

        [Property]
        [DataType(DataType.DateTime)]
        [Display(Name = "Date", ResourceType = typeof(Resources))]
        public DateTime Date { get; set; }

        [BelongsTo("cash_session")]
        [Display(Name = "CashSession", ResourceType = typeof(Resources))]
        public virtual CashSession CashSession { get; set; }

        [Property]
        [Display(Name = "Reference", ResourceType = typeof(Resources))]
        public string Reference { get; set; }

    }
}
