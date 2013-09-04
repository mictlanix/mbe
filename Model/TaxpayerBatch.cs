using System;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Mictlanix.BE.Model
{   
	[ActiveRecord("taxpayer_batch", Lazy = true)]
    public class TaxpayerBatch : ActiveRecordLinqBase<TaxpayerBatch>
    {
		[PrimaryKey(PrimaryKeyType.Identity, "taxpayer_batch_id")]
		public virtual int Id { get; set; }
		
		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[Display(Name = "Taxpayer", ResourceType = typeof(Resources))]
		[UIHint("TaxpayerSelector")]
		public virtual string TaxpayerId { get; set; }
		
		[BelongsTo("taxpayer")]
		[Display(Name = "Taxpayer", ResourceType = typeof(Resources))]
		public virtual Taxpayer Taxpayer { get; set; }
		
		[Property]
		[Display(Name = "Batch", ResourceType = typeof(Resources))]
		public virtual string Batch { get; set; }
		
		[Property("serial_start")]
        [Display(Name = "SerialStart", ResourceType = typeof(Resources))]
		public virtual int SerialStart { get; set; }
		
		[Property("serial_end")]
        [Display(Name = "SerialEnd", ResourceType = typeof(Resources))]
		public virtual int SerialEnd { get; set; }
		
		[Property("approval_number")]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [Display(Name = "ApprovalNumber", ResourceType = typeof(Resources))]
		public virtual int? ApprovalNumber { get; set; }
		
		[Property("approval_year")]
		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[Display(Name = "ApprovalYear", ResourceType = typeof(Resources))]
		public virtual int? ApprovalYear { get; set; }
		
		[Property]
        [Display(Name = "Type", ResourceType = typeof(Resources))]
		public virtual FiscalDocumentType Type { get; set; }
		
		#region Override Base Methods

		public override string ToString ()
		{
			return string.Format ("{0} [{1:D5} - {2:D5}])", Batch, SerialStart, SerialEnd);
		}

		public override bool Equals (object obj)
		{
			TaxpayerBatch other = obj as TaxpayerBatch;

			if (other == null)
				return false;

			if (Id == 0 && other.Id == 0)
				return (object)this == other;
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
