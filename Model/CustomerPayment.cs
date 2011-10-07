// 
// CustomerPayment.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.org>
//   Eduardo Nieto <enieto@mictlanix.org>
// 
// Copyright (C) 2011 Eddy Zavaleta, Mictlanix (http://www.mictlanix.org)
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

namespace Business.Essentials.Model
{
    public enum PaymentMethod : int
    {
        [Display(Name = "Cash", ResourceType = typeof(Resources))]
        Cash = 1,
        [Display(Name = "CreditCard", ResourceType = typeof(Resources))]
        CreditCard,
        [Display(Name = "DebitCard", ResourceType = typeof(Resources))]
        DebitCard,
        [Display(Name = "Check", ResourceType = typeof(Resources))]
        Check,
        [Display(Name = "WireTransfer", ResourceType = typeof(Resources))]
        WireTransfer,
        [Display(Name = "GovernmentFunding", ResourceType = typeof(Resources))]
        GovernmentFunding
    }

    [ActiveRecord("customer_payment")]
    public class CustomerPayment : ActiveRecordLinqBase<CustomerPayment>
    {
        [PrimaryKey(PrimaryKeyType.Identity, "customer_payment_id")]
        public int Id { get; set; }

        [BelongsTo("sales_order")]
        [Display(Name = "SalesOrder", ResourceType = typeof(Resources))]
        public virtual SalesOrder SalesOrder { get; set; }

        [Property]
        [Display(Name = "Amount", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof(Resources))]
        public decimal Amount { get; set; }

        [Property]
        [Display(Name = "Method", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        public PaymentMethod Method { get; set; }

        [Property]
        [DataType(DataType.DateTime)]
        [Display(Name = "Date", ResourceType = typeof(Resources))]
        public DateTime Date { get; set; }

        [BelongsTo("cash_session")]
        [Display(Name = "CashSession", ResourceType = typeof(Resources))]
        public virtual CashSession CashSession { get; set; }

        [Property]
        [Display(Name = "Reference", ResourceType = typeof(Resources))]
        public string Reference { get; set; }
    }
}
