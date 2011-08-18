using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Business.Essentials.Model
{
    [ActiveRecord("price_list")]
    public class PriceList : ActiveRecordLinqBase<PriceList>
    {
        [PrimaryKey(PrimaryKeyType.Identity, "price_list_id")]
        public int Id { get; set; }

        [Property]
        [Display(Name = "Name", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(250, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Name { get; set; }

        [Property("high_profit_margin")]
        [DataType(DataType.Currency)]
        [Display(Name = "HighProfitMargin", ResourceType = typeof(Resources))]
        public decimal HighProfitMargin { get; set; }

        [Property("low_profit_margin")]
        [DataType(DataType.Currency)]
        [Display(Name = "LowProfitMargin", ResourceType = typeof(Resources))]
        public decimal LowProfitMargin { get; set; }

    }
}
