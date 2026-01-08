using System;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Mictlanix.BE.Model {
	[ActiveRecord ("deliveries_itinerary")]
	public class DeliveriesItinerary : ActiveRecordLinqBase<DeliveriesItinerary> {
		IList<DeliveriesItineraryDetail> details = new List<DeliveriesItineraryDetail> ();

		[PrimaryKey (PrimaryKeyType.Identity, "deliveries_itinerary_id")]
		[Display (Name = "Id", ResourceType = typeof (Resources))]
		[DisplayFormat (DataFormatString = "{0:D8}")]
		public virtual int Id { get; set; }

		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "Vehicle", ResourceType = typeof (Resources))]
		[UIHint ("VehicleSelector")]
		public int VehicleId { get; set; }

		[BelongsTo ("vehicle")]
		[Display (Name = "Vehicle", ResourceType = typeof (Resources))]
		public virtual Vehicle Vehicle { get; set; }

		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "VehicleOperator", ResourceType = typeof (Resources))]
		[UIHint ("VehicleOperatorSelector")]
		public int VehicleOperatorId { get; set; }

		//[BelongsTo ("vehicle_operator", NotNull = true, Fetch = FetchEnum.Join)]
		[BelongsTo ("vehicle_operator", Fetch = FetchEnum.Join)]
		[Display (Name = "VehicleOperator", ResourceType = typeof (Resources))]
		public virtual VehicleOperator VehicleOperator { get; set; }

		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[Property ("date")]
		[DataType (DataType.Date)]
		[Display (Name = "Date", ResourceType = typeof (Resources))]
		public virtual DateTime Date { get; set; }

		[Property ("completed")]
		[Display (Name = "Completed", ResourceType = typeof (Resources))]
		public virtual bool IsCompleted { get; set; }


		[Property ("cancelled")]
		[Display (Name = "Cancelled", ResourceType = typeof (Resources))]
		public virtual bool IsCancelled { get; set; }

		[Property]
		[DataType (DataType.MultilineText)]
		[Display (Name = "Comment", ResourceType = typeof (Resources))]
		[StringLength (500, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string Comment { get; set; }

		[HasMany (typeof (DeliveriesItineraryDetail), Table = "deliveries_itinerary_detail", ColumnKey = "deliveries_itinerary", Lazy = true)]
		public virtual IList<DeliveriesItineraryDetail> Details {
			get { return details; }
			set { details = value; }
		}

		[BelongsTo ("creator", Lazy = FetchWhen.OnInvoke)]
		[Display (Name = "Creator", ResourceType = typeof (Resources))]
		public virtual Employee Creator { get; set; }

		[Property ("creation_time")]
		[DataType (DataType.DateTime)]
		[Display (Name = "CreationTime", ResourceType = typeof (Resources))]
		public virtual DateTime CreationTime { get; set; }

		[BelongsTo ("updater", Lazy = FetchWhen.OnInvoke)]
		[Display (Name = "Updater", ResourceType = typeof (Resources))]
		public virtual Employee Updater { get; set; }

		[BelongsTo ("warehouse", Lazy = FetchWhen.OnInvoke)]
		[Display (Name = "Warehouse", ResourceType = typeof (Resources))]
		public virtual Warehouse Warehouse { get; set; }

		[Property ("modification_time")]
		[DataType (DataType.DateTime)]
		[Display (Name = "ModificationTime", ResourceType = typeof (Resources))]
		public virtual DateTime ModificationTime { get; set; }

		[Display (Name = "Address", ResourceType = typeof (Resources))]
		public virtual IEnumerable<DeliveryOrder> DeliveryOrders {
			get { return Details.Select (x => x.DeliveryOrderDetail.DeliveryOrder).Distinct().ToArray (); } }

		#region Override Base Methods


		public string QueriableString {
			get {
				return string.Format ("{0:D8} [{1}, {2}]", Id, VehicleOperator.Operator.Nickname, Vehicle.NickName);
			}
		}

		public override bool Equals (object obj)
		{
			DeliveriesItinerary other = obj as DeliveriesItinerary;

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
