using System.ComponentModel.DataAnnotations;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Mictlanix.BE.Model {
		[ActiveRecord("vehicle", Lazy =true)]
	public class Vehicle:ActiveRecordLinqBase<Vehicle> {
		[PrimaryKey(PrimaryKeyType.Identity, "vehicle_id")]
		public virtual int Id { get; set; }

		[Property("license_plate")]
		[ValidateIsUnique]
		[Display (Name = "LicensePlate", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[StringLength (8, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string LicensePlate { get; set; }

		[Property("name")]
		[Display (Name = "Name", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[StringLength (50, MinimumLength = 10, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string Name { get; set; }

		[Property("nickname")]
		[ValidateIsUnique]
		[Display (Name = "Nickname", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[StringLength (30, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string NickName { get; set; }

		[Property("tons_capacity")]
		[Display (Name = "TonsCapacity", ResourceType = typeof (Resources))]
		public virtual int TonsCapacity { get; set; }

		[Property("active")]
		[Display (Name = "Active", ResourceType = typeof (Resources))]
		public virtual bool IsActive { get; set; }
	}
}
