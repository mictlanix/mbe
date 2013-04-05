﻿// 
// TaxpayerCertificate.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.org>
// 
// Copyright (C) 2013 Eddy Zavaleta, Mictlanix, and contributors.
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
	[ActiveRecord("taxpayer_certificate")]
	public class TaxpayerCertificate : ActiveRecordLinqBase<TaxpayerCertificate>
    {
		public TaxpayerCertificate ()
		{
		}

		//FIXME: DataFormatString change for D20
		[PrimaryKey(PrimaryKeyType.Assigned, "taxpayer_certificate_id")]
		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[Display(Name = "CertificateNumber", ResourceType = typeof(Resources))]
		[DisplayFormat(DataFormatString="{0:00000000000000000000}", ApplyFormatInEditMode = true)]
		public virtual ulong Id { get; set; }

		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[Display(Name = "Taxpayer", ResourceType = typeof(Resources))]
		[UIHint("TaxpayerSelector")]
		public virtual string TaxpayerId { get; set; }

		[BelongsTo("taxpayer", NotNull = true, Lazy = FetchWhen.OnInvoke)]
		[Display(Name = "Taxpayer", ResourceType = typeof(Resources))]
		public virtual Taxpayer Taxpayer { get; set; }

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
		
		[Property("valid_from")]
		[Display(Name = "NotBefore", ResourceType = typeof(Resources))]
		public DateTime NotBefore { get; set; }
		
		[Property("valid_to")]
		[Display(Name = "NotAfter", ResourceType = typeof(Resources))]
		public DateTime NotAfter { get; set; }

		[Property("active")]
		[Display(Name = "Active", ResourceType = typeof(Resources))]
		public bool IsActive { get; set; }

        #region Override Base Methods

		public override string ToString ()
		{
			return string.Format ("{0}", Id);
		}

		public override bool Equals (object obj)
		{
			TaxpayerCertificate other = obj as TaxpayerCertificate;

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
				return base.GetHashCode();

			return string.Format ("{0}#{1}", GetType ().FullName, Id).GetHashCode ();
		}

        #endregion
    }
}
