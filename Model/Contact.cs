// 
// Contact.cs
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
using System.ComponentModel.DataAnnotations;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Mictlanix.BE.Model {
	[ActiveRecord ("contact")]
	public class Contact : ActiveRecordLinqBase<Contact> {
		IList<Supplier> suppliers = new List<Supplier> ();
		IList<Customer> customers = new List<Customer> ();

		[PrimaryKey (PrimaryKeyType.Identity, "contact_id")]
		public int Id { get; set; }

		[Property]
		[Display (Name = "Name", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[StringLength (250, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string Name { get; set; }

		[Property ("job_title")]
		[Display (Name = "JobTitle", ResourceType = typeof (Resources))]
		[StringLength (100, MinimumLength = 3, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string JobTitle { get; set; }

		[Property]
		[DataType (DataType.EmailAddress)]
		[EmailAddress (ErrorMessageResourceName = "Validation_Email", ErrorMessageResourceType = typeof (Resources), ErrorMessage = null)]
		[Display (Name = "Email", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[StringLength (80, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string Email { get; set; }

		[Property]
		[Display (Name = "Phone", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[StringLength (25, MinimumLength = 8, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string Phone { get; set; }

		[Property ("phone_ext")]
		[Display (Name = "PhoneExt", ResourceType = typeof (Resources))]
		[StringLength (5, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string PhoneExt { get; set; }

		[Property]
		[Display (Name = "Mobile", ResourceType = typeof (Resources))]
		[StringLength (25, MinimumLength = 8, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string Mobile { get; set; }

		[Property]
		[Display (Name = "Fax", ResourceType = typeof (Resources))]
		[StringLength (25, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string Fax { get; set; }

		[Property]
		[DataType (DataType.Url)]
		[Url (ErrorMessageResourceName = "Validation_Url", ErrorMessageResourceType = typeof (Resources), ErrorMessage = null)]
		[Display (Name = "Website", ResourceType = typeof (Resources))]
		[StringLength (80, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string Website { get; set; }

		[Property]
		[Display (Name = "Im", ResourceType = typeof (Resources))]
		[StringLength (80, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string Im { get; set; }

		[Property]
		[Display (Name = "Sip", ResourceType = typeof (Resources))]
		[StringLength (80, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string Sip { get; set; }

		[Property]
		[DataType (DataType.Date)]
		[Display (Name = "Birthday", ResourceType = typeof (Resources))]
		public DateTime? Birthday { get; set; }

		[Property]
		[DataType (DataType.MultilineText)]
		[Display (Name = "Comment", ResourceType = typeof (Resources))]
		[StringLength (500, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string Comment { get; set; }

		[HasAndBelongsToMany (typeof (Supplier), Table = "supplier_contact", ColumnKey = "contact", ColumnRef = "supplier", Inverse = true, Lazy = true)]
		public IList<Supplier> Suppliers {
			get { return suppliers; }
			set { suppliers = value; }
		}

		[HasAndBelongsToMany (typeof (Customer), Table = "customer_contact", ColumnKey = "contact", ColumnRef = "customer", Inverse = true, Lazy = true)]
		public IList<Customer> Customers {
			get { return customers; }
			set { customers = value; }
		}

		#region Override Base Methods

		public override string ToString ()
		{
			return string.Format ("{0}", Name);
		}

		public override bool Equals (object obj)
		{
			Contact other = obj as Contact;

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