// 
// FiscalDocument.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.org>
// 
// Copyright (C) 2013 Eddy Zavaleta, Mictlanix, and contributors.
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Mictlanix.BE.Model
{
    [ActiveRecord("fiscal_document", Lazy = true)]
    public class FiscalDocument : ActiveRecordLinqBase<FiscalDocument>
    {
        IList<FiscalDocumentDetail> details = new List<FiscalDocumentDetail>();

        public FiscalDocument()
        {
        }

        [PrimaryKey(PrimaryKeyType.Identity, "fiscal_document_id")]
		[Display(Name = "FiscalDocumentId", ResourceType = typeof(Resources))]
		[DisplayFormat(DataFormatString="{0:D6}")]
		public virtual int Id { get; set; }
		
		[BelongsTo("store", Fetch = FetchEnum.Join)]
		[Display(Name = "Store", ResourceType = typeof(Resources))]
		public virtual Store Store { get; set; }
		
        [Property("creation_time")]
		[DataType(DataType.DateTime)]
		[Display(Name = "CreationTime", ResourceType = typeof(Resources))]
		public virtual DateTime CreationTime { get; set; }

        [Property("modification_time")]
		[DataType(DataType.DateTime)]
		[Display(Name = "ModificationTime", ResourceType = typeof(Resources))]
		public virtual DateTime ModificationTime { get; set; }

        [BelongsTo("creator", Lazy = FetchWhen.OnInvoke)]
        [Display(Name = "Creator", ResourceType = typeof(Resources))]
        public virtual Employee Creator { get; set; }

        [BelongsTo("updater", Lazy = FetchWhen.OnInvoke)]
        [Display(Name = "Updater", ResourceType = typeof(Resources))]
        public virtual Employee Updater { get; set; }
		
        [Property]
		[DataType(DataType.MultilineText)]
		[Display(Name = "Comment", ResourceType = typeof(Resources))]
		[StringLength(500, MinimumLength = 0)]
		public virtual string Comment { get; set; }
		
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[Display(Name = "Customer", ResourceType = typeof(Resources))]
		[UIHint("CustomerSelector")]
		public virtual int CustomerId { get; set; }

        [BelongsTo("customer")]
        [Display(Name = "Customer", ResourceType = typeof(Resources))]
        public virtual Customer Customer { get; set; }
		
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[Display(Name = "Issuer", ResourceType = typeof(Resources))]
		[UIHint("TaxpayerSelector")]
		public virtual string IssuerId { get; set; }
		
        [BelongsTo("issuer")]
		[Display(Name = "Issuer", ResourceType = typeof(Resources))]
		public virtual Taxpayer Issuer { get; set; }

		[Property("issued_location")]
		[Display(Name = "IssuedLocation", ResourceType = typeof(Resources))]
		public virtual string IssuedLocation { get; set; }

		[BelongsTo("issued_from", Lazy = FetchWhen.OnInvoke)]
		[Display(Name = "IssuedFrom", ResourceType = typeof(Resources))]
		public virtual Address IssuedFrom { get; set; }
		
		[BelongsTo("issued_at", Lazy = FetchWhen.OnInvoke)]
		[Display(Name = "IssuedAt", ResourceType = typeof(Resources))]
		public virtual Address IssuedAt { get; set; }
        
		[Property]
		[Display(Name = "IssueDate", ResourceType = typeof(Resources))]
		public virtual DateTime? Issued { get; set; }
		
		[Property]
        [Display(Name = "Type", ResourceType = typeof(Resources))]
        public virtual FiscalDocumentType Type { get; set; }
        
		[Property]
		[Display(Name = "Batch", ResourceType = typeof(Resources))]
		public virtual string Batch { get; set; }

		[Property]
		[Display(Name = "Serial", ResourceType = typeof(Resources))]
		[DisplayFormat(DataFormatString="{0:D6}")]
		public virtual int? Serial { get; set; }
		
		[Property("approval_number")]
		[Display(Name = "ApprovalNumber", ResourceType = typeof(Resources))]
		public virtual int? ApprovalNumber { get; set; }
		
		[Property("approval_year")]
		[Display(Name = "ApprovalYear", ResourceType = typeof(Resources))]
		public virtual int? ApprovalYear { get; set; }
		
		[Property("certificate_number")]
		[Display(Name = "CertificateNumber", ResourceType = typeof(Resources))]
		[DisplayFormat(DataFormatString="{0:00000000000000000000}")]
		public virtual decimal? CertificateNumber { get; set; }
		
		[Property("original_string")]
		[Display(Name = "OriginalString", ResourceType = typeof(Resources))]
		public virtual string OriginalString { get; set; }
		
		[Property("digital_seal")]
		[Display(Name = "DigitalSeal", ResourceType = typeof(Resources))]
		public virtual string DigitalSeal { get; set; }
		
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[Display(Name = "BillTo", ResourceType = typeof(Resources))]
		[UIHint("AddressSelector")]
		public virtual int BillToId { get; set; }
		
        [BelongsTo("bill_to", Lazy = FetchWhen.OnInvoke)]
        [Display(Name = "BillTo", ResourceType = typeof(Resources))]
        public virtual Address BillTo { get; set; }

        [Property("payment_method")]
        [Display(Name = "PaymentMethod", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        public virtual PaymentMethod PaymentMethod { get; set; }
		
        [Property("payment_reference")]
		[Display(Name = "PaymentReference", ResourceType = typeof(Resources))]
		[StringLength(25, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public virtual string PaymentReference { get; set; }

		[Property()]
		[Display(Name = "Reference", ResourceType = typeof(Resources))]
		[StringLength(25, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public virtual string Reference { get; set; }
		
        [Property("completed")]
		[Display(Name = "Completed", ResourceType = typeof(Resources))]
		public virtual bool IsCompleted { get; set; }

        [Property("cancelled")]
		[Display(Name = "Cancelled", ResourceType = typeof(Resources))]
		public virtual bool IsCancelled { get; set; }

        [Property("cancellation_date")]
		[Display(Name = "CancellationDate", ResourceType = typeof(Resources))]		
		public virtual DateTime? CancellationDate { get; set; }
		
		[Property()]
		public virtual decimal Version { get; set; }

        [HasMany(typeof(FiscalDocumentDetail), Table = "fiscal_document_detail", ColumnKey = "document", Lazy = true)]
		public virtual IList<FiscalDocumentDetail> Details {
			get { return details; }
			set { details = value; }
		}
		
		[DataType(DataType.Currency)]
		[Display(Name = "Subtotal", ResourceType = typeof(Resources))]
		public virtual decimal Subtotal {
			get { return Details.Sum (x => x.Subtotal); }
		}

		[DataType(DataType.Currency)]
		[Display(Name = "Taxes", ResourceType = typeof(Resources))]
		public virtual decimal Taxes {
			get { return Total - Subtotal; }
		}

		[DataType(DataType.Currency)]
		[Display(Name = "Total", ResourceType = typeof(Resources))]
		public virtual decimal Total {
			get { return Details.Sum (x => x.Total); }
		}
	}
}
