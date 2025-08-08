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
	[ActiveRecord ("delivery_order", Lazy = true)]
	[Serializable]
	public class DeliveryOrder : ActiveRecordLinqBase<DeliveryOrder> {
		IList<DeliveryOrderDetail> details = new List<DeliveryOrderDetail> ();

        [PrimaryKey (PrimaryKeyType.Identity, "delivery_order_id")]
		[Display (Name = "DeliveryOrderId", ResourceType = typeof (Resources))]
		[DisplayFormat (DataFormatString = "{0:D8}")]
		public virtual int Id { get; set; }

		[Property ("creation_time")]
		[DataType (DataType.DateTime)]
		[Display (Name = "CreationTime", ResourceType = typeof (Resources))]
		public virtual DateTime CreationTime { get; set; }

		[Property ("modification_time")]
		[DataType (DataType.DateTime)]
		[Display (Name = "ModificationTime", ResourceType = typeof (Resources))]
		public virtual DateTime ModificationTime { get; set; }

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

		[BelongsTo ("contact", NotNull = false, Lazy = FetchWhen.OnInvoke)]
		[Display (Name = "Contact", ResourceType = typeof (Resources))]
		public virtual Contact Contact { get; set; }

		[Property]
		[DataType (DataType.Date)]
		[Display (Name = "DeliveryDate", ResourceType = typeof (Resources))]
		[DisplayFormat (DataFormatString = "{0:yyyy-MM-dd}")]
		public virtual DateTime Date { get; set; }

		[Property ("completed")]
		[Display (Name = "Completed", ResourceType = typeof (Resources))]
		public virtual bool IsCompleted { get; set; }

		[Property ("cancelled")]
		[Display (Name = "Cancelled", ResourceType = typeof (Resources))]
		public virtual bool IsCancelled { get; set; }

		[Property("delivered")]
		[Display(Name = "Delivered", ResourceType = typeof(Resources))]
		public virtual bool IsDelivered { get; set; }

		[Property("confirmed")]
		[Display(Name = "Confirmed", ResourceType = typeof(Resources))]
		public virtual bool IsConfirmed { get; set; }

		[Property("picked_up")]
		[Display(Name = "CounterDelivery", ResourceType = typeof(Resources))]
		public virtual bool IsPickedUpInStore { get; set; }

		[Property]
		[Display (Name = "Priority", ResourceType = typeof (Resources))]
		public virtual Priority Priority { get; set; }

		[Property]
		[DataType (DataType.MultilineText)]
		[Display (Name = "Comment", ResourceType = typeof (Resources))]
		[StringLength (500, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string Comment { get; set; }

		[HasMany (typeof (DeliveryOrderDetail), Table = "delivery_order_detail", ColumnKey = "delivery_order")]
		public virtual IList<DeliveryOrderDetail> Details {
			get { return details; }
			set { details = value; }
		}

		public virtual DeliveryOrder GetSerializable () {
			return new DeliveryOrder {
				Id = Id,
				Date = Date,
				Contact = Contact == null ? null : Contact.GetSerializable(),
				CreationTime = CreationTime,
				Comment = Comment,
				Customer = Customer.GetSerializable(),
				//Creator = Creator.GetSerializable(),
				IsCancelled = IsCancelled,
				IsConfirmed = IsConfirmed,
				IsDelivered = IsDelivered,
				IsCompleted = IsCompleted,
				ModificationTime = ModificationTime,
				Serial = Serial,
				ShipTo = ShipTo == null ? null : ShipTo.GetSerializable(),
				Details = Details.Select(x => x.GetSerializable()).ToList(),
			};
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
