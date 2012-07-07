// 
// PurchasesController.cs
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
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Controllers
{
    public class PurchasesController : Controller
    {
        //
        // GET: /Purchases/

        public ActionResult Index()
        {
            var qry = from x in PurchaseOrder.Queryable
                      orderby x.Id descending
                      select x;

            return View(qry.ToList());
        }

        // GET: /Purchases/PrintPurchase/

        public ViewResult PrintPurchase(int id)
        {
            PurchaseOrder item = PurchaseOrder.Find(id);

            return View("_PrintPurchase", item);
        }

        //
        // GET: /Purchases/Details/5

        public ActionResult Details(int id)
        {
            PurchaseOrder item = PurchaseOrder.Find(id);

            return View(item);
        }

        //
        // GET: /Purchases/Create

        public ActionResult NewPurchase()
        {
            return View(new PurchaseOrder());
        } 

        //
        // POST: /Purchases/Create

        [HttpPost]
        public ActionResult NewPurchase(PurchaseOrder item)
        {
            item.Supplier = Supplier.Find(item.SupplierId);
            item.Creator = SecurityHelpers.GetUser(User.Identity.Name).Employee;
            item.Updater = item.Creator;
            item.CreationTime = DateTime.Now;
            item.ModificationTime = item.CreationTime;

            using (var scope = new TransactionScope()) {
                item.CreateAndFlush();
            }

            System.Diagnostics.Debug.WriteLine("New Purchase [Id = {0}]", item.Id);

            return RedirectToAction("EditPurchase", new { id = item.Id });
        }

        
        //
        // GET: /Purchases/Edit/5
 
        public ActionResult EditPurchase(int id)
        {
            PurchaseOrder item = PurchaseOrder.Find(id);

            if (item.IsCompleted || item.IsCancelled)
            {
                return RedirectToAction("Details", new { id = item.Id });
            }

            if (Request.IsAjaxRequest())
                return PartialView("_PurchaseEditor", item);
            else
                return View(item);
        }

        //
        // POST: /Purchases/Edit/5

        [HttpPost]
        public ActionResult EditPurchase(PurchaseOrder item)
        {
            var order = PurchaseOrder.Find(item.Id);

            order.Supplier = Supplier.Find(item.SupplierId);
            order.Updater = SecurityHelpers.GetUser(User.Identity.Name).Employee;
            order.ModificationTime = DateTime.Now;
            order.Comment = item.Comment;

			using (var scope = new TransactionScope ()) {
            	order.UpdateAndFlush ();
			}

            return PartialView("_PurchaseInfo", order);

        }

        //
        // POST: /Purchases/AddPurchaseDetail

        [HttpPost]
        public JsonResult AddPurchaseDetail(int movement, int warehouse, int product)
        {
            var p = Product.Find(product);

            var item = new PurchaseOrderDetail
            {
                Order = PurchaseOrder.Find(movement),
                Warehouse = Warehouse.Find(warehouse),
                Product = p,
                ProductCode = p.Code,
                ProductName = p.Name,
                Quantity = 1,
                Price = p.Cost,
                TaxRate = p.TaxRate,
                Discount = 0
            };

            using (var scope = new TransactionScope()) {
                item.CreateAndFlush();
            }

            System.Diagnostics.Debug.WriteLine("New PurchaseDetail [Id = {0}]", item.Id);

            return Json(new { id = item.Id });
        }

        //
        // POST: /Purchases/EditPurchaseDetailQty

        [HttpPost]
        public JsonResult EditPurchaseDetailQty(int id, decimal quantity)
        {
            PurchaseOrderDetail detail = PurchaseOrderDetail.Find(id);

            if (quantity > 0)
            {
                detail.Quantity = quantity;

				using (var scope = new TransactionScope ()) {
	            	detail.UpdateAndFlush ();
				}
            }

            return Json(new { id = id, quantity = detail.Quantity, total = detail.Total.ToString("c") });
        }

        //
        // POST: /Purchases/EditPurchaseDetailPrice

        [HttpPost]
        public JsonResult EditPurchaseDetailPrice(int id, string value)
        {
            PurchaseOrderDetail detail = PurchaseOrderDetail.Find(id);
            bool success;
            decimal price;

            success = decimal.TryParse(value,System.Globalization.NumberStyles.AllowCurrencySymbol, null, out price);

            if (success && price >= 0)
            {
                detail.Price = price;

				using (var scope = new TransactionScope ()) {
	            	detail.UpdateAndFlush ();
				}
            }

            return Json(new { id = id, price = detail.Price.ToString("c"), total = detail.Total.ToString("c") });
        }

        //
        // POST: /Purchases/EditPurchaseDetailDiscount

        [HttpPost]
        public JsonResult EditPurchaseDetailDiscount(int id, string value)
        {
            PurchaseOrderDetail detail = PurchaseOrderDetail.Find(id);
            bool success;
            decimal discount;

            success = decimal.TryParse(value.TrimEnd(new char[] { ' ', '%' }), out discount);
            discount /= 100m;

            if (success && discount >= 0 && discount <= 1)
            {
                detail.Discount = discount;

				using (var scope = new TransactionScope ()) {
	            	detail.UpdateAndFlush ();
				}
            }

            return Json(new { id = id, discount = detail.Discount.ToString("p"), total = detail.Total.ToString("c") });
        }

        //
        // POST: /Purchases/EditDetailWarehouse

        [HttpPost]
        public JsonResult EditDetailWarehouse(int id, int warehouse)
        {
            PurchaseOrderDetail detail = PurchaseOrderDetail.Find(id);

            detail.WarehouseId = warehouse;
            detail.Warehouse = Warehouse.Find(detail.WarehouseId);

			using (var scope = new TransactionScope ()) {
            	detail.UpdateAndFlush ();
			}

            return Json(new { id = id, warehouse = detail.Warehouse.Name });
        }

        //
        // GET: /Purchases/GetPurchaseItem/{id}

        public ActionResult GetPurchaseItem(int id)
        {
            return PartialView("_PurchaseItem", PurchaseOrderDetail.Find(id));
        }

        public ActionResult GetTotals(int id)
        {
            var order = PurchaseOrder.Find(id);
            return PartialView("_Totals", order);
        }

        //
        // POST: /Purchases/RemovePurchaseDetail/{id}

        [HttpPost]
        public JsonResult RemovePurchaseDetail(int id)
        {
            var item = PurchaseOrderDetail.Find(id);
            
			using (var scope = new TransactionScope()) {
				item.DeleteAndFlush ();
			}

            return Json(new { id = id, result = true });
        }

        //
        // POST: /Purchases/ConfirmPurchase/{id}

        [HttpPost]
        public ActionResult ConfirmPurchase(int id)
        {
            PurchaseOrder item = PurchaseOrder.Find (id);

            var qry = from x in item.Details
                      group x by x.Warehouse into g
                      select new { Warehouse = g.Key, Details = g.ToList() };

			var dt = DateTime.Now;
			var employee = SecurityHelpers.GetUser(User.Identity.Name).Employee;

			using (var scope = new TransactionScope()) {
	            foreach (var x in qry) {
	                var master = new InventoryReceipt {
	                    Order = item,
	                    Warehouse = x.Warehouse,
	                    CreationTime = dt,
	                    ModificationTime = dt,
	                    Creator = employee,
	                    Updater = employee    
	                };

                    master.Create ();

	                foreach (var y in x.Details) {
	                    var detail = new InventoryReceiptDetail {
	                        Receipt = master,
	                        Product = y.Product,
	                        QuantityOrdered = y.Quantity,
	                        Quantity = y.Quantity,
	                        ProductCode = y.ProductCode,
	                        ProductName = y.ProductName
						};

						detail.Create ();
                    }
                }

	            foreach (var x in item.Details) {
	                x.Product.Cost = x.Price;
					x.Product.Update ();
	            }

	            item.IsCompleted = true;
	            item.UpdateAndFlush ();
            }

            return RedirectToAction("Index");
        }

        //
        // POST: /Purchases/CancelPurchase/{id}

        [HttpPost]
        public ActionResult CancelPurchase(int id)
        {
            var item = PurchaseOrder.Find (id);
            
            item.IsCancelled = true;

			using (var scope = new TransactionScope ()) {
            	item.UpdateAndFlush ();
			}

            return RedirectToAction ("Index");
        }
        
    }
}
