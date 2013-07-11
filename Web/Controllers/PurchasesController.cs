// 
// PurchasesController.cs
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
        public ActionResult Index()
        {
            var qry = from x in PurchaseOrder.Queryable
                      orderby x.Id descending
                      select x;

            Search<PurchaseOrder> search = new Search<PurchaseOrder>();
            search.Limit = Configuration.PageSize;
            search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
            search.Total = qry.Count();

            return View (search);
        }

        [HttpPost]
        public ActionResult Index(Search<PurchaseOrder> search)
        {
            if (ModelState.IsValid) {
                search = GetPurchaseOrders(search);
            }

            if (Request.IsAjaxRequest()) {
                return PartialView("_Index", search);
            }
            else {
                return View (search);
            }
        }

        Search<PurchaseOrder> GetPurchaseOrders(Search<PurchaseOrder> search)
        {
            if (search.Pattern == null)
            {
                var qry = from x in PurchaseOrder.Queryable
                          orderby x.Id descending
                          select x;

                search.Total = qry.Count();
                search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
            }
            else
            {
                var qry = from x in PurchaseOrder.Queryable
                          where x.Supplier.Name.Contains(search.Pattern)
                          orderby x.Id descending
                          select x;

                search.Total = qry.Count();
                search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
            }

            return search;
        }

        public ViewResult Print (int id)
        {
            var item = PurchaseOrder.Find (id);
            return View (item);
        }

        public ActionResult Details(int id)
        {
            var item = PurchaseOrder.Find(id);

            return View (item);
        }

        public ActionResult New ()
        {
            return View (new PurchaseOrder());
        } 

        [HttpPost]
        public ActionResult New (PurchaseOrder item)
        {
            item.Supplier = Supplier.Find(item.SupplierId);
            item.Creator = SecurityHelpers.GetUser(User.Identity.Name).Employee;
            item.Updater = item.Creator;
            item.CreationTime = DateTime.Now;
            item.ModificationTime = item.CreationTime;

            using (var scope = new TransactionScope()) {
                item.CreateAndFlush();
            }

            return RedirectToAction("Edit", new { id = item.Id });
        }

        public ActionResult Edit (int id)
        {
            var item = PurchaseOrder.Find (id);

            if (item.IsCompleted || item.IsCancelled) {
                return RedirectToAction ("Details", new { id = item.Id });
            }

            if (Request.IsAjaxRequest())
                return PartialView ("_PurchaseEditor", item);
            else
                return View (item);
        }

        [HttpPost]
        public ActionResult Edit (PurchaseOrder item)
        {
            var order = PurchaseOrder.Find(item.Id);

            order.Supplier = Supplier.Find(item.SupplierId);
            order.Updater = SecurityHelpers.GetUser(User.Identity.Name).Employee;
            order.ModificationTime = DateTime.Now;
            order.Comment = item.Comment;

			using (var scope = new TransactionScope ()) {
            	order.UpdateAndFlush ();
			}

            return PartialView ("_PurchaseInfo", order);
        }

        [HttpPost]
        public JsonResult AddPurchaseDetail (int movement, int warehouse, int product)
        {
			var p = Product.Find (product);
			var cost = (from x in ProductPrice.Queryable
			            where x.Product.Id == product && x.List.Id == 0
			            select x.Value).SingleOrDefault();

            var item = new PurchaseOrderDetail
            {
                Order = PurchaseOrder.Find(movement),
                Warehouse = Warehouse.Find(warehouse),
                Product = p,
                ProductCode = p.Code,
                ProductName = p.Name,
                Quantity = 1,
                TaxRate = p.TaxRate,
				IsTaxIncluded = p.IsTaxIncluded,
                Discount = 0,
				Price = cost,
				ExchangeRate = CashHelpers.GetTodayDefaultExchangeRate(),
				Currency = Configuration.DefaultCurrency
            };

            using (var scope = new TransactionScope()) {
                item.CreateAndFlush();
            }

            return Json(new { id = item.Id });
        }

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

        [HttpPost]
        public JsonResult EditPurchaseDetailPrice(int id, string value)
        {
            PurchaseOrderDetail detail = PurchaseOrderDetail.Find(id);
            bool success;
            decimal cost;

            success = decimal.TryParse(value,System.Globalization.NumberStyles.AllowCurrencySymbol, null, out cost);

            if (success && cost >= 0)
            {
                detail.Price = cost;

				using (var scope = new TransactionScope ()) {
	            	detail.UpdateAndFlush ();
				}
            }

            return Json(new { id = id, price = detail.Price.ToString("c"), total = detail.Total.ToString("c") });
        }

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

        public ActionResult GetPurchaseItem(int id)
        {
            return PartialView("_PurchaseItem", PurchaseOrderDetail.Find(id));
        }

        public ActionResult GetTotals(int id)
        {
            var order = PurchaseOrder.Find(id);
            return PartialView("_Totals", order);
        }

        [HttpPost]
        public JsonResult RemovePurchaseDetail(int id)
        {
            var item = PurchaseOrderDetail.Find(id);
            
			using (var scope = new TransactionScope()) {
				item.DeleteAndFlush ();
			}

            return Json(new { id = id, result = true });
        }

		// TODO: Remove inventory stuff
        [HttpPost]
        public ActionResult ConfirmPurchase (int id)
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
					var price = x.Product.Prices.SingleOrDefault (t => t.List.Id == 0);

					if (price == null) {
						price = new ProductPrice {
							List = PriceList.Find (0),
							Product = x.Product
						};
					}

					price.Value = x.Price;
					price.Currency = x.Currency;
					price.Save ();
	            }

				item.IsCompleted = true;
				item.ModificationTime = DateTime.Now;
	            item.UpdateAndFlush ();
            }

            return RedirectToAction("Index");
        }

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
