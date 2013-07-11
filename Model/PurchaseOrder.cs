// 
// PurchaseOrder.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
//   Eduardo Nieto <enieto@mictlanix.com>
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
    [ActiveRecord("purchase_order")]
    public class PurchaseOrder : ActiveRecordLinqBase<PurchaseOrder>
    {
        IList<PurchaseOrderDetail> details = new List<PurchaseOrderDetail>();

        [PrimaryKey(PrimaryKeyType.Identity, "purchase_order_id")]
        [Display(Name = "PurchaseOrderId", ResourceType = typeof(Resources))]
		[DisplayFormat(DataFormatString = "{0:D8}")]
        public int Id { get; set; }

        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [Display(Name = "Supplier", ResourceType = typeof(Resources))]
        [UIHint("SupplierSelector")]
        public int SupplierId { get; set; }

        [BelongsTo("supplier")]
        [Display(Name = "Supplier", ResourceType = typeof(Resources))]
        public virtual Supplier Supplier { get; set; }

        [Property("creation_time")]
        [DataType(DataType.DateTime)]
        [Display(Name = "CreationTime", ResourceType = typeof(Resources))]
        public DateTime CreationTime { get; set; }

        [Property("modification_time")]
        [DataType(DataType.DateTime)]
        [Display(Name = "ModificationTime", ResourceType = typeof(Resources))]
        public DateTime ModificationTime { get; set; }

        [BelongsTo("creator")]
        [Display(Name = "Creator", ResourceType = typeof(Resources))]
        public virtual Employee Creator { get; set; }

        [BelongsTo("updater")]
        [Display(Name = "Updater", ResourceType = typeof(Resources))]
        public virtual Employee Updater { get; set; }

        [Property("completed")]
        [Display(Name = "Completed", ResourceType = typeof(Resources))]
        public bool IsCompleted { get; set; }

        [Property("cancelled")]
        [Display(Name = "Cancelled", ResourceType = typeof(Resources))]
        public bool IsCancelled { get; set; }

        [Property("invoice_number")]
        [Display(Name = "InvoiceNumber", ResourceType = typeof(Resources))]
        [StringLength(50, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string InvoiceNumber { get; set; }

        [Property]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Comment", ResourceType = typeof(Resources))]
        [StringLength(500, MinimumLength = 0)]
        public string Comment { get; set; }

        [HasMany(typeof(PurchaseOrderDetail), Table = "purchase_order_detail", ColumnKey = "purchase_order")]
        public IList<PurchaseOrderDetail> Details
        {
            get { return details; }
            set { details = value; }
        }

        [DataType(DataType.Currency)]
        [Display(Name = "Subtotal", ResourceType = typeof(Resources))]
        public decimal Subtotal
        {
            get { return Details.Sum(x => x.Subtotal); }
        }

        [DataType(DataType.Currency)]
        [Display(Name = "Taxes", ResourceType = typeof(Resources))]
        public decimal Taxes
        {
            get { return Total - Subtotal; }
        }

        [DataType(DataType.Currency)]
        [Display(Name = "Total", ResourceType = typeof(Resources))]
        public decimal Total
        {
            get { return Details.Sum(x => x.Total); }
        }

        #region Override Base Methods

        public override string ToString()
        {
            return string.Format("{0} [{1}, {2}, {3}]", Id, CreationTime, Creator, Supplier);
        }

        public override bool Equals(object obj)
        {
            PurchaseOrder other = obj as PurchaseOrder;

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
