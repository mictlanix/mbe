// 
// FiscalDocument.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
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

namespace Mictlanix.BE.Model {
	[ActiveRecord ("fiscal_document", Lazy = true)]
	public class FiscalDocument : ActiveRecordLinqBase<FiscalDocument> {
		IList<FiscalDocumentDetail> details = new List<FiscalDocumentDetail> ();
		IList<FiscalDocumentRelation> relations = new List<FiscalDocumentRelation> ();

		public FiscalDocument ()
		{
		}

		[PrimaryKey (PrimaryKeyType.Identity, "fiscal_document_id")]
		[Display (Name = "FiscalDocumentId", ResourceType = typeof (Resources))]
		[DisplayFormat (DataFormatString = "{0:D6}")]
		public virtual int Id { get; set; }

		[BelongsTo ("store", Fetch = FetchEnum.Join)]
		[Display (Name = "Store", ResourceType = typeof (Resources))]
		public virtual Store Store { get; set; }

		[Property ("creation_time")]
		[DataType (DataType.DateTime)]
		[Display (Name = "CreationTime", ResourceType = typeof (Resources))]
		public virtual DateTime CreationTime { get; set; }

		[Property ("modification_time")]
		[DataType (DataType.DateTime)]
		[Display (Name = "ModificationTime", ResourceType = typeof (Resources))]
		public virtual DateTime ModificationTime { get; set; }

		[BelongsTo ("creator", Lazy = FetchWhen.OnInvoke)]
		[Display (Name = "Creator", ResourceType = typeof (Resources))]
		public virtual Employee Creator { get; set; }

		[BelongsTo ("updater", Lazy = FetchWhen.OnInvoke)]
		[Display (Name = "Updater", ResourceType = typeof (Resources))]
		public virtual Employee Updater { get; set; }

		[Property]
		[DataType (DataType.MultilineText)]
		[Display (Name = "Comment", ResourceType = typeof (Resources))]
		[StringLength (500, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string Comment { get; set; }

		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "Issuer", ResourceType = typeof (Resources))]
		[UIHint ("TaxpayerSelector")]
		public virtual string IssuerId { get; set; }

		[BelongsTo ("issuer")]
		[Display (Name = "Issuer", ResourceType = typeof (Resources))]
		public virtual TaxpayerIssuer Issuer { get; set; }

		[Property ("issuer_name")]
		[Display (Name = "IssuerName", ResourceType = typeof (Resources))]
		public virtual string IssuerName { get; set; }

		[BelongsTo ("issuer_regime")]
		[Display (Name = "TaxRegime", ResourceType = typeof (Resources))]
		public virtual SatTaxRegime IssuerRegime { get; set; }

		[Property ("issuer_regime_name")]
		[Display (Name = "TaxRegime", ResourceType = typeof (Resources))]
		public virtual string IssuerRegimeName { get; set; }

