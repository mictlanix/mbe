// 
// CustomerReturnsController.cs
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
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Queries;
using NHibernate;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Controllers
{
    public class CustomerReturnsController : Controller
    {
        //
        // GET: /Returns/

        public ActionResult Index ()
		{
			var item = Configuration.Store;
			
			if (item == null) {
				return View ("InvalidStore");
			}
			
			var qry = from x in CustomerReturn.Queryable
                      where x.Store.Id == item.Id &&
							!x.IsCancelled &&
							!x.IsCompleted
                      orderby x.Id descending
                      select x;

			return View (qry.ToList ());
		}

        [HttpPost]
        public ActionResult Index (int id)
		{
			var qry = from x in SalesOrder.Queryable
					  where x.IsCompleted && !x.IsCancelled &&
							x.IsPaid && x.Id == id
                      select x;

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", qry.ToList ());
			} else {
				return View (new SalesOrder ());
			}
		}

        public ViewResult PrintCustomerReturn (int id)
		{
			CustomerReturn item = CustomerReturn.Find (id);

			return View ("_CustomerReturnTicket", item);
		}

        public ViewResult Historic()
        {
            DateRange item = new DateRange();
            item.StartDate = DateTime.Now;
            item.EndDate = DateTime.Now;

            return View("Historic", item);
        }

        [HttpPost]
        public ActionResult Historic(DateRange item, Search<CustomerReturn> search)
        {
            ViewBag.CustomerReturnDates = item;
            search.Limit = Configuration.PageSize;
            search = GetCustomerReturns(item, search);

            return PartialView("_Historic", search);
        }

        Search<CustomerReturn> GetCustomerReturns(DateRange dates, Search<CustomerReturn> search)
        {
            var qry = from x in CustomerReturn.Queryable
                      where (x.IsCompleted || x.IsCancelled) &&
                      (x.ModificationTime >= dates.StartDate.Date && x.ModificationTime <= dates.EndDate.Date.Add(new TimeSpan(23, 59, 59)))
                      orderby x.Id descending
                      select x;

            search.Total = qry.Count();
            search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();

            return search;
        }


        //
        // GET: /Returns/Details/5

        public ActionResult Details (int id)
        {
            CustomerReturn item = CustomerReturn.Find (id);
            return View (item);
        }

        public ViewResult HistoricDetails(int id)
        {
            CustomerReturn order = CustomerReturn.Find(id);

            return View(order);
        }

        //
        // GET: /Returns/Create
        [HttpPost]
		public ActionResult CreateFromSalesOrder (int id)
		{
			var sales = SalesOrder.Find (id);
			var item = new CustomerReturn ();
			
			// Store and Serial
			item.Store = sales.Store;
			
			try {
				item.Serial = (from x in CustomerReturn.Queryable
	            			   where x.Store.Id == item.Store.Id
	                      	   select x.Serial).Max () + 1;
			} catch {
				item.Serial = 1;
			}
			
			item.CreationTime = DateTime.Now;
			item.Creator = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			item.SalesOrder = sales;
			item.SalesPerson = sales.SalesPerson;
			item.Customer = sales.Customer;
			item.Updater = item.Creator;
			item.ModificationTime = item.CreationTime;

			using (var scope = new TransactionScope()) {
				item.Create ();

				foreach (var x in sales.Details) {
					var sum = GetReturnableQuantity (x.Id);

					if (sum <= 0)
						continue;
					
					var detail = new CustomerReturnDetail {
	                    Order = item,
	                    SalesOrderDetail = x,
	                    Product = x.Product,
	                    ProductCode = x.ProductCode,
	                    ProductName = x.ProductName,
	                    Discount = x.Discount,
	                    TaxRate = x.TaxRate,
	                    Price = x.Price,
	                    Quantity = sum
	                };

					detail.Create ();
				}
			}

			return RedirectToAction ("Details", new { id = item.Id });
		} 

        [HttpPost]
        public JsonResult EditDetailQuantity(int id, decimal quantity)
        {
            CustomerReturnDetail detail = CustomerReturnDetail.Find (id);
            decimal sum = GetReturnableQuantity (detail.SalesOrderDetail.Id);

			detail.Quantity = (quantity > 0 && quantity <= sum) ? quantity : sum;

			using (var scope = new TransactionScope ()) {
				detail.UpdateAndFlush ();
			}

            return Json(new { id = id, quantity = detail.Quantity, total = detail.Total.ToString("c") });
        }

        public ActionResult GetTotals (int id)
        {
            var item = CustomerReturn.Find (id);
            return PartialView ("_Totals", item);
        }

        [HttpPost]
        public JsonResult RemoveDetail(int id)
        {
            var item = CustomerReturnDetail.Find (id);

			using (var scope = new TransactionScope ()) {
				item.DeleteAndFlush ();
			}

            return Json(new { id = id, result = true });
        }

        // TODO: Falta realizar la salida del almacén 
        [HttpPost]
        public ActionResult ConfirmReturn (int id)
        {
			var item = CustomerReturn.Find (id);
			var dt = DateTime.Now;

			using (var scope = new TransactionScope ()) {
				foreach( var x in item.Details) {
					InventoryHelpers.ChangeNotification(TransactionType.CustomerReturn, item.Id, dt,
					                                    x.SalesOrderDetail.Warehouse, x.Product, x.Quantity);
				}
				
            	item.IsCompleted = true;
				item.UpdateAndFlush ();
			}

            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult CancelReturn (int id)
        {
            var item = CustomerReturn.Find(id);
			
			item.IsCancelled = true;

			using (var scope = new TransactionScope ()) {
				item.UpdateAndFlush ();
			}

            return RedirectToAction("Index");
        }

        decimal GetReturnableQuantity(int id)
        {
            var item = SalesOrderDetail.Find(id);
            string sql = @"SELECT SUM(d.quantity) quantity
                           FROM customer_return_detail d INNER JOIN customer_return m ON d.customer_return = m.customer_return_id
                           WHERE m.completed <> 0 AND m.cancelled = 0 AND d.sales_order_detail = :detail ";

            IList<decimal> quantities = (IList<decimal>)ActiveRecordMediator<CustomerReturnDetail>.Execute(
                delegate(ISession session, object instance) {
                    try {
                        return session.CreateSQLQuery(sql)
                        			  .SetParameter("detail", id)
                            		  .SetMaxResults(1)
                            		  .List<decimal>();
                    } catch (Exception) {
                        return null;
                    }
                }, null);

            if (quantities != null && quantities.Count > 0)
            {
                return item.Quantity - quantities[0];
            }

            return item.Quantity;
        }
    }
}
