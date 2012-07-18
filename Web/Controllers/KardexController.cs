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
    public class KardexController : Controller
    {
		public ViewResult Index ()
		{
			return View ();
		}

        //FIXME: Query optimization (SUM function)
        [HttpPost]
        public ActionResult Index(Warehouse item)
        {
            var qry = from x in Kardex.Queryable
                      where x.Warehouse.Id == item.Id
                      select new { product = x.Product, quantity = x.Quantity};
            var list = from x in qry.ToList()
                       group x by x.product into c
                       select new Kardex { Product = c.Key, Quantity = c.Sum(y => y.quantity) };
            
            var warehouse = Warehouse.Find(item.Id);

            return PartialView("_Index", new MasterDetails<Warehouse, Kardex> { Master = warehouse , Details = list.ToList() });
        }

        public ViewResult ProductDetails(int warehouse, int product)
        {
            ItemDateRange<Warehouse> item = new ItemDateRange<Warehouse>();
            item.Item = Warehouse.Find(warehouse);
            item.StartDate = DateTime.Now;
            item.EndDate = DateTime.Now;

            Product auxProduct = Product.Find(product);

            return View("ProductDetails", new Pair<ItemDateRange<Warehouse>, Product> { First = item , Second = auxProduct });
        }

        [HttpPost]
        public ActionResult ProductDetails(Pair<ItemDateRange<Warehouse>, Product> item)
        {
            var qry = from x in Kardex.Queryable
                          where x.Warehouse.Id == item.First.Item.Id && x.Product.Id == item.Second.Id &&
                          x.Date >= item.First.StartDate && x.Date <= item.First.EndDate.Add(new TimeSpan(23, 59, 59))
                          select x;
            var warehouse = Warehouse.Find(item.First.Item.Id);
          
            return PartialView("_ProductDetails", new MasterDetails<Warehouse, Kardex> { Master = warehouse , Details = qry.ToList() });
        }
	}
}

//x.Date >= start.Date && x.Date <= end.Date.Add(new TimeSpan(23, 59, 59))