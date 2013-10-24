// 
// CustomerTaxpayer.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2011-2013 Eddy Zavaleta, Mictlanix, and contributors.
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Mictlanix.BE.Model
{   
	[ActiveRecord("taxpayer", Lazy = true)]
    public class Taxpayer : ActiveRecordLinqBase<Taxpayer>
    {
		IList<TaxpayerBatch> batches = new List<TaxpayerBatch>();
		IList<TaxpayerCertificate> certificates = new List<TaxpayerCertificate>();
		
        public Taxpayer()
		{
		}

		[PrimaryKey("taxpayer_id")]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(13, MinimumLength = 12, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        [Display(Name = "TaxpayerId", ResourceType = typeof(Resources))]
		public virtual string Id { get; set; }
		
        [Property]
        [StringLength(250, MinimumLength = 3, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        [Display(Name = "TaxpayerName", ResourceType = typeof(Resources))]
		public virtual string Name { get; set; }

		[Property]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(250, MinimumLength = 3, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        [Display(Name = "TaxRegime", ResourceType = typeof(Resources))]
		public virtual string Regime { get; set; }

		[Property]
		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[Display(Name = "Scheme", ResourceType = typeof(Resources))]
		public virtual FiscalScheme Scheme { get; set; }

		[Property]
		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[Display(Name = "Provider", ResourceType = typeof(Resources))]
		public virtual FiscalCertificationProvider Provider { get; set; }

		[Display(Name = "Address", ResourceType = typeof(Resources))]
		public virtual bool HasAddress { get; set; }

		[BelongsTo("address", Lazy = FetchWhen.OnInvoke)]
		[Display(Name = "Address", ResourceType = typeof(Resources))]
		public virtual Address Address { get; set; }
		
		[HasMany(typeof(TaxpayerCertificate), Table = "taxpayer_certificate", ColumnKey = "taxpayer", Lazy = true)]
		public virtual IList<TaxpayerCertificate> Certificates {
			get { return certificates; }
			set { certificates = value; }
		}

		[HasMany(typeof(TaxpayerBatch), Table = "taxpayer_document", ColumnKey = "taxpayer", Lazy = true)]
		public virtual IList<TaxpayerBatch> Batches {
			get { return batches; }
			set { batches = value; }
		}
		
        #region Override Base Methods

		public override string ToString ()
		{
			var format = string.IsNullOrWhiteSpace (Name) ? "{0}" : "{0} ({1})";
			return string.Format (format, Id, Name);
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
