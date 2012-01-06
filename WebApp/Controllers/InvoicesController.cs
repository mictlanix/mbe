// 
// InvoicesController.cs
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
using Business.Essentials.Model;
using Business.Essentials.WebApp.Models;
using Business.Essentials.WebApp.Helpers;

namespace Business.Essentials.WebApp.Controllers
{
    public class InvoicesController : Controller
    {
        public ViewResult Index()
        {
            var qry = from x in SalesInvoice.Queryable
					  orderby x.Id descending
                      select x;

            return View(qry.ToList());
        }

        public ViewResult New()
        {
            return View(new SalesInvoice { CustomerId = 1, Customer = Customer.Find(1) });
        }

        [HttpPost]
        public ActionResult New(SalesInvoice item)
        {
            item.Customer = Customer.TryFind(item.CustomerId);
            item.BillTo = Address.TryFind(item.BillToId);
			
			if (!ModelState.IsValid)
			{
				return View(item);
			}
			
			// Invoice creation info
            item.CreationTime = DateTime.Now;
            item.ModificationTime = item.CreationTime;
            item.Creator = SecurityHelpers.GetUser(User.Identity.Name).Employee;
            item.Updater = item.Creator;
			
			// Bill to info
			item.BillToTaxId = item.BillTo.TaxpayerId;
			item.BillToName = item.BillTo.TaxpayerName;
			item.Street = item.BillTo.Street;
			item.ExteriorNumber = item.BillTo.ExteriorNumber;
			item.InteriorNumber = item.BillTo.InteriorNumber;
			item.Neighborhood = item.BillTo.Neighborhood;
			item.Borough = item.BillTo.Borough;
			item.State = item.BillTo.State;
			item.Country = item.BillTo.Country;
			item.ZipCode = item.BillTo.ZipCode;
			
            using (var session = new SessionScope())
            {
                item.CreateAndFlush();
            }

            System.Diagnostics.Debug.WriteLine("New SalesInvoice [Id = {0}]", item.Id);

            if (item.Id == 0)
            {
                return View("UnknownError");
            }

            return RedirectToAction("Edit", new { id = item.Id });
        }

        public ViewResult Details(int id)
        {
            SalesInvoice order = SalesInvoice.Find(id);

            return View(order);
        }

        public ActionResult Edit(int id)
        {
            SalesInvoice item = SalesInvoice.Find(id);

            if (Request.IsAjaxRequest())
                return PartialView("_Edit", item);
            else
                return View(item);
        }

        [HttpPost]
        public ActionResult Edit(SalesInvoice item)
        {
            var order = SalesInvoice.Find(item.Id);
            var customer = Customer.Find(item.CustomerId);

            order.Customer = customer;
			
            order.Save();

            return PartialView("_SalesInfo", order);
        }

        [HttpPost]
        public ActionResult Confirm(int id)
        {
            SalesInvoice item = SalesInvoice.Find(id);

            item.IsCompleted = true;
            item.Save();

            return RedirectToAction("New");
        }

        [HttpPost]
        public ActionResult Cancel(int id)
        {
            SalesInvoice item = SalesInvoice.Find(id);

            item.IsCancelled = true;
            item.Save();

            return RedirectToAction("New");
        }
		
        public ViewResult Print(int id)
        {
            SalesInvoice item = SalesInvoice.Find(id);

            if(item.IsCompleted)
                return View("_SalesTicket", item);
            else
                return View("_SalesNote", item);
        }

        [HttpPost]
        public JsonResult AddDetail(int order, int product)
        {
            var p = Product.Find(product);

            var item = new SalesInvoiceDetail
            {
                Invoice = SalesInvoice.Find(order),
                Product = p,
                ProductCode = p.Code,
                ProductName = p.Name,
                Discount = 0,
                TaxRate = p.TaxRate,
                Quantity = 1,
            };

            using (var session = new SessionScope())
            {
                item.CreateAndFlush();
            }

            System.Diagnostics.Debug.WriteLine("New SalesInvoiceDetail [Id = {0}]", item.Id);

            return Json(new { id = item.Id });
        }

        [HttpPost]
        public JsonResult EditDetailQuantity(int id, decimal quantity)
        {
            SalesInvoiceDetail detail = SalesInvoiceDetail.Find(id);

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
            SalesInvoiceDetail detail = SalesInvoiceDetail.Find(id);
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

        public ActionResult GetSalesTotals(int id)
        {
            var order = SalesInvoice.Find(id);
            return PartialView("_SalesTotals", order);
        }

        public ActionResult GetSalesItem(int id)
        {
            return PartialView("_SalesItem", SalesInvoiceDetail.Find(id));
        }

        [HttpPost]
        public JsonResult RemoveDetail(int id)
        {
            SalesInvoiceDetail item = SalesInvoiceDetail.Find(id);
            item.Delete();
            return Json(new { id = id, result = true });
        }
    }
}
