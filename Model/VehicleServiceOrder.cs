

using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Mictlanix.BE.Model {
	[ActiveRecord ("vehicle_service_order")]
	public class ServiceOrder : ActiveRecordLinqBase<ServiceOrder> {
		IList<ServiceOrderDetail> details = new List<ServiceOrderDetail> ();

		[PrimaryKey (PrimaryKeyType.Identity, "service_order_id")]
		[Display (Name = "ServiceOrderId", ResourceType = typeof (Resources))]
		[DisplayFormat (DataFormatString = "{0:D8}")]
		public int Id { get; set; }

		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "Vehicle", ResourceType = typeof (Resources))]
		[UIHint ("VehicleSelector")]
		public int VehicleId { get; set; }

		[BelongsTo ("vehicle", Fetch = FetchEnum.Join)]
		[Display (Name = "Vehicle", ResourceType = typeof (Resources))]
		public virtual Vehicle Vehicle { get; set; }

		[Property("problem_description")]
		[DataType (DataType.MultilineText)]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "BreakdownReport", ResourceType = typeof (Resources))]
		[StringLength (500, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string ProblemDescription { get; set; }

		[Property ("service_description")]
		[DataType (DataType.MultilineText)]
		[Display (Name = "SolutionReport", ResourceType = typeof (Resources))]
		[StringLength (500, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string ServiceDescription { get; set; }

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

		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "Notifier", ResourceType = typeof (Resources))]
		[UIHint ("NotifierSelector")]
		public int NotifierId { get; set; }

		[BelongsTo ("notifier")]
		[Display (Name = "Notifier", ResourceType = typeof (Resources))]
		public virtual Employee Notifier { get; set; }

		[BelongsTo ("updater")]
		[Display (Name = "Updater", ResourceType = typeof (Resources))]
		public virtual Employee Updater { get; set; }

		[Property]
		[DataType (DataType.Date)]
		[Display (Name = "Date", ResourceType = typeof (Resources))]
		[DisplayFormat (DataFormatString = "{0:yyyy-MM-dd}")]
		public virtual DateTime? Date { get; set; }

		[Property ("completed")]
		[Display (Name = "Completed", ResourceType = typeof (Resources))]
		public bool IsCompleted { get; set; }

		[Property ("cancelled")]
		[Display (Name = "Cancelled", ResourceType = typeof (Resources))]
		public bool IsCancelled { get; set; }

		[Property]
		[DataType (DataType.MultilineText)]
		[Display (Name = "Comment", ResourceType = typeof (Resources))]
		[StringLength (500, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string Comment { get; set; }

		[HasMany (typeof (ServiceOrderDetail), Table = "service_order_detail", ColumnKey = "vehicle_service_order")]
		public IList<ServiceOrderDetail> Details {
			get { return details; }
			set { details = value; }
		}

		#region Override Base Methods

		public override string ToString ()
		{
			return string.Format ("{0:D8}", Id);
		}

		public override bool Equals (object obj)
		{
			var other = obj as ServiceOrder;

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
