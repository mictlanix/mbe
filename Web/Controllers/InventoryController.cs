// 
// InventoryController.cs
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
    public class InventoryController : Controller
    {
        #region Receipts

        //
        // GET: /Inventory/Receipts

        public ActionResult Receipts()
        {
            var qry = from x in InventoryReceipt.Queryable
                      orderby x.Id descending
                      select x;

            return View(qry.ToList());
        }

        // GET: /Inventory/PrintReceipt/

        public ViewResult PrintReceipt(int id)
        {
            InventoryReceipt item = InventoryReceipt.Find(id);

            return View("_PrintReceipt",item);
        }
        //
        // GET: /Inventory/Receipt/{id}

        public ViewResult Receipt(int id)
        {
            InventoryReceipt item = InventoryReceipt.Find(id);

            return View(item);
        }

        //
        // GET: /Inventory/NewReceipt

        public ViewResult NewReceipt()
        {
            return View(new InventoryReceipt());
        }


        //
        // POST: /Inventory/NewReceipt

        [HttpPost]
		public ActionResult NewReceipt (InventoryReceipt item)
		{
			item.CreationTime = DateTime.Now;
			item.ModificationTime = item.CreationTime;
			item.Creator = SecurityHelpers.GetUser (User.Identity.Name).Employee;
			item.Updater = item.Creator;
			item.Warehouse = Warehouse.Find (item.WarehouseId);


			using (var scope = new TransactionScope()) {
				item.CreateAndFlush ();
			}

			System.Diagnostics.Debug.WriteLine ("New InventoryReceipt [Id = {0}]", item.Id);

            return RedirectToAction("EditReceipt", new { id = item.Id });
        }

        //
        // GET: /Inventory/EditReceipt/5

        public ActionResult EditReceipt(int id)
        {
            InventoryReceipt item = InventoryReceipt.Find(id);

            if (item.IsCompleted || item.IsCancelled)
            {
                return RedirectToAction("Receipt", new { id = item.Id });
            }

            if (Request.IsAjaxRequest())
                return PartialView("_ReceiptEditor", item);
            else
                return View(item);
        }

        //
        // POST: /Inventory/EditReceipt

        [HttpPost]
        public ActionResult EditReceipt(InventoryReceipt item)
        {
            var movement = InventoryReceipt.Find (item.Id);

            movement.Warehouse = Warehouse.Find(item.WarehouseId);
            movement.Updater = SecurityHelpers.GetUser(User.Identity.Name).Employee;
            movement.ModificationTime = DateTime.Now;
            movement.Comment = item.Comment;

			using (var scope = new TransactionScope()) {
            	movement.UpdateAndFlush ();
			}

            return PartialView("_ReceiptInfo", movement);
        }

        //
        // POST: /Inventory/AddReceiptDetail

        [HttpPost]
        public JsonResult AddReceiptDetail(int movement, int product)
        {
            var p = Product.Find(product);

            var item = new InventoryReceiptDetail
            {
                Receipt = InventoryReceipt.Find(movement),
                Product = p,
                ProductCode = p.Code,
                ProductName = p.Name,
                Quantity = 1,
                QuantityOrdered = 0
            };

            using (var scope = new TransactionScope()) {
                item.CreateAndFlush();
            }

            System.Diagnostics.Debug.WriteLine("New InventoryReceiptDetail [Id = {0}]", item.Id);

            return Json(new { id = item.Id });
        }

        //
        // POST: /Inventory/EditReceiptDetailQty

        [HttpPost]
        public JsonResult EditReceiptDetailQty(int id, decimal quantity)
        {
            InventoryReceiptDetail detail = InventoryReceiptDetail.Find(id);

            if (quantity > 0) {
                detail.Quantity = quantity;

				using (var scope = new TransactionScope()) {
	            	detail.UpdateAndFlush ();
				}
            }

            return Json(new { id = id, quantity = detail.Quantity });
        }

        //
        // GET: /Inventory/GetReceiptItem/{id}

        public ActionResult GetReceiptItem(int id)
        {
            return PartialView("_ReceiptItem", InventoryReceiptDetail.Find(id));
        }

        //
        // POST: /Inventory/RemoveReceiptDetail/{id}

        [HttpPost]
        public JsonResult RemoveReceiptDetail(int id)
        {
            var item = InventoryReceiptDetail.Find(id);
            
			using (var scope = new TransactionScope()) {
				item.DeleteAndFlush ();
			}

            return Json(new { id = id, result = true });
        }

        //
        // POST: /Inventory/ConfirmReceipt/{id}

        [HttpPost]
        public ActionResult ConfirmReceipt (int id)
        {
            InventoryReceipt item = InventoryReceipt.Find (id);

            item.IsCompleted = true;

			using (var scope = new TransactionScope()) {

				foreach(var x in item.Details) {
					var kardex = new Kardex {
						Warehouse = item.Warehouse,
						Product = x.Product,
						Source = KardexSource.InventoryReceipt,
						Quantity = x.Quantity,
						Date = DateTime.Now,
						Reference = item.Id
					};

					kardex.CreateAndFlush ();
				}

            	item.UpdateAndFlush ();
			}

            return RedirectToAction("Receipts");
        }

        //
        // POST: /Inventory/CancelReceipt/{id}

        [HttpPost]
        public ActionResult CancelReceipt (int id)
        {
            InventoryReceipt item = InventoryReceipt.Find(id);

            item.IsCancelled = true;

			using (var scope = new TransactionScope()) {
            	item.UpdateAndFlush ();
			}

            return RedirectToAction("Receipts");
        }
        
        #endregion

        #region Issues

        //
        // GET: /Inventory/Issues

        public ActionResult Issues()
        {
            var qry = from x in InventoryIssue.Queryable
                      orderby x.Id descending
                      select x;

            return View(qry.ToList());
        }

        // GET: /Inventory/PrintIssue/

        public ViewResult PrintIssue(int id)
        {
            InventoryIssue item = InventoryIssue.Find(id);

            return View("_PrintIssue", item);
        }

        //
        // GET: /Inventory/Issue/{id}

        public ViewResult Issue(int id)
        {
            InventoryIssue item = InventoryIssue.Find(id);

            return View(item);
        }

        //
        // GET: /Inventory/NewIssue

        public ViewResult NewIssue()
        {
            return View(new InventoryIssue());
        }


        //
        // POST: /Inventory/NewIssue

        [HttpPost]
        public ActionResult NewIssue(InventoryIssue item)
        {
            item.CreationTime = DateTime.Now;
            item.ModificationTime = item.CreationTime;
            item.Creator = SecurityHelpers.GetUser(User.Identity.Name).Employee;
            item.Updater = item.Creator;
            item.Warehouse = Warehouse.Find(item.WarehouseId);

            using (var scope = new TransactionScope()) {
                item.CreateAndFlush();
            }

            System.Diagnostics.Debug.WriteLine("New InventoryIssue [Id = {0}]", item.Id);

            return RedirectToAction("EditIssue", new { id = item.Id });
        }

        //
        // GET: /Inventory/EditIssue/5

        public ActionResult EditIssue(int id)
        {
            InventoryIssue item = InventoryIssue.Find(id);

            if (item.IsCompleted || item.IsCancelled)
            {
                return RedirectToAction("Issue", new { id = item.Id });
            }

            if (Request.IsAjaxRequest())
                return PartialView("_IssueEditor", item);
            else
                return View(item);
        }

        //
        // POST: /Inventory/EditIssue

        [HttpPost]
        public ActionResult EditIssue(InventoryIssue item)
        {
            var movement = InventoryIssue.Find(item.Id);
            var warehouse = Warehouse.Find(item.WarehouseId);

            movement.Warehouse = warehouse;
            movement.Updater = SecurityHelpers.GetUser(User.Identity.Name).Employee;
            movement.ModificationTime = DateTime.Now;
            movement.Comment = item.Comment;

			using (var scope = new TransactionScope()) {
            	movement.UpdateAndFlush ();
			}

            return PartialView("_IssueInfo", movement);
        }

        //
        // POST: /Inventory/AddIssueDetail

        [HttpPost]
        public JsonResult AddIssueDetail(int movement, int product)
        {
            var p = Product.Find(product);

            var item = new InventoryIssueDetail {
                Issue = InventoryIssue.Find(movement),
                Product = p,
                ProductCode = p.Code,
                ProductName = p.Name,
                Quantity = 1,
            };

            using (var scope = new TransactionScope()) {
                item.CreateAndFlush();
            }

            System.Diagnostics.Debug.WriteLine("New InventoryIssueDetail [Id = {0}]", item.Id);

            return Json(new { id = item.Id });
        }

        //
        // POST: /Inventory/EditIssueDetailQty

        [HttpPost]
        public JsonResult EditIssueDetailQty(int id, decimal quantity)
        {
            InventoryIssueDetail detail = InventoryIssueDetail.Find(id);

            if (quantity > 0) {
                detail.Quantity = quantity;

				using (var scope = new TransactionScope()) {
	            	detail.UpdateAndFlush ();
				}
            }

            return Json(new { id = id, quantity = detail.Quantity });
        }

        //
        // GET: /Inventory/GetIssueItem/{id}

        public ActionResult GetIssueItem(int id)
        {
            return PartialView("_IssueItem", InventoryIssueDetail.Find(id));
        }

        //
        // POST: /Inventory/RemoveIssueDetail/{id}

        [HttpPost]
        public JsonResult RemoveIssueDetail(int id)
        {
            var item = InventoryIssueDetail.Find(id);

			using (var scope = new TransactionScope()) {
            	item.DeleteAndFlush ();
			}

            return Json(new { id = id, result = true });
        }

        //
        // POST: /Inventory/ConfirmIssue/{id}

        [HttpPost]
        public ActionResult ConfirmIssue(int id)
        {
            InventoryIssue item = InventoryIssue.Find(id);

            item.IsCompleted = true;

			using (var scope = new TransactionScope()) {
            	item.UpdateAndFlush ();
			}

            return RedirectToAction("Issues");
        }

        //
        // POST: /Inventory/CancelIssue/{id}

        [HttpPost]
        public ActionResult CancelIssue (int id)
        {
            InventoryIssue item = InventoryIssue.Find(id);

            item.IsCancelled = true;

			using (var scope = new TransactionScope()) {
            	item.UpdateAndFlush ();
			}

            return RedirectToAction("Issues");
        }

        #endregion

        #region Transfers

        //
        // GET: /Inventory/Transfers

        public ActionResult Transfers()
        {
            var qry = from x in InventoryTransfer.Queryable
                      select x;

            return View(qry.ToList());
        }

        //
        // GET: /Inventory/Transfer/{id}

        public ViewResult Transfer(int id)
        {
            InventoryTransfer item = InventoryTransfer.Find(id);

            return View(item);
        }

        // GET: /Inventory/PrintTransfer/

        public ViewResult PrintTransfer(int id)
        {
            InventoryTransfer item = InventoryTransfer.Find(id);

            return View("_PrintTransfer", item);
        }

        //
        // GET: /Inventory/NewTransfer

        public ViewResult NewTransfer()
        {
            return View(new InventoryTransfer());
        }


        //
        // POST: /Inventory/NewTransfer

        [HttpPost]
        public ActionResult NewTransfer(InventoryTransfer item)
        {
            item.From = Warehouse.TryFind(item.FromId);
            item.To = Warehouse.TryFind(item.ToId);

            if (!ModelState.IsValid)
            {
                return View(item);
            }
			
            item.CreationTime = DateTime.Now;
            item.ModificationTime = item.CreationTime;
            item.Creator = SecurityHelpers.GetUser(User.Identity.Name).Employee;
            item.Updater = item.Creator;

            using (var scope = new TransactionScope()) {
                item.CreateAndFlush();
            }

            System.Diagnostics.Debug.WriteLine("New InventoryTransfer [Id = {0}]", item.Id);

            return RedirectToAction("EditTransfer", new { id = item.Id });
        }

        //
        // GET: /Inventory/EditTransfer/5

        public ActionResult EditTransfer(int id)
        {
            InventoryTransfer item = InventoryTransfer.Find(id);

            if (item.IsCompleted || item.IsCancelled)
            {
                return RedirectToAction("Transfer", new { id = item.Id });
            }

            if (Request.IsAjaxRequest())
                return PartialView("_TransferEditor", item);
            else
                return View(item);
        }

        //
        // POST: /Inventory/EditTransfer

        [HttpPost]
        public ActionResult EditTransfer(InventoryTransfer item)
        {
            item.From = Warehouse.TryFind(item.FromId);
            item.To = Warehouse.TryFind(item.ToId);

            if (!ModelState.IsValid)
            {
                if (Request.IsAjaxRequest())
                    return PartialView("_TransferEditor", item);
                else
                    return View(item);
            }

            var movement = InventoryTransfer.Find(item.Id);

            movement.From = item.From;
            movement.To = item.To;
            movement.Updater = SecurityHelpers.GetUser(User.Identity.Name).Employee;
            movement.ModificationTime = DateTime.Now;
            movement.Comment = item.Comment;

			using (var scope = new TransactionScope()) {
            	movement.UpdateAndFlush ();
			}

            return PartialView("_TransferInfo", movement);
        }

        //
        // POST: /Inventory/AddTransferDetail

        [HttpPost]
        public JsonResult AddTransferDetail(int movement, int product)
        {
            var p = Product.Find(product);

            var item = new InventoryTransferDetail
            {
                Transfer = InventoryTransfer.Find(movement),
                Product = p,
                ProductCode = p.Code,
                ProductName = p.Name,
                Quantity = 1,
            };

            using (var scope = new TransactionScope()) {
                item.CreateAndFlush ();
            }

            System.Diagnostics.Debug.WriteLine("New InventoryTransferDetail [Id = {0}]", item.Id);

            return Json(new { id = item.Id });
        }

        //
        // POST: /Inventory/EditTransferDetailQty

        [HttpPost]
        public JsonResult EditTransferDetailQty(int id, decimal quantity)
        {
            InventoryTransferDetail detail = InventoryTransferDetail.Find(id);

            if (quantity > 0)
            {
                detail.Quantity = quantity;

				using (var scope = new TransactionScope()) {
	            	detail.UpdateAndFlush ();
				}
            }

            return Json(new { id = id, quantity = detail.Quantity });
        }

        //
        // GET: /Inventory/GetTransferItem/{id}

        public ActionResult GetTransferItem(int id)
        {
            return PartialView("_TransferItem", InventoryTransferDetail.Find(id));
        }

        //
        // POST: /Inventory/RemoveTransferDetail/{id}

        [HttpPost]
        public JsonResult RemoveTransferDetail(int id)
        {
            var item = InventoryTransferDetail.Find(id);

			using (var scope = new TransactionScope()) {
            	item.DeleteAndFlush ();
			}

            return Json(new { id = id, result = true });
        }

        //
        // POST: /Inventory/ConfirmTransfer/{id}

        [HttpPost]
        public ActionResult ConfirmTransfer(int id)
        {
            InventoryTransfer item = InventoryTransfer.Find(id);

            item.IsCompleted = true;

			using (var scope = new TransactionScope()) {
            	item.UpdateAndFlush ();
			}

            return RedirectToAction("Transfers");
        }

        //
        // POST: /Inventory/CancelTransfer/{id}

        [HttpPost]
        public ActionResult CancelTransfer (int id)
        {
            InventoryTransfer item = InventoryTransfer.Find(id);

            item.IsCancelled = true;

			using (var scope = new TransactionScope()) {
            	item.UpdateAndFlush ();
			}

            return RedirectToAction("Transfers");
        }

        #endregion
    }
}
