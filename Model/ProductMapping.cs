
using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;
using Mictlanix.BE.Model.Validation;

namespace Mictlanix.BE.Model {
	[ActiveRecord ("product")]
	[Serializable]
	public class ProductMapping : ActiveRecordLinqBase<ProductMapping> {

		[PrimaryKey (PrimaryKeyType.Identity, "product_mapping_id")]
		public int Id { get; set; }

		[BelongsTo ("source_product")]
		[Display (Name = "SourceProduct", ResourceType = typeof (Resources))]
		public Product SourceProduct { get; set; }

		[BelongsTo ("target_product")]
		[Display (Name = "TargetProduct", ResourceType = typeof (Resources))]
		public Product TargetProduct { get; set; }
		

		[Property ("base_quantity")]
		[DisplayFormat (DataFormatString = "{0:0.####}")]
		[Display (Name = "BaseQuantity", ResourceType = typeof (Resources))]
		public virtual decimal BaseQuantity { get; set; }

		[Property ("mapping_quantity")]
		[DisplayFormat (DataFormatString = "{0:0.####}")]
		[Display (Name = "MappingQuantity", ResourceType = typeof (Resources))]
		public virtual decimal MappingQuantity { get; set; }

		#region Override Base Methods

		public override string ToString ()
		{
			return string.Format ("{0} [{1}, {2}, {3}]", SourceProduct.Name, BaseQuantity, TargetProduct.Name , MappingQuantity);
		}

		public override bool Equals (object obj)
		{
			Product other = obj as Product;

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
