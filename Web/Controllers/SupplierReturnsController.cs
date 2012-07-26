// 
// SupplierReturnsController.cs
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
    public class SupplierReturnsController : Controller
    {
        //
        // GET: /SupplierReturns/

        public ActionResult Index()
        {
            var qry = from x in SupplierReturn.Queryable
                      where !x.IsCancelled && !x.IsCompleted
                      orderby x.Id descending
                      select x;

            return View(qry.ToList());
        }

        [HttpPost]
        public ActionResult Index(int id)
        {
            var qry = from x in PurchaseOrder.Queryable
                      where x.IsCompleted && x.Id == id
                      select x;

            if (Request.IsAjaxRequest())
            {
                return PartialView("_Index", qry.ToList());
            }
            else
            {
                return View(new PurchaseOrder());

            }
        }

        public ViewResult Print(int id)
        {
            return View(SupplierReturn.Find (id));
        }

        public ViewResult Historic ()
        {
            DateRange item = new DateRange();
            item.StartDate = DateTime.Now;
            item.EndDate = DateTime.Now;

            return View("Historic", item);
        }

        [HttpPost]
        public ActionResult Historic(DateRange item, Search<SupplierReturn> search)
        {
            ViewBag.SupplierReturnsDates = item;
            search.Limit = Configuration.PageSize;
            search = GetSupplierReturns(item, search);

            return PartialView("_Historic", search);
        }

        Search<SupplierReturn> GetSupplierReturns(DateRange dates, Search<SupplierReturn> search)
        {
            var qry = from x in SupplierReturn.Queryable
                      where (x.IsCompleted || x.IsCancelled) &&
                      (x.ModificationTime >= dates.StartDate.Date && x.ModificationTime <= dates.EndDate.Date.Add(new TimeSpan(23, 59, 59)))
                      orderby x.Id descending
                      select x;

            search.Total = qry.Count();
            search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();

            return search;
        }

        public ViewResult HistoricDetails (int id)
        {
            var item = SupplierReturn.Find (id);

            return View (item);
        }


        //
        // GET: /Returns/Details/5

        public ActionResult EditSupplierReturn (int id)
        {
            var item = SupplierReturn.Find(id);
            
            item.ModificationTime = DateTime.Now;
            item.Updater = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			
			using (var scope = new TransactionScope ()) {
				item.UpdateAndFlush ();
			}

            return View (item);
        }

        //
        // GET: /Returns/Create
        [HttpPost]
        public ActionResult CreateFromPurchaseOrder(int id)
        {
            var purchase = PurchaseOrder.Find (id);
            var item = new SupplierReturn();

            item.CreationTime = DateTime.Now;
            item.ModificationTime = item.CreationTime;
            item.Creator = SecurityHelpers.GetUser(User.Identity.Name).Employee;
            item.Updater = item.Creator;
            item.PurchaseOrder = purchase;
            item.Supplier = purchase.Supplier;

            using (var scope = new TransactionScope()) {
            	item.Create();

	            foreach (var x in purchase.Details) {
	                var qty = GetReturnableQuantity(x.Id);

	                if (qty <= 0)
						continue;

					var detail = new SupplierReturnDetail {
                        Order = item,
                        PurchaseOrderDetail = x,
                        Product = x.Product,
                        ProductCode = x.ProductCode,
                        ProductName = x.ProductName,
                        Discount = x.Discount,
                        TaxRate = x.TaxRate,
                        Price = x.Price,
                        Quantity = qty,
                        Warehouse = x.Warehouse
                    };

					detail.Create ();
                }
            }

			return RedirectToAction("EditSupplierReturn", new { id = item.Id });
        }

        [HttpPost]
        public JsonResult EditDetailQuantity (int id, decimal quantity)
        {
            SupplierReturnDetail detail = SupplierReturnDetail.Find (id);
            var sum = GetReturnableQuantity (detail.PurchaseOrderDetail.Id);

			detail.Quantity = (quantity > 0 && quantity <= sum) ? quantity : sum;

			using (var scope = new TransactionScope ()) {
				detail.UpdateAndFlush ();
			}

            return Json (new { id = id, quantity = detail.Quantity, total = detail.Total.ToString ("c") });
        }

        public ActionResult GetTotals (int id)
        {
            var item = SupplierReturn.Find (id);
            return PartialView ("_Totals", item);
        }

        [HttpPost]
        public JsonResult RemoveDetail(int id)
        {
            SupplierReturnDetail item = SupplierReturnDetail.Find(id);
            item.Delete();
            return Json(new { id = id, result = true });
        }

        [HttpPost]
        public ActionResult ConfirmReturn(int id)
        {
            var item = SupplierReturn.Find (id);

            var qry = from x in item.Details
                      where x.Order.Id == id
                      group x by x.Warehouse into g
                      select new { Warehouse = g.Key, Details = g.ToList() };

			var dt = DateTime.Now;
			var employee = SecurityHelpers.GetUser(User.Identity.Name).Employee;
			
            using (var scope = new TransactionScope()) {
	            foreach (var x in qry) {
	                var master = new InventoryIssue {
	                    Return = item,
	                    Warehouse = x.Warehouse,
	                    CreationTime = dt,
	                    ModificationTime = dt,
	                    Creator = employee,
	                    Updater = employee,
	                    Comment = string.Format(Resources.Message_SupplierReturn, item.Supplier.Name, item.PurchaseOrder.Id, item.Id)
	                };

	               	master.Create ();

	                foreach (var y in x.Details) {
	                    var detail = new InventoryIssueDetail {
	                        Issue = master,
	                        Product = y.Product,
	                        Quantity = y.Quantity,
	                        ProductCode = y.ProductCode,
	                        ProductName = y.ProductName
	                    };

						detail.Create ();
	                }
	            }

	            item.IsCompleted = true;
	            item.UpdateAndFlush ();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
		public ActionResult CancelReturn (int id)
		{
			var item = SupplierReturn.Find (id);

			item.IsCancelled = true;

			using (var scope = new TransactionScope()) {
				item.UpdateAndFlush ();
			}

            return RedirectToAction ("Index");
        }

        decimal GetReturnableQuantity(int id)
        {
            var item = PurchaseOrderDetail.Find(id);
            string sql = @"SELECT SUM(d.quantity) quantity
                           FROM supplier_return_detail d INNER JOIN supplier_return m ON d.supplier_return = m.supplier_return_id
                           WHERE m.completed <> 0 AND m.cancelled = 0 AND d.purchase_order_detail = :detail ";

            IList<decimal> quantities = (IList<decimal>)ActiveRecordMediator<SupplierReturnDetail>.Execute(
                delegate(ISession session, object instance)
                {
                    try
                    {
                        return session.CreateSQLQuery(sql)
                            .SetParameter("detail", id)
                            .SetMaxResults(1)
                            .List<decimal>();
                    }
                    catch (Exception)
                    {
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
