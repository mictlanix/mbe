// 
// CashCut.cs
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
using System.Globalization;
using System.Web.Mvc;
using System.Linq;
using System.Web.Security;
using Business.Essentials.Model;

namespace Business.Essentials.WebApp.Models
{

    public class CashCountReport
    {
        public int SessionId { get; set; }

        [Display(Name = "Cashier", ResourceType = typeof(Resources))]
        public Employee Cashier { get; set; }

        [Display(Name = "CashDrawer", ResourceType = typeof(Resources))]
        public CashDrawer CashDrawer { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Start", ResourceType = typeof(Resources))]
        public DateTime Start { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "End", ResourceType = typeof(Resources))]
        public DateTime? End { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "StartingCash", ResourceType = typeof(Resources))]
        public decimal StartingCash { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "CashSales", ResourceType = typeof(Resources))]
        public decimal CashSales
        {
            get
            {
                var item = MoneyCounts.SingleOrDefault(x => x.Type == PaymentMethod.Cash);
                return item == null ? 0 : item.Amount;
            }
        }

        [DataType(DataType.Currency)]
        [Display(Name = "CashInDrawer", ResourceType = typeof(Resources))]
        public decimal CashInDrawer
        {
            get
            {
               return  CashSales + StartingCash;
            }
        }

        [DataType(DataType.Currency)]
        [Display(Name = "Balance", ResourceType = typeof(Resources))]
        public decimal Balance
        {
            get
            {
                return Math.Abs(CountedCash - CashInDrawer);
            }
        }

        [DataType(DataType.Currency)]
        [Display(Name = "CountedCash", ResourceType = typeof(Resources))]
        public decimal CountedCash
        {
            get { return CashCounts.Where(x => x.Type == CashCountType.CountedCash).Sum(x => x.Total); }
        }

        public IList<MoneyCount> MoneyCounts { get; set;}
        public IList<CashCount> CashCounts { get; set; }
    }
    
}