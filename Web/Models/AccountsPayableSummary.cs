// 
// AccountsPayableSummary.cs
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
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Web.Mvc;
using System.Linq;
using System.Web.Security;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Models
{

    public class AccountsPayableSummary
    {
        [Display(Name = "Supplier", ResourceType = typeof(Resources))]
        public Supplier Supplier { get; set; }

        [DataType(DataType.Currency)]
		[Display(Name = "PaymentsSummary", ResourceType = typeof(Resources))]
        public decimal PaymentsSummary { get; set; }

        [DataType(DataType.Currency)]
		[Display(Name = "PurchasesSummary", ResourceType = typeof(Resources))]
        public decimal PurchasesSummary { get; set; }

        [DataType(DataType.Currency)]
		[Display(Name = "ReturnsSummary", ResourceType = typeof(Resources))]
        public decimal ReturnsSummary { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "AvailableCredit", ResourceType = typeof(Resources))]
        public decimal AvailableCredit
        { get 
            {
                if ((Supplier.CreditLimit - Math.Abs(Balance)) < 0) { return 0m; }
                else { return Balance + Supplier.CreditLimit; }      
            } 
        }

        [DataType(DataType.Currency)]
        [Display(Name = "Balance", ResourceType = typeof(Resources))]
        public decimal Balance 
        { 
            get 
            {
                return ReturnsSummary + PaymentsSummary - PurchasesSummary;
            } 
        }
    }
    
}