		[BelongsTo ("issuer_address", Lazy = FetchWhen.OnInvoke)]
		[Display (Name = "Address", ResourceType = typeof (Resources))]
		public virtual Address IssuerAddress { get; set; }

		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "Customer", ResourceType = typeof (Resources))]
		[UIHint ("CustomerSelector")]
		public virtual int CustomerId { get; set; }

		[BelongsTo ("customer")]
		[Display (Name = "Customer", ResourceType = typeof (Resources))]
		public virtual Customer Customer { get; set; }

		[Property ("recipient")]
		[Display (Name = "Recipient", ResourceType = typeof (Resources))]
		public virtual string Recipient { get; set; }

		[Property ("recipient_name")]
		[Display (Name = "RecipientName", ResourceType = typeof (Resources))]
		public virtual string RecipientName { get; set; }

		[BelongsTo ("`usage`")]
		[Display (Name = "CfdiUsage", ResourceType = typeof (Resources))]
		public virtual SatCfdiUsage Usage { get; set; }

		[BelongsTo ("recipient_address", Lazy = FetchWhen.OnInvoke)]
		[Display (Name = "Address", ResourceType = typeof (Resources))]
		public virtual Address RecipientAddress { get; set; }

		[Property]
		[Display (Name = "IssueDate", ResourceType = typeof (Resources))]
		public virtual DateTime? Issued { get; set; }

		[BelongsTo ("issued_at", Lazy = FetchWhen.OnInvoke)]
		[Display (Name = "IssuedAt", ResourceType = typeof (Resources))]
		public virtual Address IssuedAt { get; set; }

		[Property ("issued_location")]
		[Display (Name = "IssuedLocation", ResourceType = typeof (Resources))]
		public virtual string IssuedLocation { get; set; }

		[Property]
		[Display (Name = "Type", ResourceType = typeof (Resources))]
		public virtual FiscalDocumentType Type { get; set; }

		[Property]
		[Display (Name = "Currency", ResourceType = typeof (Resources))]
		public virtual CurrencyCode Currency { get; set; }

		[Property ("exchange_rate")]
		[DisplayFormat (DataFormatString = "{0:0.00##}")]
		[Display (Name = "ExchangeRate", ResourceType = typeof (Resources))]
		public virtual decimal ExchangeRate { get; set; }

		[Property ("payment_terms")]
		[Display (Name = "PaymentTerms", ResourceType = typeof (Resources))]
		public virtual PaymentTerms Terms { get; set; }

		[Property]
		[Display (Name = "Batch", ResourceType = typeof (Resources))]
		public virtual string Batch { get; set; }

		[Property]
		[Display (Name = "Serial", ResourceType = typeof (Resources))]
		[DisplayFormat (DataFormatString = "{0:D6}")]
		public virtual int? Serial { get; set; }

		[Property ("approval_number")]
		[Display (Name = "ApprovalNumber", ResourceType = typeof (Resources))]
		public virtual int? ApprovalNumber { get; set; }

		[Property ("approval_year")]
		[Display (Name = "ApprovalYear", ResourceType = typeof (Resources))]
		public virtual int? ApprovalYear { get; set; }

		[Property ("issuer_certificate_number")]
		[Display (Name = "CertificateNumber", ResourceType = typeof (Resources))]
		public virtual string IssuerCertificateNumber { get; set; }

		[Property ("original_string")]
		[UIHint ("Breakable")]
		[Display (Name = "OriginalString", ResourceType = typeof (Resources))]
		public virtual string OriginalString { get; set; }

		[Property ("stamp_uuid")]
		[Display (Name = "StampId", ResourceType = typeof (Resources))]
		public virtual string StampId { get; set; }

		[Property]
		[Display (Name = "StampDate", ResourceType = typeof (Resources))]
		public virtual DateTime? Stamped { get; set; }

		[Property ("issuer_digital_seal")]
		[UIHint ("Breakable")]
		[Display (Name = "IssuerDigitalSeal", ResourceType = typeof (Resources))]
		public virtual string IssuerDigitalSeal { get; set; }

		[Property ("authority_digital_seal")]
		[UIHint ("Breakable")]
		[Display (Name = "AuthorityDigitalSeal", ResourceType = typeof (Resources))]
		public virtual string AuthorityDigitalSeal { get; set; }

		[Property ("authority_certificate_number")]
		[Display (Name = "AuthorityCertificateNumber", ResourceType = typeof (Resources))]
		public virtual string AuthorityCertificateNumber { get; set; }

		[Property ("payment_method")]
		[Display (Name = "PaymentMethod", ResourceType = typeof (Resources))]
		public virtual PaymentMethod PaymentMethod { get; set; }

		[Property ("payment_reference")]
		[Display (Name = "PaymentReference", ResourceType = typeof (Resources))]
		[StringLength (25, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string PaymentReference { get; set; }

		[Property ()]
		[Display (Name = "Reference", ResourceType = typeof (Resources))]
		[StringLength (25, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string Reference { get; set; }

		[Property ("completed")]
		[Display (Name = "Completed", ResourceType = typeof (Resources))]
		public virtual bool IsCompleted { get; set; }

		[Property ("cancelled")]
		[Display (Name = "Cancelled", ResourceType = typeof (Resources))]
		public virtual bool IsCancelled { get; set; }

		[Property ("cancellation_date")]
		[Display (Name = "CancellationDate", ResourceType = typeof (Resources))]
		public virtual DateTime? CancellationDate { get; set; }

		[Property]
		public virtual decimal Version { get; set; }

		[Property]
		public virtual FiscalCertificationProvider Provider { get; set; }

		[Property ("retention_rate")]
		[DisplayFormat (DataFormatString = "{0:p}")]
		[Display (Name = "RetentionRate", ResourceType = typeof (Resources))]
		public virtual decimal RetentionRate { get; set; }

		[Property ("local_retention_name")]
		public virtual string LocalRetentionName { get; set; }

		[Property ("local_retention_rate")]
		[DisplayFormat (DataFormatString = "{0:p}")]
		[Display (Name = "LocalRetentionRate", ResourceType = typeof (Resources))]
		public virtual decimal LocalRetentionRate { get; set; }

		[Property ("payment_date")]
		[DataType (DataType.Date)]
		[Display (Name = "PaymentDate", ResourceType = typeof (Resources))]
		public virtual DateTime? PaymentDate { get; set; }

		[Property ("payment_amount")]
		[DataType (DataType.Currency)]
		[Display (Name = "PaymentAmount", ResourceType = typeof (Resources))]
		public virtual decimal PaymentAmount { get; set; }

		[HasMany (typeof (FiscalDocumentDetail), Table = "fiscal_document_detail", ColumnKey = "document", Lazy = true)]
		public virtual IList<FiscalDocumentDetail> Details {
			get { return details; }
			set { details = value; }
		}

		[HasMany (typeof (FiscalDocumentRelation), Table = "fiscal_document_relation", ColumnKey = "document", Lazy = true)]
		public virtual IList<FiscalDocumentRelation> Relations {
			get { return relations; }
			set { relations = value; }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Subtotal", ResourceType = typeof (Resources))]
		public virtual decimal Subtotal {
			get { return Details.Sum (x => x.Subtotal); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Discount", ResourceType = typeof (Resources))]
		public virtual decimal Discount {
			get { return Details.Sum (x => x.Discount); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Taxes", ResourceType = typeof (Resources))]
		public virtual decimal Taxes {
			get { return Details.Sum (x => x.Taxes); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "RetentionTaxes", ResourceType = typeof (Resources))]
		public virtual decimal RetentionTaxes {
			get { return Details.Sum (x => x.RetentionTaxes); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "LocalRetentionTaxes", ResourceType = typeof (Resources))]
		public virtual decimal LocalRetentionTaxes {
			get { return Details.Sum (x => x.LocalRetentionTaxes); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Total", ResourceType = typeof (Resources))]
		public virtual decimal Total {
			get { return Details.Sum (x => x.Total) - RetentionTaxes - LocalRetentionTaxes; }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Subtotal", ResourceType = typeof (Resources))]
		public virtual decimal SubtotalEx {
			get { return Details.Sum (x => x.SubtotalEx); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Discount", ResourceType = typeof (Resources))]
		public virtual decimal DiscountEx {
			get { return Details.Sum (x => x.DiscountEx); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Taxes", ResourceType = typeof (Resources))]
		public virtual decimal TaxesEx {
			get { return Details.Sum (x => x.TaxesEx); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "RetentionTaxes", ResourceType = typeof (Resources))]
		public virtual decimal RetentionTaxesEx {
			get { return ModelHelpers.TotalRounding ((SubtotalEx - DiscountEx) * RetentionRate); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "LocalRetentionTaxes", ResourceType = typeof (Resources))]
		public virtual decimal LocalRetentionTaxesEx {
			get { return ModelHelpers.TotalRounding ((SubtotalEx - DiscountEx) * LocalRetentionRate); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Total", ResourceType = typeof (Resources))]
		public virtual decimal TotalEx {
			get { return Details.Sum (x => x.TotalEx) - RetentionTaxesEx - LocalRetentionTaxesEx; }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "PaymentAmount", ResourceType = typeof (Resources))]
		public virtual decimal PaymentAmountEx {
			get { return ModelHelpers.TotalRounding (PaymentAmount * ExchangeRate); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Paid", ResourceType = typeof (Resources))]
		public virtual decimal Paid {
			get { return ModelHelpers.TotalRounding (Relations.Sum (x => x.AmountEx)); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Paid", ResourceType = typeof (Resources))]
		public virtual decimal PaidEx {
			get { return ModelHelpers.TotalRounding (Relations.Sum (x => x.AmountEx) * ExchangeRate); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Balance", ResourceType = typeof (Resources))]
		public virtual decimal Balance {
			get { return PaymentAmount - Paid; }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Balance", ResourceType = typeof (Resources))]
		public virtual decimal BalanceEx {
			get { return PaymentAmountEx - PaidEx; }
		}
	}
}
