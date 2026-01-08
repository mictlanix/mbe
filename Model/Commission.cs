using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Mictlanix.BE.Model {
	[ActiveRecord ("commission", Lazy = true)]
	public class Commission : ActiveRecordLinqBase<Commission> {
		[PrimaryKey (PrimaryKeyType.Identity, "commission_id")]
		public virtual int Id { get; set; }

		[Property ("name")]
		[Display (Name = "Name", ResourceType = typeof (Resources))]
		public virtual string Name { get; set; }

		[Property ("commission_rate")]
		[Display (Name = "CommissionRate", ResourceType = typeof (Resources))]
		public virtual decimal CommissionRate { get; set; }
	}
}
