// 
// PurchaseOrderDetail.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
//   Eduardo Nieto <enieto@mictlanix.com>
// 
// Copyright (C) 2011-2017 Eddy Zavaleta, Mictlanix, and contributors.
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
	[ActiveRecord ("purchase_request_detail")]
	[Serializable]
	public class PurchaseRequestDetail : ActiveRecordLinqBase<PurchaseRequestDetail> {
		[PrimaryKey (PrimaryKeyType.Identity, "purchase_request_detail_id")]
		public int Id { get; set; }


		[BelongsTo ("purchase_request", Lazy = FetchWhen.OnInvoke)]
		[Display (Name = "PurchaseRequest", ResourceType = typeof (Resources))]
		public virtual PurchaseRequest PurchaseRequest { get; set; }

		[BelongsTo ("product")]
		[Display (Name = "Product", ResourceType = typeof (Resources))]
		public virtual Product Product { get; set; }

		[Property ("product_name")]
		[Display (Name = "ProductName", ResourceType = typeof (Resources))]
		[StringLength (250, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string ProductName { get; set; }

		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "Warehouse", ResourceType = typeof (Resources))]
		[UIHint ("WarehouseSelector")]
		public int WarehouseId { get; set; }

		[BelongsTo ("warehouse")]
		[Display (Name = "Warehouse", ResourceType = typeof (Resources))]
		public virtual Warehouse Warehouse { get; set; }


		[BelongsTo ("customer")]
		[Display (Name = "Customer", ResourceType = typeof (Resources))]
		public virtual Customer Customer { get; set; }

		[Property]
		[DisplayFormat (DataFormatString = "{0:0.####}")]
		[Display (Name = "Quantity", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof (Resources))]
		public virtual decimal Quantity { get; set; }

		[Property("to_purchase")]
		[DisplayFormat (DataFormatString = "{0:0.####}")]
		[Display (Name = "Accepted", ResourceType = typeof (Resources))]
		public virtual bool ToPurchase { get; set; }

		#region Override Base Methods

		public override string ToString ()
		{
			return string.Format ("{0} [{1}, {2}, {3}]", Id, PurchaseRequest, Product, Quantity);
		}

		public override bool Equals (object obj)
		{
			PurchaseRequestDetail other = obj as PurchaseRequestDetail;

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

		public virtual PurchaseRequestDetail GetSerializable () {
			return new PurchaseRequestDetail {
				Customer = Customer.GetSerializable(),
				Quantity = Quantity,
				Id = Id,
				Product = Product.GetSerializable(),
				ProductName = ProductName,
				WarehouseId = WarehouseId
			};
		}
	}
}
