// 
// SalesController.cs
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
using Business.Essentials.Model;
using Business.Essentials.WebApp.Models;
using Business.Essentials.WebApp.Helpers;

namespace Business.Essentials.WebApp.Controllers
{
    public class SalesController : Controller
    {
        //
        // GET: /Sales/

        public ViewResult Index()
        {
            var qry = from x in SalesOrder.Queryable
                      where !x.IsCancelled && !x.IsCompleted
                      select x;

            return View(qry.ToList());
        }

        // GET: /Sales/PrintOrder/

        public ViewResult PrintOrder(int id)
        {
            SalesOrder item = SalesOrder.Find(id);

            if(item.IsCompleted)
                return View("_SalesTicket", item);
            else
                return View("_SalesNote", item);
        }

        // GET: /Sales/Details/

        public ViewResult Details(int id)
        {
            SalesOrder order = SalesOrder.Find(id);

            return View(order);
        }

        //
        // GET: /Sales/New

        public ViewResult New()
        {
            if (GetPoS() == null)
            {
                return View("InvalidPointOfSale");
            }

            return View(new SalesOrder { CustomerId = 1, Customer = Customer.Find(1) });
        }

        [HttpPost]
        public ActionResult New(SalesOrder item)
        {
            item.PointOfSale = GetPoS();

            if (item.PointOfSale == null)
            {
                return View("InvalidPointOfSale");
            }

            item.Customer = Customer.Find(item.CustomerId);
            item.SalesPerson = SecurityHelpers.GetUser(User.Identity.Name).Employee;
            item.Date = DateTime.Now;
            item.DueDate = item.IsCredit ? item.Date.AddDays(item.Customer.CreditDays) : item.Date;

            using (var session = new SessionScope())
            {
                item.CreateAndFlush();
            }

            System.Diagnostics.Debug.WriteLine("New SalesOrder [Id = {0}]", item.Id);

            if (item.Id == 0)
            {
                return View("UnknownError");
            }

            return RedirectToAction("Edit", new { id = item.Id });
        }

        public ActionResult Edit(int id)
        {
            SalesOrder item = SalesOrder.Find(id);

            if (Request.IsAjaxRequest())
                return PartialView("_Edit", item);
            else
                return View(item);
        }

        //
        // POST: /Customer/Edit/5

        [HttpPost]
        public ActionResult Edit(SalesOrder item)
        {
            var order = SalesOrder.Find(item.Id);
            var customer = Customer.Find(item.CustomerId);

            order.Customer = customer;
            order.IsCredit = item.IsCredit;
            order.DueDate = item.IsCredit ? order.Date.AddDays(customer.CreditDays) : order.Date;

            order.Save();

            return PartialView("_SalesInfo", order);
        }

        [HttpPost]
        public JsonResult AddDetail(int order, int product)
        {
            var p = Product.Find(product);

            var item = new SalesOrderDetail
            {
                SalesOrder = SalesOrder.Find(order),
                Product = p,
                ProductCode = p.Code,
                ProductName = p.Name,
                Discount = 0,
                TaxRate = p.TaxRate,
                Quantity = 1,
            };

            switch (item.SalesOrder.Customer.PriceList.Id)
            {
                case 1:
                    item.Price = p.Price1;
                    break;
                case 2:
                    item.Price = p.Price2;
                    break;
                case 3:
                    item.Price = p.Price3;
                    break;
                case 4:
                    item.Price = p.Price4;
                    break;
            }

            using (var session = new SessionScope())
            {
                item.CreateAndFlush();
            }

            System.Diagnostics.Debug.WriteLine("New SalesOrderDetail [Id = {0}]", item.Id);

            return Json(new { id = item.Id });
        }

        [HttpPost]
        public JsonResult EditDetailQuantity(int id, decimal quantity)
        {
            SalesOrderDetail detail = SalesOrderDetail.Find(id);

            if (quantity > 0)
            {
                detail.Quantity = quantity;
                detail.Save();
            }

            return Json(new { id = id, quantity = detail.Quantity, total = detail.Total.ToString("c") });
        }

        [HttpPost]
        public JsonResult EditDetailDiscount(int id, string value)
        {
            SalesOrderDetail detail = SalesOrderDetail.Find(id);
            bool success;
            decimal discount;

            success = decimal.TryParse(value.TrimEnd(new char[] { ' ', '%' }), out discount);
            discount /= 100m;

            if (success && discount >= 0 && discount <= 1)
            {
                detail.Discount = discount;
                detail.Save();
            }

            return Json(new { id = id, discount = detail.Discount.ToString("p"), total = detail.Total.ToString("c") });
        }

        //GET/SalesTotals

        public ActionResult GetSalesTotals(int id)
        {
            var order = SalesOrder.Find(id);
            return PartialView("_SalesTotals", order);
        }

        //GET/SalesTotals/{id}

        public ActionResult GetSalesItem(int id)
        {
            return PartialView("_SalesItem", SalesOrderDetail.Find(id));
        }

        [HttpPost]
        public JsonResult RemoveDetail(int id)
        {
            SalesOrderDetail item = SalesOrderDetail.Find(id);
            item.Delete();
            return Json(new { id = id, result = true });
        }

        [HttpPost]
        public ActionResult ConfirmOrder(int id)
        {
            SalesOrder item = SalesOrder.Find(id);

            item.IsCompleted = true;
            item.Save();

            return RedirectToAction("New");
        }

        [HttpPost]
        public ActionResult CancelOrder(int id)
        {
            SalesOrder item = SalesOrder.Find(id);

            item.IsCancelled = true;
            item.Save();

            return RedirectToAction("New");
        }

        PointOfSale GetPoS()
        {
            var addr = Request.UserHostAddress;
            return PointOfSale.Queryable.FirstOrDefault();//.SingleOrDefault(x => x.HostAddress == addr);
        }
    }
}
