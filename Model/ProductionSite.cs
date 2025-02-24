using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Mictlanix.BE.Model {
	[ActiveRecord ("production_site", Lazy = true)]
	public class ProductionSite : ActiveRecordLinqBase<ProductionSite> {
		[PrimaryKey (PrimaryKeyType.Identity, "production_site_id")]
		public virtual int Id { get; set; }

		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "Store", ResourceType = typeof (Resources))]
		[UIHint ("StoreSelector")]
		public virtual int StoreId { get; set; }

		[BelongsTo ("store")]
		[Display (Name = "Store", ResourceType = typeof (Resources))]
		public virtual Store Store { get; set; }

		[Property]
		[ValidateIsUnique]
		[Display (Name = "Code", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[RegularExpression (@"^\S+$", ErrorMessageResourceName = "Validation_NonWhiteSpace", ErrorMessageResourceType = typeof (Resources))]
		[StringLength (25, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string Code { get; set; }

		[Property]
		[Display (Name = "Name", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[StringLength (250, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string Name { get; set; }

		[Property]
		[DataType (DataType.MultilineText)]
		[Display (Name = "Comment", ResourceType = typeof (Resources))]
		[StringLength (500, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public virtual string Comment { get; set; }

		[Property ("disabled")]
		[Display (Name = "LogicalDelete", ResourceType = typeof (Resources))]
		public virtual bool IsDisabled { get; set; }


		#region Override Base Methods

		public override string ToString ()
		{
			return string.Format ("{0}", Name);
		}

		public override bool Equals (object obj)
		{
			Warehouse other = obj as Warehouse;

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