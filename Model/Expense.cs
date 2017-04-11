using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Mictlanix.BE.Model {
	[ActiveRecord ("expenses", Lazy = true)]
	public class Expense : ActiveRecordLinqBase<Expense> {
		[PrimaryKey (PrimaryKeyType.Identity, "expense_id")]
		public virtual int Id { get; set; }

		[Property ("expense")]
		[Display (Name = "Name", ResourceType = typeof (Resources))]
		public virtual string Name { get; set; }

		[Property ("comment")]
		[DataType (DataType.MultilineText)]
		[Display (Name = "Comment", ResourceType = typeof (Resources))]
		[StringLength (500, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string Comment { get; set; }
	}
}
