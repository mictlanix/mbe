using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Mictlanix.BE.Model {
	[ActiveRecord ("vehicle_operator")]
	public class VehicleOperator : ActiveRecordLinqBase<VehicleOperator> {

		[PrimaryKey (PrimaryKeyType.Identity, "vehicle_operator_id")]
		[Display (Name = "VehicleOperatorId", ResourceType = typeof (Resources))]
		[DisplayFormat (DataFormatString = "{0:D8}")]
		public int Id { get; set; }

		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "Employee", ResourceType = typeof (Resources))]
		[UIHint ("EmployeeSelector")]
		public int OperatorId { get; set; }

		[BelongsTo ("driver", Fetch = FetchEnum.Join)]
		[Display (Name = "VehicleOperator", ResourceType = typeof (Resources))]
		public virtual Employee Operator { get; set; }

		[Property ("driver_license_number")]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "DriverLicenseNumber", ResourceType = typeof (Resources))]
		public string LicenseDriverNumber { get; set; }

		[Property ("license_type")]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "Type", ResourceType = typeof (Resources))]
		public string LicenseType { get; set; }

		[Property ("issuing_location")]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "IssuingLocation", ResourceType = typeof (Resources))]
		public string IssuingLocation { get; set; }

		[Property ("creation_time")]
		[DataType (DataType.DateTime)]
		[Display (Name = "CreationTime", ResourceType = typeof (Resources))]
		public DateTime CreationTime { get; set; }

		[Property ("modification_time")]
		[DataType (DataType.DateTime)]
		[Display (Name = "ModificationTime", ResourceType = typeof (Resources))]
		public DateTime ModificationTime { get; set; }

		[Property ("issue_date")]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[DataType (DataType.Date)]
		[Display (Name = "IssueDate", ResourceType = typeof (Resources))]
		public DateTime IssueLicenceDate { get; set; }

		[Property ("expiration_date")]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[DataType (DataType.Date)]
		[Display (Name = "ExpirationDate", ResourceType = typeof (Resources))]
		public DateTime ExpirationLicenceDate { get; set; }

		[BelongsTo ("creator")]
		[Display (Name = "Creator", ResourceType = typeof (Resources))]
		public virtual Employee Creator { get; set; }

		[BelongsTo ("updater")]
		[Display (Name = "Updater", ResourceType = typeof (Resources))]
		public virtual Employee Updater { get; set; }

		[Property ("active")]
		[Display (Name = "Active", ResourceType = typeof (Resources))]
		public bool IsActive { get; set; }

		[Display (Name = "CurrentValid", ResourceType = typeof (Resources))]
		public bool IsLicenceCurrentValid {
			get {
				return ExpirationLicenceDate > DateTime.Now && IssueLicenceDate < DateTime.Now;
			}
		}

		#region Override Base Methods

		public override string ToString ()
		{
			return string.Format ("{0:D8}", Id);
		}

		public override bool Equals (object obj)
		{
			var other = obj as VehicleOperator;

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
