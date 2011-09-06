using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Business.Essentials.Model
{
    [ActiveRecord("address")]
    public class Address : ActiveRecordLinqBase<Address>
    {
        IList<Supplier> suppliers = new List<Supplier>();
        IList<Customer> customers = new List<Customer>();

        [PrimaryKey(PrimaryKeyType.Identity, "address_id")]
        public int Id { get; set; }

        [Property("taxpayer_id")]
        [Display(Name = "TaxpayerId", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(13, MinimumLength = 12, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string TaxpayerId { get; set; }

        [Property("taxpayer_name")]
        [Display(Name = "TaxpayerName", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(250, MinimumLength = 3, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string TaxpayerName { get; set; }

        [Property]
        [Display(Name = "Street", ResourceType = typeof(Resources))]
        [StringLength(45, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Street { get; set; }

        [Property("exterior_number")]
        [Display(Name = "ExteriorNumber", ResourceType = typeof(Resources))]
        [StringLength(15, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string ExteriorNumber { get; set; }

        [Property("interior_number")]
        [Display(Name = "InteriorNumber", ResourceType = typeof(Resources))]
        [StringLength(15, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string InteriorNumber { get; set; }

        [Property]
        [Display(Name = "Neighborhood", ResourceType = typeof(Resources))]
        [StringLength(150, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Neighborhood { get; set; }

        [Property]
        [Display(Name = "Borough", ResourceType = typeof(Resources))]
        [StringLength(150, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Borough { get; set; }

        [Property]
        [Display(Name = "State", ResourceType = typeof(Resources))]
        [StringLength(80, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string State { get; set; }

        [Property]
        [Display(Name = "Country", ResourceType = typeof(Resources))]
        [StringLength(80, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Country { get; set; }

        [Property("zip_code")]
        [Display(Name = "ZipCode", ResourceType = typeof(Resources))]
        [StringLength(5, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string ZipCode { get; set; }

        [Property]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Comment", ResourceType = typeof(Resources))]
        [StringLength(500, MinimumLength = 0)]
        public string Comment { get; set; }

        [HasAndBelongsToMany(typeof(Supplier), Table = "supplier_address", ColumnKey = "address", ColumnRef = "supplier")]
        public IList<Supplier> Suppliers
        {
            get { return suppliers; }
            set { suppliers = value; }
        }

        [HasAndBelongsToMany(typeof(Customer), Table = "customer_address", ColumnKey = "address", ColumnRef = "customer")]
        public IList<Customer> Customers
        {
            get { return customers; }
            set { customers = value; }
        }
    }
}