using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Business.Essentials.Model
{
    [ActiveRecord("customer")]
    public class Customer : ActiveRecordLinqBase<Customer>
    {
        [PrimaryKey(PrimaryKeyType.Identity, "customer_id")]
        public int Id { get; set; }

        [Property]
        [Display(Name = "Name", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(250, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Name { get; set; }

        [Property]
        [Display(Name = "Zone", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(250, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Zone { get; set; }

        [Property("credit_limit")]
        [DataType(DataType.Currency)]
        [Display(Name = "CreditLimit", ResourceType = typeof(Resources))]
        public decimal CreditLimit { get; set; }

        [Property("credit_days")]
        [Display(Name = "CreditDays", ResourceType = typeof(Resources))]
        public int CreditDays { get; set; }

        [Property]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Comment", ResourceType = typeof(Resources))]
        [StringLength(500, MinimumLength = 0)]
        public string Comment { get; set; }
    }
}
