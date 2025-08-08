
using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Mictlanix.BE.Model {
	[ActiveRecord ("commission_product")]
	public class CommissionProduct : ActiveRecordLinqBase<CommissionProduct> {
		[PrimaryKey (PrimaryKeyType.Identity, "commission_product_id")]
		public virtual int Id { get; set; }

		[BelongsTo ("product")]
		[Display (Name = "Product", ResourceType = typeof (Resources))]
		public virtual Product Product { get; set; }

		[BelongsTo ("commission")]
		[Display (Name = "Commission", ResourceType = typeof (Resources))]
		public virtual Commission Commission { get; set; }


		#region Override Base Methods

		public override bool Equals (object obj)
		{
			var other = obj as ProductPrice;

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
