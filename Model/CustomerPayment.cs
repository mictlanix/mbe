// 
// CustomerPayment.cs
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
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Mictlanix.BE.Model
{
    [ActiveRecord("customer_payment")]
    public class CustomerPayment : ActiveRecordLinqBase<CustomerPayment>
    {
        [PrimaryKey(PrimaryKeyType.Identity, "customer_payment_id")]
        public int Id { get; set; }
		
        [Property("creation_time")]
		[DataType(DataType.DateTime)]
		[Display(Name = "CreationTime", ResourceType = typeof(Resources))]
		public virtual DateTime CreationTime { get; set; }

        [Property("modification_time")]
		[DataType(DataType.DateTime)]
		[Display(Name = "ModificationTime", ResourceType = typeof(Resources))]
		public virtual DateTime ModificationTime { get; set; }

        [BelongsTo("creator", Lazy = FetchWhen.OnInvoke)]
        [Display(Name = "Creator", ResourceType = typeof(Resources))]
        public virtual Employee Creator { get; set; }

        [BelongsTo("updater", Lazy = FetchWhen.OnInvoke)]
        [Display(Name = "Updater", ResourceType = typeof(Resources))]
        public virtual Employee Updater { get; set; }

		[BelongsTo("store")]
		[Display(Name = "Store", ResourceType = typeof(Resources))]
		public virtual Store Store { get; set; }
		
		[Property("serial")]
		[Display(Name = "Serial", ResourceType = typeof(Resources))]
		[DisplayFormat(DataFormatString="{0:000000}")]
		public int Serial { get; set; }
		
		[BelongsTo("cash_session")]
		[Display(Name = "CashSession", ResourceType = typeof(Resources))]
		public virtual CashSession CashSession { get; set; }

        [BelongsTo("sales_order")]
        [Display(Name = "SalesOrder", ResourceType = typeof(Resources))]
        public virtual SalesOrder SalesOrder { get; set; }

        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [Display(Name = "Customer", ResourceType = typeof(Resources))]
        [UIHint("CustomerSelector")]
        public int CustomerId { get; set; }

        [BelongsTo("customer")]
        [Display(Name = "Customer", ResourceType = typeof(Resources))]
        public virtual Customer Customer { get; set; }

        [Property]
        [DataType(DataType.Currency)]
        [Display(Name = "Amount", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof(Resources))]
        public decimal Amount { get; set; }

        [Property("cash_change")]
        [DataType(DataType.Currency)]
        [Display(Name = "Change", ResourceType = typeof(Resources))]
        public decimal? Change { get; set; }

        [Property]
        [Display(Name = "PaymentMethod", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        public PaymentMethod Method { get; set; }

        [Property]
        [DataType(DataType.DateTime)]
        [Display(Name = "Date", ResourceType = typeof(Resources))]
        public DateTime Date { get; set; }

        [Property]
        [Display(Name = "PaymentReference", ResourceType = typeof(Resources))]
        public string Reference { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "Cash", ResourceType = typeof(Resources))]
        public decimal Cash
        {
            get { return Amount + Change.GetValueOrDefault(); }
        }

        #region Override Base Methods

        public override string ToString()
        {
            return string.Format("{0} : {1:c} [{2:u}]", Method, Amount, Date);
        }

        public override bool Equals(object obj)
        {
            CustomerPayment other = obj as CustomerPayment;

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
