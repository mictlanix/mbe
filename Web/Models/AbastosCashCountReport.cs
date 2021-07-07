
// 
// CashCount.cs
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
using Castle.ActiveRecord;

namespace Mictlanix.BE.Web.Models
{
    public class AbastosCashCountReport
    {
        [DisplayFormat(DataFormatString = "{0:000000}")]
        public int SessionId { get; set; }

        [Display(Name = "Cashier", ResourceType = typeof(Resources))]
        public Employee Cashier { get; set; }

        [Display(Name = "CashDrawer", ResourceType = typeof(Resources))]
        public CashDrawer CashDrawer { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "StartDate", ResourceType = typeof(Resources))]
        public DateTime Start { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "End", ResourceType = typeof(Resources))]
        public DateTime? End { get; set; }

	[DataType(DataType.Currency)]
        [Display(Name = "Expenses", ResourceType = typeof(Resources))]
        public decimal TotalExpenses {
            get {
                return (decimal?)Expenses.Sum(x => x.Amount)??0;
            }
        }

	[DataType(DataType.Currency)]
        [Display(Name = "Sales", ResourceType = typeof(Resources))]
        public decimal TotalSales {
            get {
                return (decimal?)Sales.Sum(x => x.Price * x.Quantity)??0;
            }
        }

	[DataType(DataType.Currency)]
        [Display(Name = "Quantity", ResourceType = typeof(Resources))]
        public decimal TotalQuantitySales {
            get {
                return (decimal?)Sales.Sum(x => x.Quantity)??0;
            }
        }
	
	[DataType(DataType.Currency)]
        [Display(Name = "AccountReceivables", ResourceType = typeof(Resources))]
        public decimal TotalReceivables {
            get {
                return (decimal?)Receivables.Sum(x => x.Amount)??0;
            }
        }
		
	[DataType(DataType.Currency)]
        [Display(Name = "AccountPayables", ResourceType = typeof(Resources))]
        public decimal TotalPayables {
            get {
                return (decimal?)Payables.Sum(x => x.Amount)??0;
            }
        }



        public IList<AbastosAccountPayable> Payables { get; set; }
        public IList<AbastosAccountReceivable> Receivables { get; set; }
	public IList<AbastosSalesOrder> Sales { get; set; }
	public IList<AbastosExpense> Expenses { get; set; }
	public IList<AbastosStock> Stock { get; set; }
    }

	[ActiveRecord]
	public class AbastosSalesOrder {

		public string Customer { get; set; }
		public string Product { get; set; }
		public DateTime Date { get; set; }
		public string UnitOfMeasure { get; set; }
		public decimal Quantity { get; set; }
		public decimal Price { get; set; }
		public decimal Subtotal { get; set; }
		public string Term { get; set; }
		public int Reference { get; set; }
		public string LotCode { get; set; }
	}

	public class AbastosStock {
		public string Product { get; set; }
		public string LotCode { get; set; }
		public string Warehouse { get; set; }
		public decimal Quantity { get; set; }
	}

	public class AbastosExpense {
		public string Cashier { get; set; }

		[DataType(DataType.DateTime)]
		public DateTime Date { get; set; }
		public string Concept { get; set; }
		public decimal Amount { get; set; }
		public string Comment { get; set; }

	}

	public class AbastosAccountReceivable {
		public string Customer { get; set; }

		[DataType(DataType.DateTime)]
		public DateTime Date { get; set; }
		public decimal Amount { get; set; }
		public PaymentMethod PaymentMethod { get; set; }

	}

	public class AbastosAccountPayable {
		public string Supplier { get; set; }

		[DataType(DataType.DateTime)]
		public DateTime Date { get; set; }
		public decimal Amount { get; set; }
		public PaymentMethod PaymentMethod { get; set; }

	}

}