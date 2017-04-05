// 
// Address.cs
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
	[ActiveRecord ("address", Lazy = true)]
	public class Address : ActiveRecordLinqBase<Address> {
		IList<Supplier> suppliers = new List<Supplier> ();
		IList<Customer> customers = new List<Customer> ();

		public Address ()
		{
		}

		[PrimaryKey (PrimaryKeyType.Identity, "address_id")]
		public virtual int Id { get; set; }

		[Property]
		[Display (Name = "Type", ResourceType = typeof (Resources))]
		public virtual AddressType Type { get; set; }

		[Property]
		[Display (Name = "Street", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[StringLength (150, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string Street { get; set; }

		[Property ("exterior_number")]
		[Display (Name = "ExteriorNumber", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[StringLength (25, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string ExteriorNumber { get; set; }

		[Property ("interior_number")]
		[Display (Name = "InteriorNumber", ResourceType = typeof (Resources))]
		[StringLength (25, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string InteriorNumber { get; set; }

		[Property ("postal_code")]
		[Display (Name = "PostalCode", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[RegularExpression (@"^\d{5}$", ErrorMessageResourceName = "Validation_DigitsOnly", ErrorMessageResourceType = typeof (Resources))]
		public virtual string PostalCode { get; set; }

		[Property]
		[Display (Name = "Neighborhood", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[StringLength (100, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string Neighborhood { get; set; }

		[Property]
		[Display (Name = "Locality", ResourceType = typeof (Resources))]
		[StringLength (100, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string Locality { get; set; }

		[Property]
		[Display (Name = "Borough", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[StringLength (50, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string Borough { get; set; }

		[Property]
		[Display (Name = "State", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[StringLength (50, MinimumLength = 2, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string State { get; set; }

		[Property]
		[Display (Name = "City", ResourceType = typeof (Resources))]
		[StringLength (50, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string City { get; set; }

		[Property]
		[Display (Name = "Country", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[StringLength (50, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string Country { get; set; }

		[Property]
		[DataType (DataType.MultilineText)]
		[Display (Name = "Comment", ResourceType = typeof (Resources))]
		[StringLength (500, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string Comment { get; set; }

		[HasAndBelongsToMany (typeof (Supplier), Table = "supplier_address", ColumnKey = "address", ColumnRef = "supplier", Inverse = true, Lazy = true)]
		public virtual IList<Supplier> Suppliers {
			get { return suppliers; }
			set { suppliers = value; }
		}

		[HasAndBelongsToMany (typeof (Customer), Table = "customer_address", ColumnKey = "address", ColumnRef = "customer", Inverse = true, Lazy = true)]
		public virtual IList<Customer> Customers {
			get { return customers; }
			set { customers = value; }
		}

		[Display (Name = "Street", ResourceType = typeof (Resources))]
		public virtual string StreetAndNumber {
			get {
				var fmt = string.IsNullOrWhiteSpace (InteriorNumber) ? "{0} {1}" : "{0} {1} - {2}";

				return string.Format (fmt, Street, ExteriorNumber, InteriorNumber).Trim ();
			}
		}

		#region Override Base Methods

		public override string ToString ()
		{
			return string.Format ("{0}, {1}, {2}", StreetAndNumber, State, PostalCode);
		}

		public override bool Equals (object obj)
		{
			Address other = obj as Address;

			if (other == null)
				return false;

			return Id == other.Id &&
				Street == other.Street &&
				ExteriorNumber == other.ExteriorNumber &&
				InteriorNumber == other.InteriorNumber &&
				PostalCode == other.PostalCode &&
				Neighborhood == other.Neighborhood &&
				Locality == other.Locality &&
				Borough == other.Borough &&
				State == other.State &&
				City == other.City &&
				Country == other.Country &&
				Comment == other.Comment;
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