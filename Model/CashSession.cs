using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Business.Essentials.Model
{
    [ActiveRecord("cash_session")]
    public class CashSession : ActiveRecordLinqBase<CashSession>
    {
        [PrimaryKey(PrimaryKeyType.Identity, "cash_session_id")]
        public int Id { get; set; }

        [Property]
        [DataType(DataType.DateTime)]
        [Display(Name = "Start", ResourceType = typeof(Resources))]
        public DateTime? Start { get; set; }

        [Property]
        [DataType(DataType.DateTime)]
        [Display(Name = "End", ResourceType = typeof(Resources))]
        public DateTime? End { get; set; }

        [BelongsTo("cashier")]
        [Display(Name = "Cashier", ResourceType = typeof(Resources))]
        public virtual Employee Cashier { get; set; }

        [BelongsTo("cash_drawer")]
        [Display(Name = "CashDrawer", ResourceType = typeof(Resources))]
        public virtual CashDrawer CashDrawer { get; set; }
    }
}
