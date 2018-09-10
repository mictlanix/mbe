// 
// FiscalDocumentRelation.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2018 Mictlanix SAS de CV and contributors.
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
	[ActiveRecord ("fiscal_document_relation")]
	public class FiscalDocumentRelation : ActiveRecordLinqBase<FiscalDocumentRelation> {
		public FiscalDocumentRelation ()
		{
		}

		[PrimaryKey (PrimaryKeyType.Identity, "fiscal_document_relation_id")]
		public virtual int Id { get; set; }

		[BelongsTo ("document")]
		[Display (Name = "FiscalDocument", ResourceType = typeof (Resources))]
		public virtual FiscalDocument Document { get; set; }

		[BelongsTo ("relation")]
		[Display (Name = "FiscalDocumentRelation", ResourceType = typeof (Resources))]
		public virtual FiscalDocument Relation { get; set; }

		[Property ("exchange_rate")]
		[DisplayFormat (DataFormatString = "{0:0.00##}")]
		[Display (Name = "ExchangeRate", ResourceType = typeof (Resources))]
		public virtual decimal ExchangeRate { get; set; }

		[Property]
		[Display (Name = "InstallmentPayment", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof (Resources))]
		public virtual int Installment { get; set; }

		[Property ("previous_balance")]
		[Display (Name = "PreviousBalance", ResourceType = typeof (Resources))]
		[DataType (DataType.Currency)]
		[Required (ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof (Resources))]
		public virtual decimal PreviousBalance { get; set; }

		[Property ("amount")]
		[Display (Name = "Amount", ResourceType = typeof (Resources))]
		[DataType (DataType.Currency)]
		[Required (ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof (Resources))]
		public virtual decimal Amount { get; set; }

		[DataType (DataType.Currency)]
		[Display (Name = "OutstandingBalance", ResourceType = typeof (Resources))]
		public decimal AmountEx {
			get { return Amount / ExchangeRate; }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "OutstandingBalance", ResourceType = typeof (Resources))]
		public decimal OutstandingBalance {
			get { return PreviousBalance - Amount; }
		}
	}
}
