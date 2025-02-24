using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;

namespace Mictlanix.BE.Model {

	[ActiveRecord ("purchase_request", Lazy = true)]
	[Serializable]
	public class PurchaseRequest : ActiveRecordLinqBase<PurchaseRequest> {
		IList<PurchaseRequestDetail> details = new List<PurchaseRequestDetail> ();

		[PrimaryKey (PrimaryKeyType.Identity, "purchase_request_id")]
		[DisplayFormat (DataFormatString = "{0:D8}")]
		public virtual int Id { get; set; }

		[Property ("serial")]
		[Display (Name = "Serial", ResourceType = typeof (Resources))]
		[DisplayFormat (DataFormatString = "{0:D8}")]
		public virtual int Serial { get; set; }

		[BelongsTo ("creator", Lazy = FetchWhen.OnInvoke)]
		[Display (Name = "Creator", ResourceType = typeof (Resources))]
		public virtual Employee Creator { get; set; }

		[BelongsTo ("updater", Lazy = FetchWhen.OnInvoke)]
		[Display (Name = "Updater", ResourceType = typeof (Resources))]
		public virtual Employee Updater { get; set; }

		[Property ("date")]
		[DataType (DataType.Date)]
		[Display (Name = "Date", ResourceType = typeof (Resources))]
		public virtual DateTime Date { get; set; }

		[Property ("creation_time")]
		[Display (Name = "CreationTime", ResourceType = typeof (Resources))]
		public virtual DateTime CreationTime { get; set; }

		[Property ("modification_time")]
		[Display (Name = "ModificationTime", ResourceType = typeof (Resources))]
		public virtual DateTime ModificationTime { get; set; }

		[Display (Name = "Warehouse", ResourceType = typeof (Resources))]
		[UIHint ("WarehouseSelector")]
		public virtual int WarehouseId { get; set; }

		[BelongsTo ("warehouse")]
		[Display (Name = "Warehouse", ResourceType = typeof (Resources))]
		public virtual Warehouse Warehouse { get; set; }

		[HasMany (typeof (PurchaseRequestDetail), Table = "purchase_request_detail", ColumnKey = "purchase_request")]
		public virtual IList<PurchaseRequestDetail> Details {
			get { return details; }
			set { details = value; }
		}

		[Property ("comment")]
		[DataType (DataType.MultilineText)]
		[Display (Name = "Comment", ResourceType = typeof (Resources))]
		[StringLength (500, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string Comment { get; set; }

		[Property ("completed")]
		[Display (Name = "Completed", ResourceType = typeof (Resources))]
		public virtual bool IsCompleted { get; set; }

		[Property ("approved")]
		[Display (Name = "Approved", ResourceType = typeof (Resources))]
		public virtual bool IsApproved { get; set; }

		[Property ("cancelled")]
		[Display (Name = "Cancelled", ResourceType = typeof (Resources))]
		public virtual bool IsCancelled { get; set; }

		#region Override Base Methods

		public override string ToString ()
																{
			return string.Format ("{0} [{1}]", Id, Updater);
		}

		public override bool Equals (object obj)
		{
			PurchaseRequest other = obj as PurchaseRequest;

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

		public virtual PurchaseRequest GetSerializable () {
			return new PurchaseRequest {
				WarehouseId = WarehouseId,
				Id = Id,
				Comment = Comment,
				CreationTime = CreationTime,
				Creator = Creator.GetSerializable(),
				Date = Date,
				Details = Details.Select(x => x.GetSerializable()).ToList(),
				IsApproved = IsApproved,
				IsCancelled = IsCancelled,
				IsCompleted = IsCompleted,
				ModificationTime = ModificationTime,
				Serial = Serial,
				Updater = Updater.GetSerializable()
			};
		}
	}
}
