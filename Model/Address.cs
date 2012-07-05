// 
// Address.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.org>
//   Eduardo Nieto <enieto@mictlanix.org>
// 
// Copyright (C) 2011 Eddy Zavaleta, Mictlanix, and contributors.
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

namespace Mictlanix.BE.Model
{
    [ActiveRecord("address", Lazy = true)]
    public class Address : ActiveRecordLinqBase<Address>
    {
        IList<Supplier> suppliers = new List<Supplier>();
        IList<Customer> customers = new List<Customer>();
		
		public Address ()
		{
		}
		
		public Address (Address item)
		{
			Copy (item);
		}
		
        [PrimaryKey(PrimaryKeyType.Identity, "address_id")]
		public virtual int Id { get; set; }

        [Property("taxpayer_id")]
		[Display(Name = "TaxpayerId", ResourceType = typeof(Resources))]
		[StringLength(13, MinimumLength = 12, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public virtual string TaxpayerId { get; set; }

        [Property("taxpayer_name")]
		[Display(Name = "TaxpayerName", ResourceType = typeof(Resources))]
		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[StringLength(250, MinimumLength = 3, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public virtual string TaxpayerName { get; set; }
		
        [Property("taxpayer_regime")]
		[Display(Name = "TaxRegime", ResourceType = typeof(Resources))]
		[StringLength(250, MinimumLength = 3, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public virtual string TaxpayerRegime { get; set; }

        [Property]
		[Display(Name = "Street", ResourceType = typeof(Resources))]
		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[StringLength(250, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public virtual string Street { get; set; }

        [Property("exterior_number")]
		[Display(Name = "ExteriorNumber", ResourceType = typeof(Resources))]
		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[StringLength(15, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public virtual string ExteriorNumber { get; set; }

        [Property("interior_number")]
		[Display(Name = "InteriorNumber", ResourceType = typeof(Resources))]
		[StringLength(15, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public virtual string InteriorNumber { get; set; }

        [Property]
		[Display(Name = "Neighborhood", ResourceType = typeof(Resources))]
		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[StringLength(150, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public virtual string Neighborhood { get; set; }

        [Property]
		[Display(Name = "Locality", ResourceType = typeof(Resources))]
		[StringLength(150, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public virtual string Locality { get; set; }

        [Property]
		[Display(Name = "Borough", ResourceType = typeof(Resources))]
		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[StringLength(150, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public virtual string Borough { get; set; }

        [Property]
		[Display(Name = "State", ResourceType = typeof(Resources))]
		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[StringLength(80, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public virtual string State { get; set; }

        [Property]
		[Display(Name = "Country", ResourceType = typeof(Resources))]
		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[StringLength(80, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public virtual string Country { get; set; }

        [Property("zip_code")]
		[Display(Name = "ZipCode", ResourceType = typeof(Resources))]
		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[StringLength(5, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public virtual string ZipCode { get; set; }

        [Property]
		[DataType(DataType.MultilineText)]
		[Display(Name = "Comment", ResourceType = typeof(Resources))]
		[StringLength(500, MinimumLength = 0)]
		public virtual string Comment { get; set; }
		
        [HasAndBelongsToMany(typeof(Supplier), Table = "supplier_address", ColumnKey = "address", ColumnRef = "supplier", Inverse = true, Lazy = true)]
		public virtual IList<Supplier> Suppliers
        {
            get { return suppliers; }
            set { suppliers = value; }
        }

        [HasAndBelongsToMany(typeof(Customer), Table = "customer_address", ColumnKey = "address", ColumnRef = "customer", Inverse = true, Lazy = true)]
		public virtual IList<Customer> Customers
        {
            get { return customers; }
            set { customers = value; }
        }
		
        public virtual string StreetAndNumber {
			get { return string.Format ("{0} {1} {2}", Street, ExteriorNumber, InteriorNumber).Trim (); }
		}
		
		public virtual void Copy (Address item)
		{
			TaxpayerId = item.TaxpayerId;
			TaxpayerName = item.TaxpayerName;
			Street = item.Street;
			ExteriorNumber = item.ExteriorNumber;
			InteriorNumber = item.InteriorNumber;
			Neighborhood = item.Neighborhood;
			Locality = item.Locality;
			Borough = item.Borough;
			State = item.State;
			Country = item.Country;
			ZipCode = item.ZipCode;
		}
		
		#region Override Base Methods

        public override string ToString()
        {
            return string.Format("{0} [{1}, {2}, {3}]", TaxpayerName, Street, Neighborhood, ZipCode);
        }

		public override bool Equals(object obj)
		{
		    Address other = obj as Address;
			
		    if (other == null)
				return false;
			
		    if (Id == 0 && other.Id == 0)
		        return (object)this == other;
		    else
		        return Id == other.Id;
		}
		
		public override int GetHashCode()
		{
		    if (Id == 0)
				return base.GetHashCode();
			
		    return string.Format("{0}#{1}", GetType().FullName, Id).GetHashCode();
		}
		
        #endregion
    }
}