// 
// KardexController.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Castle.ActiveRecord;
using NHibernate.Exceptions;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Controllers
{
    public class ReportsController : Controller
    {
		public ViewResult Kardex ()
		{
			return View ();
		}

        //FIXME: Query optimization (SUM function)
        [HttpPost]
        public ActionResult Kardex(Warehouse item)
        {
            var qry = from x in Model.Kardex.Queryable
                      where x.Warehouse.Id == item.Id
                      select new { product = x.Product, quantity = x.Quantity};
            var list = from x in qry.ToList()
                       group x by x.product into c
                       select new Kardex { Product = c.Key, Quantity = c.Sum(y => y.quantity) };
            
            var warehouse = Warehouse.Find(item.Id);

            return PartialView("_Kardex", new MasterDetails<Warehouse, Kardex> { Master = warehouse , Details = list.ToList() });
        }

		public ViewResult KardexDetails(int warehouse, int product)
        {
            var item = new DateRange();
            item.StartDate = DateTime.Now;
            item.EndDate = DateTime.Now;

            ViewBag.Warehouse = Warehouse.Find(warehouse);
            ViewBag.Product = Product.Find(product);

            return View(item);
        }

        [HttpPost]
		public ActionResult KardexDetails(int warehouse, int product, DateRange item)
        {
            var qry = from x in Model.Kardex.Queryable
                      where x.Warehouse.Id == warehouse && x.Product.Id == product &&
                            x.Date >= item.StartDate.Date && x.Date <= item.EndDate.Date.Add(new TimeSpan(23, 59, 59))
                      select x;
          
			return PartialView("_KardexDetails", qry.ToList());
        }

		public ViewResult SalesOrdersHistoric ()
		{
			DateRange item = new DateRange();
			item.StartDate = DateTime.Now;
			item.EndDate = DateTime.Now;
			
			return View(item);
		}
		
		[HttpPost]
		public ActionResult SalesOrdersHistoric(DateRange item, Search<SalesOrder> search)
		{
			ViewBag.Dates = item;
			search.Limit = Configuration.PageSize;   
			search = GetSalesOrder(item, search);
			
			return PartialView("_SalesOrdersHistoric", search);
		}
		
		public ViewResult SalesOrdersHistoricDetails (int id)
		{
			var item = SalesOrder.Find (id);
			
			item.Details.ToList ();
			
			return View (item);
		}

		Search<SalesOrder> GetSalesOrder(DateRange dates, Search<SalesOrder> search)
		{
			var qry = from x in SalesOrder.Queryable
				where (x.IsCompleted || x.IsCancelled) &&
					(x.Date >= dates.StartDate.Date && x.Date <= dates.EndDate.Date.Add(new TimeSpan(23, 59, 59)))
					orderby x.Id descending
					select x;
			
			search.Total = qry.Count();
			search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
			
			return search;
		}

		public ViewResult ReceivedPayments ()
		{
			return View (new DateRange(DateTime.Now, DateTime.Now));
		}

		[HttpPost]
		public ActionResult ReceivedPayments (int store, DateRange dates)
		{
			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			var qry = from x in CustomerPayment.Queryable
					  where x.Store.Id == store &&
							x.Date >= start && x.Date <= end &&
							x.Amount > 0
					  select new ReceivedPayment {
						Date = x.Date,
						SalesOrder = x.SalesOrder.Id,
						Serial = x.SalesOrder.Serial,
						Customer = x.Customer,
						Method = x.Method,
						Amount = x.Amount };



			return PartialView("_ReceivedPayments", qry.ToList());
		}

	}
}
