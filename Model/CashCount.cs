using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Business.Essentials.Model
{
    [ActiveRecord("cash_count")]
    public class CashCount : ActiveRecordLinqBase<CashCount>
    {
        [PrimaryKey(PrimaryKeyType.Identity, "cash_count_id")]
        public int Id { get; set; }

        [BelongsTo("session")]
        [Display(Name = "CashSession", ResourceType = typeof(Resources))]
        public virtual CashSession CashSession { get; set; }

        [Property]
        [Display(Name = "Denomination", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof(Resources))]
        public decimal Denomination { get; set; }

        [Property]
        [Display(Name = "Quantity", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof(Resources))]
        public int Quantity { get; set; }
    }
}
