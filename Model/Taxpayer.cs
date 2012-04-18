using System;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Mictlanix.BE.Model
{   
    [ActiveRecord("taxpayer")]
    public class Taxpayer : ActiveRecordLinqBase<Taxpayer>
    {
        IList<TaxpayerDocument> documents = new List<TaxpayerDocument>();
		
        public Taxpayer()
		{
		}

		[PrimaryKey("taxpayer_id")]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(13, MinimumLength = 12, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        [Display(Name = "TaxpayerId", ResourceType = typeof(Resources))]
        public string Id { get; set; }
		
        [Property]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(250, MinimumLength = 3, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        [Display(Name = "TaxpayerName", ResourceType = typeof(Resources))]
        public string Name { get; set; }
		
		[BelongsTo("address")]
		[Display(Name = "Address", ResourceType = typeof(Resources))]
		public virtual Address Address { get; set; }
		
		[Property("certificate_number")]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [Display(Name = "CertificateNumber", ResourceType = typeof(Resources))]
		[DisplayFormat(DataFormatString="{0:00000000000000000000}", ApplyFormatInEditMode = true)]
        public decimal CertificateNumber { get; set; }
		
        [Property("certificate_data")]
        [Display(Name = "Certificate", ResourceType = typeof(Resources))]
        public virtual byte[] CertificateData { get; set; }
		
        [Property("key_data")]
        [Display(Name = "PrivateKey", ResourceType = typeof(Resources))]
        public virtual byte[] KeyData { get; set; }
		
        [Property("key_password")]
        [Display(Name = "PrivateKeyPassword", ResourceType = typeof(Resources))]
        public virtual byte[] KeyPassword { get; set; }
		
		[DataType(DataType.Password)]
        [Display(Name = "PrivateKeyPassword", ResourceType = typeof(Resources))]
        public string KeyPassword2 { get; set; }

		[HasMany(typeof(TaxpayerDocument), Table = "taxpayer_document", ColumnKey = "taxpayer", Lazy = true)]
		public IList<TaxpayerDocument> Documents {
			get { return documents; }
			set { documents = value; }
		}
		
        #region Override Base Methods

		public override string ToString ()
		{
			return string.Format ("{1} ({0})", Id, Name);
		}

		public override bool Equals (object obj)
		{
			Taxpayer other = obj as Taxpayer;

			if (other == null)
				return false;

			if (string.IsNullOrEmpty(Id) && string.IsNullOrEmpty(other.Id))
				return (object)this == other;
			else
				return Id == other.Id;
		}

		public override int GetHashCode ()
		{
			if (string.IsNullOrEmpty (Id))
				return base.GetHashCode ();

			return string.Format ("{0}#{1}", GetType ().FullName, Id).GetHashCode ();
		}

        #endregion
    }
}
