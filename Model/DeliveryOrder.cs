// 
// InventoryReceipt.cs
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
	[ActiveRecord ("delivery_order")]
	public class DeliveryOrder : ActiveRecordLinqBase<DeliveryOrder> {
		IList<DeliveryOrderDetail> details = new List<DeliveryOrderDetail> ();

        [PrimaryKey (PrimaryKeyType.Identity, "delivery_order_id")]
		[Display (Name = "DeliveryOrderId", ResourceType = typeof (Resources))]
		[DisplayFormat (DataFormatString = "{0:D8}")]
		public int Id { get; set; }

		[Property ("creation_time")]
		[DataType (DataType.DateTime)]
		[Display (Name = "CreationTime", ResourceType = typeof (Resources))]
		public DateTime CreationTime { get; set; }

		[Property ("modification_time")]
		[DataType (DataType.DateTime)]
		[Display (Name = "ModificationTime", ResourceType = typeof (Resources))]
		public DateTime ModificationTime { get; set; }

		[BelongsTo ("creator")]
		[Display (Name = "Creator", ResourceType = typeof (Resources))]
		public virtual Employee Creator { get; set; }

		[BelongsTo ("updater")]
		[Display (Name = "Updater", ResourceType = typeof (Resources))]
		public virtual Employee Updater { get; set; }

		[BelongsTo ("store", Fetch = FetchEnum.Join)]
		[Display (Name = "Store", ResourceType = typeof (Resources))]
		public virtual Store Store { get; set; }

		[Property ("serial")]
		[Display (Name = "Serial", ResourceType = typeof (Resources))]
		[DisplayFormat (DataFormatString = "{0:D8}")]
		public virtual int Serial { get; set; }

		[BelongsTo ("customer", NotNull = true, Fetch = FetchEnum.Join)]
		[Display (Name = "Customer", ResourceType = typeof (Resources))]
		public virtual Customer Customer { get; set; }

		[BelongsTo ("ship_to", Lazy = FetchWhen.OnInvoke)]
		[Display (Name = "ShipTo", ResourceType = typeof (Resources))]
		public virtual Address ShipTo { get; set; }

		[Property]
		[DataType (DataType.Date)]
		[Display (Name = "Date", ResourceType = typeof (Resources))]
		[DisplayFormat (DataFormatString = "{0:yyyy-MM-dd}")]
		public virtual DateTime Date { get; set; }

		[Property ("completed")]
		[Display (Name = "Completed", ResourceType = typeof (Resources))]
		public bool IsCompleted { get; set; }

		[Property ("cancelled")]
		[Display (Name = "Cancelled", ResourceType = typeof (Resources))]
		public bool IsCancelled { get; set; }

		[Property("delivered")]
		[Display(Name = "Delivered", ResourceType = typeof(Resources))]
		public bool IsDelivered { get; set; }

		[Property]
		[DataType (DataType.MultilineText)]
		[Display (Name = "Comment", ResourceType = typeof (Resources))]
		[StringLength (500, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string Comment { get; set; }

		[HasMany (typeof (DeliveryOrderDetail), Table = "delivery_order_detail", ColumnKey = "delivery_order")]
		public IList<DeliveryOrderDetail> Details {
			get { return details; }
			set { details = value; }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Subtotal", ResourceType = typeof (Resources))]
		public virtual decimal Subtotal {
			get { return Details.Sum (x => x.Subtotal); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Discount", ResourceType = typeof (Resources))]
		public virtual decimal Discount {
			get { return Details.Sum (x => x.Discount); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Taxes", ResourceType = typeof (Resources))]
		public virtual decimal Taxes {
			get { return Details.Sum (x => x.Taxes); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Total", ResourceType = typeof (Resources))]
		public virtual decimal Total {
			get { return Details.Sum (x => x.Total); }
		}

		#region Override Base Methods

		public override string ToString ()
		{
			return string.Format ("{0:D8}", Id);
		}

		public override bool Equals (object obj)
		{
			var other = obj as DeliveryOrder;

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
