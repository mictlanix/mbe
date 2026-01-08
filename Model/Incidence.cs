

using System;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Mictlanix.BE.Model {
	[ActiveRecord ("incidence", Lazy = true)]
	public class Incidence : ActiveRecordLinqBase<Incidence> {

		[PrimaryKey (PrimaryKeyType.Identity, "incidence_id")]
		[Display (Name = "IncidenceId", ResourceType = typeof (Resources))]
		[DisplayFormat (DataFormatString = "{0:D8}")]
		public virtual int Id { get; set; }

		[Property ("source")]
		[Display (Name = "source", ResourceType = typeof (Resources))]
		public virtual SourceType SourceType { get; set; }

		[Property ("instance_id")]
		[DisplayFormat (DataFormatString = "{0:D8}")]
		[Display (Name = "Reference", ResourceType = typeof (Resources))]
		public virtual int Reference { get; set; }

		[BelongsTo ("updater")]
		[Display (Name = "Employee", ResourceType = typeof (Resources))]
		public virtual Employee Updater { get; set; }

		[Property ("modification_time")]
		[DataType (DataType.DateTime)]
		[Display (Name = "ModificationTime", ResourceType = typeof (Resources))]
		public virtual DateTime ModificationTime { get; set; }

		[Property ("content")]
		[DataType (DataType.MultilineText)]
		[Display (Name = "Report", ResourceType = typeof (Resources))]
		[StringLength (1000, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string PreviousState { get; set; }

		[Property("comment")]
		[DataType (DataType.MultilineText)]
		[Display (Name = "Comment", ResourceType = typeof (Resources))]
		[StringLength (500, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string Comment { get; set; }

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
