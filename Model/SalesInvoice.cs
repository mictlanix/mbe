using System;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Business.Essentials.Model
{
    [ActiveRecord("sales_invoice")]
    public class SalesInvoice : ActiveRecordLinqBase<SalesInvoice>
    {
        IList<SalesInvoiceDetail> details = new List<SalesInvoiceDetail>();

        public SalesInvoice()
        {
        }

        [PrimaryKey(PrimaryKeyType.Identity, "sales_invoice_id")]
        [Display(Name = "SalesInvoiceId", ResourceType = typeof(Resources))]
        [DisplayFormat(DataFormatString="{0:000000}")]
        public int Id { get; set; }
		
        [Property("creation_time")]
        [DataType(DataType.DateTime)]
        [Display(Name = "CreationTime", ResourceType = typeof(Resources))]
        public DateTime CreationTime { get; set; }

        [Property("modification_time")]
        [DataType(DataType.DateTime)]
        [Display(Name = "ModificationTime", ResourceType = typeof(Resources))]
        public DateTime ModificationTime { get; set; }

        [BelongsTo("creator")]
        [Display(Name = "Creator", ResourceType = typeof(Resources))]
        public virtual Employee Creator { get; set; }

        [BelongsTo("updater")]
        [Display(Name = "Updater", ResourceType = typeof(Resources))]
        public virtual Employee Updater { get; set; }
		
        [Property]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Comment", ResourceType = typeof(Resources))]
        [StringLength(500, MinimumLength = 0)]
        public string Comment { get; set; }
		
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [Display(Name = "Customer", ResourceType = typeof(Resources))]
        [UIHint("CustomerSelector")]
        public int CustomerId { get; set; }

        [BelongsTo("customer")]
        [Display(Name = "Customer", ResourceType = typeof(Resources))]
        public virtual Customer Customer { get; set; }

        [BelongsTo("taxpayer")]
        [Display(Name = "Issuer", ResourceType = typeof(Resources))]
        public Taxpayer Issuer { get; set; }
		
        [Property("purchase_order")]
        [Display(Name = "PurchaseOrder", ResourceType = typeof(Resources))]
        [StringLength(25, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string PurchaseOrder { get; set; }
        
		[Property]
        [Display(Name = "Issued", ResourceType = typeof(Resources))]
        public DateTime? Issued { get; set; }
        
		[Property]
        [Display(Name = "Batch", ResourceType = typeof(Resources))]
		public string Batch { get; set; }
		
		[Property]
        [Display(Name = "Serial", ResourceType = typeof(Resources))]
        public int? Serial { get; set; }
		
		[Property("approval_number")]
        [Display(Name = "ApprovalNumber", ResourceType = typeof(Resources))]
        public int ApprovalNumber { get; set; }
		
		[Property("approval_year")]
        [Display(Name = "ApprovalYear", ResourceType = typeof(Resources))]
        public int ApprovalYear { get; set; }
		
		[Property("certificate_number")]
        [Display(Name = "CertificateNumber", ResourceType = typeof(Resources))]
        public decimal CertificateNumber { get; set; }
		
		[Property("original_string")]
        [Display(Name = "OriginalString", ResourceType = typeof(Resources))]
        public string OriginalString { get; set; }
		
		[Property("digital_seal")]
        [Display(Name = "DigitalSeal", ResourceType = typeof(Resources))]
        public string DigitalSeal { get; set; }
		
		[Property("customer_tax_id")]
        [Display(Name = "TaxpayerId", ResourceType = typeof(Resources))]
        [StringLength(13, MinimumLength = 12, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string CustomerTaxId { get; set; }

        [Property("customer_tax_name")]
        [Display(Name = "TaxpayerName", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(250, MinimumLength = 3, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string CustomerTaxName { get; set; }
		
		[Property]
        [Display(Name = "Street", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(250, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Street { get; set; }

        [Property("exterior_number")]
        [Display(Name = "ExteriorNumber", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(15, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string ExteriorNumber { get; set; }

        [Property("interior_number")]
        [Display(Name = "InteriorNumber", ResourceType = typeof(Resources))]
        [StringLength(15, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string InteriorNumber { get; set; }

        [Property]
        [Display(Name = "Neighborhood", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(150, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Neighborhood { get; set; }

        [Property]
        [Display(Name = "Borough", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(150, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Borough { get; set; }

        [Property]
        [Display(Name = "State", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(80, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string State { get; set; }

        [Property]
        [Display(Name = "Country", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(80, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Country { get; set; }

        [Property("zip_code")]
        [Display(Name = "ZipCode", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(5, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string ZipCode { get; set; }
		
        [Property("completed")]
        [Display(Name = "Completed", ResourceType = typeof(Resources))]
        public bool IsCompleted { get; set; }

        [Property("cancelled")]
        [Display(Name = "Cancelled", ResourceType = typeof(Resources))]
        public bool IsCancelled { get; set; }

        [Property("cancellation_date")]
        [Display(Name = "CancellationDate", ResourceType = typeof(Resources))]		
        public DateTime? CancellationDate { get; set; }

        [HasMany(typeof(SalesInvoiceDetail), Table = "sales_invoice_detail", ColumnKey = "invoice")]
        public IList<SalesInvoiceDetail> Details
        {
            get { return details; }
            set { details = value; }
        }
    }
}
