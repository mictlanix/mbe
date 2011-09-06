using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Business.Essentials.Model
{
    [ActiveRecord("bank_account")]
    public class BankAccount : ActiveRecordLinqBase<BankAccount>
    {
        IList<Supplier> suppliers = new List<Supplier>();

        [PrimaryKey(PrimaryKeyType.Identity, "bank_account_id")]
        public int Id { get; set; }

        [Property("bank_name")]
        [Display(Name = "BankName", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(250, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string BankName { get; set; }

        [Property("account_number")]
        [Display(Name = "AccountNumber", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(20, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string AccountNumber { get; set; }

        [Property]
        [Display(Name = "Reference", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(20, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Reference { get; set; }

        [Property("routing_number")]
        [Display(Name = "RoutingNumber", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(18, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string RoutingNumber { get; set; }
        
        [Property]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Comment", ResourceType = typeof(Resources))]
        [StringLength(500, MinimumLength = 0)]
        public string Comment { get; set; }

        [HasAndBelongsToMany(typeof(Supplier), Table = "supplier_bank_account", ColumnKey = "bank_account", ColumnRef = "supplier")]
        public IList<Supplier> Suppliers
        {
            get { return suppliers; }
            set { suppliers = value; }
        }

    }
}