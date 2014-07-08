// 
// TechnicalServiceRequest.cs
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

namespace Mictlanix.BE.Model
{
	[ActiveRecord("tech_service_request")]
	public class TechnicalServiceRequest : ActiveRecordLinqBase<TechnicalServiceRequest>
    {
		[PrimaryKey(PrimaryKeyType.Identity, "tech_service_request_id")]
        [Display(Name = "Id", ResourceType = typeof(Resources))]
		[DisplayFormat(DataFormatString = "{0:D8}")]
		public int Id { get; set; }

		[Property]
		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[Display(Name = "RequestType", ResourceType = typeof(Resources))]
		public TechnicalServiceRequestType Type { get; set; }

		[Property]
		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[Display(Name = "Brand", ResourceType = typeof(Resources))]
		[StringLength(64, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public string Brand { get; set; }

		[Property]
		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[Display(Name = "Equipment", ResourceType = typeof(Resources))]
		[StringLength(64, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public string Equipment { get; set; }

		[Property]
		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[Display(Name = "Model", ResourceType = typeof(Resources))]
		[StringLength(64, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public string Model { get; set; }

		[Property("serial_number")]
		[Display(Name = "SerialNumber", ResourceType = typeof(Resources))]
		[StringLength(64, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public virtual string SerialNumber { get; set; }

		[Property]
		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[DataType(DataType.Date)]
		[Display(Name = "Date", ResourceType = typeof(Resources))]
		public virtual DateTime Date { get; set; }

		[Property("end_date")]
		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[DataType(DataType.Date)]
		[Display(Name = "EndDate", ResourceType = typeof(Resources))]
		public virtual DateTime? EndDate { get; set; }

		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[Display(Name = "Customer", ResourceType = typeof(Resources))]
		[UIHint("CustomerSelector")]
		public virtual string CustomerId { get; set; }

		[BelongsTo("customer", NotNull = true, Fetch = FetchEnum.Join)]
		[Display(Name = "Customer", ResourceType = typeof(Resources))]
		public virtual Customer Customer { get; set; }

		[Property("responsible")]
		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[Display(Name = "ResponsiblePerson", ResourceType = typeof(Resources))]
		[StringLength(128, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public string ResponsiblePerson { get; set; }

		[Property]
		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[Display(Name = "RequestLocation", ResourceType = typeof(Resources))]
		[StringLength(512, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public string Location { get; set; }

		[Property("payment_status")]
		[Display(Name = "PaymentStatus", ResourceType = typeof(Resources))]
		[StringLength(64, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public string PaymentStatus { get; set; }

		[Property("shipping_method")]
		[Display(Name = "ShippingMethod", ResourceType = typeof(Resources))]
		[StringLength(64, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public string ShippingMethod { get; set; }

		[Property("contact_name")]
		[Display(Name = "ContactName", ResourceType = typeof(Resources))]
		[StringLength(64, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public string ContactName { get; set; }

		[Property("contact_phone_number")]
		[Display(Name = "ContactPhoneNumber", ResourceType = typeof(Resources))]
		[StringLength(64, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public string ContactPhoneNumber { get; set; }

		[Property("address")]
		[DataType(DataType.MultilineText)]
		[Display(Name = "Address", ResourceType = typeof(Resources))]
		[StringLength(256, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public string Address { get; set; }

		[Property]
		[DataType(DataType.MultilineText)]
		[Display(Name = "Remarks", ResourceType = typeof(Resources))]
		[StringLength(1024, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public string Remarks { get; set; }

        [Property]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Comment", ResourceType = typeof(Resources))]
		[StringLength(1024, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Comment { get; set; }

        #region Override Base Methods

		public override string ToString ()
		{
			return string.Format ("[ServiceReport: Id={0}, Date={1}, Type={2}", Id, Date, Type);
		}

        public override bool Equals(object obj)
        {
			var other = obj as TechnicalServiceRequest;

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
