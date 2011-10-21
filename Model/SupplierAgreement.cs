// 
// SupplierAgreement.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.org>
//   Eduardo Nieto <enieto@mictlanix.org>
// 
// Copyright (C) 2011 Eddy Zavaleta, Mictlanix, and contributors.
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
using System.ComponentModel.DataAnnotations;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Business.Essentials.Model.Validation;

namespace Business.Essentials.Model
{
    [ActiveRecord("supplier_agreement")]
    public class SupplierAgreement : ActiveRecordLinqBase<SupplierAgreement>
    {

        [PrimaryKey(PrimaryKeyType.Identity, "supplier_agreement_id")]
        public int Id { get; set; }

        [Property]
        [DataType(DataType.Date)]
        [Display(Name = "Start", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        public DateTime? Start { get; set; }

        [Property]
        [DataType(DataType.Date)]
        [Display(Name = "End", ResourceType = typeof(Resources))]
        [DateGreaterThan("Start", ErrorMessageResourceName = "Validation_DateGreaterThan", ErrorMessageResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        public DateTime? End { get; set; }

        [Property]
        [Display(Name = "Comment", ResourceType = typeof(Resources))]
        [DataType(DataType.MultilineText)]
        [StringLength(500, MinimumLength = 0)]
        public string Comment { get; set; }

        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [Display(Name = "Supplier", ResourceType = typeof(Resources))]
        public int SupplierId { get; set; }

        [BelongsTo("supplier", Lazy = FetchWhen.OnInvoke)]
        [Display(Name = "Supplier", ResourceType = typeof(Resources))]
        public virtual Supplier Supplier { get; set; }

        #region Override Base Methods

        public override string ToString()
        {
            return string.Format("{0} [{1}, {2}]", Start, End, Supplier);
        }

        public override bool Equals(object obj)
        {
            SupplierAgreement other = obj as SupplierAgreement;

            if (other == null)
                return false;

            if (Id == 0 && other.Id == 0)
                return (object)this == other;
            else
                return Id == other.Id;
        }

        public override int GetHashCode()
        {
            if (Id == 0)
                return base.GetHashCode();

            return string.Format("{0}#{1}", GetType().FullName, Id).GetHashCode();
        }

        #endregion
    }
}
