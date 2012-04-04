// 
// ReturnsCustomerController.cs
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
    public class ReturnsCustomerController : Controller
    {
        //
        // GET: /Returns/

        public ActionResult Index ()
		{
			var item = GetStore ();
			
			if (item == null) {
				return View ("InvalidStore");
			}
			
			var qry = from x in ReturnCustomer.Queryable
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
                      where x.IsCompleted && x.IsPaid &&
                            x.Id == id
                      select x;

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", qry.ToList ());
			} else {
				return View (new SalesOrder ());
			}
		}

        public ViewResult PrintCustomerReturn (int id)
		{
			ReturnCustomer item = ReturnCustomer.Find (id);

			return View ("_CustomerReturnTicket", item);
		}

        public ViewResult Historic()
        {
            var qry = from x in ReturnCustomer.Queryable
                      where x.IsCompleted || x.IsCancelled
                      orderby x.Id descending
                      select x;

            return View(qry.ToList());
        }


        //
        // GET: /Returns/Details/5

        public ActionResult Details(int id)
        {
            ReturnCustomer item = ReturnCustomer.Find(id);

            item.ModificationTime = DateTime.Now;
            item.Updater = SecurityHelpers.GetUser(User.Identity.Name).Employee;

            item.Save();

            return View(item);
        }

        public ViewResult HistoricDetails(int id)
        {
            ReturnCustomer order = ReturnCustomer.Find(id);

            return View(order);
        }

        //
        // GET: /Returns/Create
        [HttpPost]
        public ActionResult CreateFromSalesOrder (int id)
		{
			SalesOrder sales = SalesOrder.Find (id);

			var item = new ReturnCustomer ();
			
			// Store and Serial
			item.Store = sales.Store;
			
			try {
				item.Serial = (from x in ReturnCustomer.Queryable
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
			item.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			item.ModificationTime = DateTime.Now;

			item.Create ();

			foreach (var x in sales.Details) {
				var sum = GetReturnableQuantity (x.Id);

				if (sum > 0) {
					var detail = new ReturnCustomerDetail
                    {
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

					using (var session = new SessionScope()) {
						detail.CreateAndFlush ();
					}
				}
			}

			return RedirectToAction ("Details", new { id = item.Id });
		} 

        [HttpPost]
        public JsonResult EditDetailQuantity(int id, decimal quantity)
        {
            ReturnCustomerDetail detail = ReturnCustomerDetail.Find(id);

            decimal sum = GetReturnableQuantity(detail.SalesOrderDetail.Id);

            if (quantity > 0 && quantity <= sum)
            {
                detail.Quantity = quantity;
                detail.Save();
                return Json(new { id = id, quantity = detail.Quantity, total = detail.Total.ToString("c") });
            }
            else
            {
                detail.Quantity = sum;
                detail.Save();
                return Json(new { id = id, quantity = sum, total = detail.Total.ToString("c") });
            }

        }

        public ActionResult GetReturnTotals(int id)
        {
            var order = ReturnCustomer.Find(id);
            return PartialView("_ReturnTotals", order);
        }

        [HttpPost]
        public JsonResult RemoveDetail(int id)
        {
            ReturnCustomerDetail item = ReturnCustomerDetail.Find(id);
            item.Delete();
            return Json(new { id = id, result = true });
        }

        [HttpPost]
        public ActionResult ConfirmReturn(int id)
        {
            ReturnCustomer item = ReturnCustomer.Find(id);

            item.IsCompleted = true;
            item.Save();
            //TODO/ Falta realizar la salida del almacén 
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult CancelReturn(int id)
        {
            ReturnCustomer item = ReturnCustomer.Find(id);

            foreach (var x in item.Details)
            {
                x.Delete();
            }

            item.IsCancelled = true;
            item.Save();

            return RedirectToAction("Index");
        }

        decimal GetReturnableQuantity(int id)
        {
            var item = SalesOrderDetail.Find(id);
            string sql = @"SELECT SUM(d.quantity) quantity
                           FROM return_customer_detail d INNER JOIN return_customer m ON d.return_customer = m.return_customer_id
                           WHERE m.completed <> 0 AND d.sales_order_detail = :detail ";

            IList<decimal> quantities = (IList<decimal>)ActiveRecordMediator<ReturnCustomerDetail>.Execute(
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
		
		Store GetStore ()
		{
			if (Request.Cookies ["Store"] != null) {
				return Store.TryFind (int.Parse (Request.Cookies ["Store"].Value));
			}
			
			return null;
		}
    }
}
