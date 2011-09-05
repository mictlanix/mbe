using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Business.Essentials.Model.Validation;

namespace Business.Essentials.Model
{
    [ActiveRecord("supplier_agreement")]
    public class SupplierAgreement : ActiveRecordLinqBase<SupplierAgreement>
    {
        [PrimaryKey(PrimaryKeyType.Identity, "supplier_agreement_id")]
        public int Id { get; set; }

        [Property]
        [DataType(DataType.Date)]
        [Display(Name = "Start", ResourceType = typeof(Resources))]
        public DateTime? Start { get; set; }

        [Property]
        [DataType(DataType.Date)]
        [Display(Name = "End", ResourceType = typeof(Resources))]
        [DateGreaterThan("Start", ErrorMessageResourceName = "Validation_DateGreaterThan", ErrorMessageResourceType = typeof(Resources))]
        public DateTime? End { get; set; }

        [Property]
        [Display(Name = "Comment", ResourceType = typeof(Resources))]
        [DataType(DataType.MultilineText)]
        [StringLength(500, MinimumLength = 0)]
        public string Comment { get; set; }

        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [Display(Name = "Supplier", ResourceType = typeof(Resources))]
        public int SupplierId { get; set; }

        [BelongsTo("supplier")]
        [Display(Name = "Supplier", ResourceType = typeof(Resources))]
        public virtual Supplier Supplier { get; set; }

    }
}
