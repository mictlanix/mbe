// 
// CustomerTaxpayer.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2017 Mictlanix SAS de CV and contributors.
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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Mictlanix.BE.Model {
	[ActiveRecord ("taxpayer_recipient", Lazy = true)]
	public class TaxpayerRecipient : ActiveRecordLinqBase<TaxpayerRecipient> {
		public TaxpayerRecipient ()
		{
		}

		[PrimaryKey ("taxpayer_recipient_id")]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[StringLength (13, MinimumLength = 12, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "TaxpayerId", ResourceType = typeof (Resources))]
		public virtual string Id { get; set; }

		[Property]
		[StringLength (250, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "TaxpayerName", ResourceType = typeof (Resources))]
		public virtual string Name { get; set; }

		[Property]
		[DataType (DataType.EmailAddress)]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[EmailAddress (ErrorMessageResourceName = "Validation_Email", ErrorMessageResourceType = typeof (Resources), ErrorMessage = null)]
		[StringLength (80, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "Email", ResourceType = typeof (Resources))]
		public virtual string Email { get; set; }

		[Property ("regime", Update = false, Insert = false)]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "TaxRegime", ResourceType = typeof (Resources))]
		[UIHint ("TaxRegimeSelector")]
		public virtual string RegimeId { get; set; }

		[BelongsTo ("regime")]
		[Display (Name = "TaxRegime", ResourceType = typeof (Resources))]
		public virtual SatTaxRegime Regime { get; set; }

		[Property ("postal_code")]
		[Display (Name = "PostalCodeFiscal", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[RegularExpression (@"^\d{5}$", ErrorMessageResourceName = "Validation_DigitsOnly", ErrorMessageResourceType = typeof (Resources))]
		public virtual string PostalCode { get; set; }

		#region Override Base Methods

		public override string ToString ()
		{
			if (string.IsNullOrWhiteSpace (Name)) {
				return Id;
			}

			return string.Format ("{0} ({1})", Id, Name);
		}

		public override bool Equals (object obj)
		{
			var other = obj as TaxpayerRecipient;

			if (other == null)
				return false;

			if (string.IsNullOrEmpty (Id) && string.IsNullOrEmpty (other.Id))
				return (object) this == other;
			else
				return Id == other.Id;
		}

		public override int GetHashCode ()
		{
			if (string.IsNullOrEmpty (Id))
				return base.GetHashCode ();

			return string.Format ("{0}#{1}", GetType ().FullName, Id).GetHashCode ();
		}

		#endregion
	}
}
