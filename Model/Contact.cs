using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using WebActivator;
using DataAnnotationsExtensions;

namespace Business.Essentials.Model
{
    [ActiveRecord("contact")]
    public class Contact : ActiveRecordLinqBase<Contact>
    {
        IList<Supplier> suppliers = new List<Supplier>();
        IList<Customer> customers = new List<Customer>();

        [PrimaryKey(PrimaryKeyType.Identity, "contact_id")]
        public int Id { get; set; }

        [Property]
        [Display(Name = "Name", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(250, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Name { get; set; }

        [Property("job_title")]
        [Display(Name = "JobTitle", ResourceType = typeof(Resources))]
        [StringLength(100, MinimumLength = 3, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string JobTitle { get; set; }

        [Property]
        [Display(Name = "Phone", ResourceType = typeof(Resources))]
        [StringLength(25, MinimumLength = 8, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Phone { get; set; }
       
        [Property("phone_ext")]
        [Display(Name = "PhoneExt", ResourceType = typeof(Resources))]
        [StringLength(5, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string PhoneExt { get; set; }

        [Property]
        [Display(Name = "Mobile", ResourceType = typeof(Resources))]
        [StringLength(25, MinimumLength = 8, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Mobile { get; set; }

        [Property]
        [Display(Name = "Fax", ResourceType = typeof(Resources))]
        [StringLength(25, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Fax { get; set; }

        [Property]
        [Url(ErrorMessageResourceName = "Validation_Url", ErrorMessageResourceType = typeof(Resources))]
        [Display(Name = "Website", ResourceType = typeof(Resources))]
        [StringLength(80, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Website { get; set; }

        [Property]
        [Email(ErrorMessageResourceName = "Validation_Email", ErrorMessageResourceType = typeof(Resources))]
        [Display(Name = "Email", ResourceType = typeof(Resources))]
        [StringLength(80, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Email { get; set; }

        [Property]
        [Display(Name = "Im", ResourceType = typeof(Resources))]
        [StringLength(80, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Im { get; set; }

        [Property]
        [Display(Name = "Sip", ResourceType = typeof(Resources))]
        [StringLength(80, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Sip { get; set; }

        [Property]
        [DataType(DataType.Date)]
        [Display(Name = "Birthday", ResourceType = typeof(Resources))]
        public DateTime? Birthday { get; set; }

        [Property]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Comment", ResourceType = typeof(Resources))]
        [StringLength(500, MinimumLength = 0)]
        public string Comment { get; set; }

        [HasAndBelongsToMany(typeof(Supplier), Table = "supplier_contact", ColumnKey = "contact", ColumnRef = "supplier")]
        public IList<Supplier> Suppliers
        {
            get { return suppliers; }
            set { suppliers = value; }
        }

        [HasAndBelongsToMany(typeof(Customer), Table = "customer_contact", ColumnKey = "contact", ColumnRef = "customer")]
        public IList<Customer> Customers
        {
            get { return customers; }
            set { customers = value; }
        }
    }
}