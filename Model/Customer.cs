// 
// Customer.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
//   Eduardo Nieto <enieto@mictlanix.com>
// 
// Copyright (C) 2011-2013 Eddy Zavaleta, Mictlanix, and contributors.
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
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Mictlanix.BE.Model {
	[ActiveRecord ("customer", Lazy = true)]
	public class Customer : ActiveRecordLinqBase<Customer> {
		IList<Address> addresses = new List<Address> ();
		IList<Contact> contacts = new List<Contact> ();
		IList<TaxpayerRecipient> taxpayers = new List<TaxpayerRecipient> ();
		IList<CustomerDiscount> discounts = new List<CustomerDiscount> ();

		[PrimaryKey (PrimaryKeyType.Identity, "customer_id")]
		public virtual int Id { get; set; }

		[Property]
		[ValidateIsUnique]
		[Display (Name = "Code", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[RegularExpression (@"^\S+$", ErrorMessageResourceName = "Validation_NonWhiteSpace", ErrorMessageResourceType = typeof (Resources))]
		[StringLength (25, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string Code { get; set; }

		[Property]
		[Display (Name = "Name", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[StringLength (250, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string Name { get; set; }

		[Property]
		[Display (Name = "Zone", ResourceType = typeof (Resources))]
		[StringLength (250, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string Zone { get; set; }

		[Property ("credit_limit")]
		[DataType (DataType.Currency)]
		[Display (Name = "CreditLimit", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		public virtual decimal CreditLimit { get; set; }

		[Property ("credit_days")]
		[Display (Name = "CreditDays", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		public virtual int CreditDays { get; set; }

		public virtual bool HasCredit {
			get { return CreditDays > 0 && CreditLimit > 0; }
		}

		[Property]
		[DataType (DataType.MultilineText)]
		[Display (Name = "Comment", ResourceType = typeof (Resources))]
		[StringLength (1024, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string Comment { get; set; }

		[Display (Name = "PriceList", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[UIHint ("PriceListSelector")]
		public virtual int PriceListId { get; set; }

		[BelongsTo ("price_list")]
		[Display (Name = "PriceList", ResourceType = typeof (Resources))]
		public virtual PriceList PriceList { get; set; }

		[Property]
		[Display (Name = "ShippingRequired", ResourceType = typeof (Resources))]
		public virtual bool Shipping { get; set; }

		[Property ("shipping_required_document")]
		[Display (Name = "ShippingInvoiceRequired", ResourceType = typeof (Resources))]
		public virtual bool ShippingRequiredDocument { get; set; }

		[Display (Name = "SalesPerson", ResourceType = typeof (Resources))]
		[UIHint ("EmployeeSelector")]
		public virtual int SalesPersonId { get; set; }

		[BelongsTo("salesperson")]
		[Display(Name = "SalesPerson", ResourceType = typeof(Resources))]
		public virtual Employee SalesPerson { get; set; }

		[HasAndBelongsToMany (typeof (Address), Table = "customer_address", ColumnKey = "customer", ColumnRef = "address", Lazy = true)]
		public virtual IList<Address> Addresses {
			get { return addresses; }
			set { addresses = value; }
		}

		[HasAndBelongsToMany (typeof (Contact), Table = "customer_contact", ColumnKey = "customer", ColumnRef = "contact", Lazy = true)]
		public virtual IList<Contact> Contacts {
			get { return contacts; }
			set { contacts = value; }
		}

		[HasAndBelongsToMany (typeof (TaxpayerRecipient), Table = "customer_taxpayer", ColumnKey = "customer", ColumnRef = "taxpayer", Lazy = true)]
		public virtual IList<TaxpayerRecipient> Taxpayers {
			get { return taxpayers; }
			set { taxpayers = value; }
		}

		[HasMany (typeof (CustomerDiscount), Table = "customer_discount", ColumnKey = "customer", Lazy = true)]
		public virtual IList<CustomerDiscount> Discounts {
			get { return discounts; }
			set { discounts = value; }
		}

		#region Override Base Methods

		public override string ToString ()
		{
			return string.Format ("{0}", Name);
		}

		public override bool Equals (object obj)
		{
			Customer other = obj as Customer;

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
