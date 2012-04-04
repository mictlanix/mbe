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

        public ViewResult PrintSupplierReturn(int id)
        {
            SupplierReturn item = SupplierReturn.Find(id);

            return View("_PrintSupplierReturn", item);
        }


        public ViewResult Historic()
        {
            var qry = from x in SupplierReturn.Queryable
                      where x.IsCompleted || x.IsCancelled
                      orderby x.Id descending
                      select x;

            return View(qry.ToList());
        }

        public ViewResult HistoricDetails(int id)
        {
            SupplierReturn order = SupplierReturn.Find(id);

            return View(order);
        }


        //
        // GET: /Returns/Details/5

        public ActionResult EditSupplierReturn(int id)
        {
            SupplierReturn item = SupplierReturn.Find(id);
            
            item.ModificationTime = DateTime.Now;
            item.Updater = SecurityHelpers.GetUser(User.Identity.Name).Employee;

            item.Save();

            return View(item);
        }

        //
        // GET: /Returns/Create
        [HttpPost]
        public ActionResult CreateFromPurchaseOrder(int id)
        {
            PurchaseOrder purchase = PurchaseOrder.Find(id);
            
            SupplierReturn item = new SupplierReturn();
            item.CreationTime = DateTime.Now;
            item.Creator = SecurityHelpers.GetUser(User.Identity.Name).Employee;
            item.PurchaseOrder = purchase;
            item.Supplier = purchase.Supplier;
            item.Updater = SecurityHelpers.GetUser(User.Identity.Name).Employee;
            item.ModificationTime = DateTime.Now;

            item.Create();

            
            foreach (var x in purchase.Details)
            {
                var sum = GetReturnableQuantity(x.Id);

                if (sum > 0)
                {
                    var detail = new SupplierReturnDetail
                    {
                        Order = item,
                        PurchaseOrderDetail = x,
                        Product = x.Product,
                        ProductCode = x.ProductCode,
                        ProductName = x.ProductName,
                        Discount = x.Discount,
                        TaxRate = x.TaxRate,
                        Price = x.Price,
                        Quantity = sum,
                        Warehouse = x.Warehouse
                    };

                    using (var session = new SessionScope())
                    {
                        detail.CreateAndFlush();
                    }
                }
            }
                return RedirectToAction("EditSupplierReturn", new { id = item.Id });
        }

        [HttpPost]
        public JsonResult EditDetailQuantity(int id, decimal quantity)
        {
            SupplierReturnDetail detail = SupplierReturnDetail.Find(id);

            var sum = GetReturnableQuantity(detail.PurchaseOrderDetail.Id);

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
            var order = SupplierReturn.Find(id);
            return PartialView("_ReturnTotals", order);
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
            SupplierReturn item = SupplierReturn.Find(id);

            var qry = from x in SupplierReturnDetail.Queryable
                      where x.Order.Id == id
                      group x by x.Warehouse into g
                      select new { Warehouse = g.Key, Details = g.ToList() };

            foreach (var x in qry)
            {
                var master = new InventoryIssue
                {
                    Return = item,
                    Warehouse = x.Warehouse,
                    CreationTime = DateTime.Now,
                    Creator = SecurityHelpers.GetUser(User.Identity.Name).Employee,
                    ModificationTime = DateTime.Now,
                    Updater = SecurityHelpers.GetUser(User.Identity.Name).Employee,
                    Comment = string.Format(Resources.Message_SupplierReturn, item.Supplier.Name, item.PurchaseOrder.Id, item.Id)
                };

                using (var session = new SessionScope())
                {
                    master.CreateAndFlush();
                }

                foreach (var y in x.Details)
                {
                    var detail = new InventoryIssueDetail
                    {
                        Issue = master,
                        Product = y.Product,
                        Quantity = y.Quantity,
                        ProductCode = y.ProductCode,
                        ProductName = y.ProductName
                    };

                    using (var session = new SessionScope())
                    {
                        detail.CreateAndFlush();
                    }
                }
            }

            item.IsCompleted = true;
            item.Save();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult CancelReturn(int id)
        {
            SupplierReturn item = SupplierReturn.Find(id);

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
            var item = PurchaseOrderDetail.Find(id);
            string sql = @"SELECT SUM(d.quantity) quantity
                           FROM supplier_return_detail d INNER JOIN supplier_return m ON d.supplier_return = m.supplier_return_id
                           WHERE m.completed <> 0 AND d.purchase_order_detail = :detail ";

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
