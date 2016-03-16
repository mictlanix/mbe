// 
// TechnicalServiceRequestComponent.cs
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
using System.ComponentModel.DataAnnotations;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Mictlanix.BE.Model.Validation;

namespace Mictlanix.BE.Model {
	[ActiveRecord ("tech_service_request_component", Lazy = true)]
	public class TechnicalServiceRequestComponent : ActiveRecordLinqBase<TechnicalServiceRequestComponent> {
		[PrimaryKey (PrimaryKeyType.Identity, "tech_service_request_component_id")]
		public virtual int Id { get; set; }

		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "TechnicalServiceRequest", ResourceType = typeof (Resources))]
		public virtual int RequestId { get; set; }

		[BelongsTo ("request", Lazy = FetchWhen.OnInvoke)]
		[Display (Name = "TechnicalServiceRequest", ResourceType = typeof (Resources))]
		public virtual TechnicalServiceRequest Request { get; set; }

		[Property]
		[Display (Name = "Name", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof (Resources))]
		[StringLength (128, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string Name { get; set; }

		[Property]
		[Display (Name = "Quantity", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof (Resources))]
		public virtual int Quantity { get; set; }

		[Property ("serial_number")]
		[Display (Name = "SerialNumber", ResourceType = typeof (Resources))]
		[StringLength (64, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string SerialNumber { get; set; }

		[Property]
		[Display (Name = "Comment", ResourceType = typeof (Resources))]
		[DataType (DataType.MultilineText)]
		[StringLength (256, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string Comment { get; set; }

		#region Override Base Methods

		public override string ToString ()
		{
			return string.Format ("[TechnicalServiceRequestComponent: Id={0}, Name={1}, Quantity={2}, SerialNumber={3}, Comment={4}]", Id, Name, Quantity, SerialNumber, Comment);
		}

		public override bool Equals (object obj)
		{
			var other = obj as TechnicalServiceRequestComponent;

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
