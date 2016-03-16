// 
// Notarization.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2014 Eddy Zavaleta, Mictlanix, and contributors.
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
	[ActiveRecord ("notarization")]
	public class Notarization : ActiveRecordLinqBase<Notarization> {
		[PrimaryKey (PrimaryKeyType.Identity, "notarization_id")]
		[Display (Name = "Id", ResourceType = typeof (Resources))]
		[DisplayFormat (DataFormatString = "{0:D8}")]
		public int Id { get; set; }

		[Property]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[DataType (DataType.Date)]
		[Display (Name = "Date", ResourceType = typeof (Resources))]
		public virtual DateTime Date { get; set; }

		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "Requester", ResourceType = typeof (Resources))]
		[UIHint ("EmployeeSelector")]
		public virtual int RequesterId { get; set; }

		[BelongsTo ("requester")]
		[Display (Name = "Requester", ResourceType = typeof (Resources))]
		public virtual Employee Requester { get; set; }

		[Property ("notary_office")]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "NotaryOffice", ResourceType = typeof (Resources))]
		[StringLength (256, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string NotaryOffice { get; set; }

		[Property ("document_description")]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[DataType (DataType.MultilineText)]
		[Display (Name = "DocumentDescription", ResourceType = typeof (Resources))]
		[StringLength (512, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string DocumentDescription { get; set; }

		[Property]
		[DataType (DataType.Currency)]
		[Range (0.0001, double.MaxValue, ErrorMessageResourceName = "Validation_CannotBeZeroOrNegative", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "Amount", ResourceType = typeof (Resources))]
		public decimal Amount { get; set; }

		[Property ("payment_date")]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[DataType (DataType.Date)]
		[Display (Name = "PaymentDate", ResourceType = typeof (Resources))]
		public virtual DateTime PaymentDate { get; set; }

		[Property ("delivery_date")]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[DataType (DataType.Date)]
		[Display (Name = "DeliveryDate", ResourceType = typeof (Resources))]
		public virtual DateTime DeliveryDate { get; set; }

		[Property]
		[DataType (DataType.MultilineText)]
		[Display (Name = "Comment", ResourceType = typeof (Resources))]
		[StringLength (1024, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string Comment { get; set; }

		#region Override Base Methods

		public override string ToString ()
		{
			return string.Format ("[Notarization: Id={0}, Date={1}, RequesterId={2}, Requester={3}, NotaryOffice={4}, DocumentDescription={5}, Amount={6}, PaymentDate={7}, DeliveryDate={8}, Comment={9}]", Id, Date, RequesterId, Requester, NotaryOffice, DocumentDescription, Amount, PaymentDate, DeliveryDate, Comment);
		}

		public override bool Equals (object obj)
		{
			var other = obj as TranslationRequest;

			if (other == null)
				return false;

			if (Id == 0 && other.Id == 0)
				return (object) this == other;
			else
				return Id == other.Id;
		}

		public override int GetHashCode ()
		{
			if (Id == 0)
				return base.GetHashCode ();

			return string.Format ("{0}#{1}", GetType ().FullName, Id).GetHashCode ();
		}

		#endregion
	}
}
