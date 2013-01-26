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

            Search<InventoryReceipt> search = new Search<InventoryReceipt>();
            search.Limit = Configuration.PageSize;
            search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
            search.Total = qry.Count();

            return View(search);
        }

        // POST: /Categories/

        [HttpPost]
        public ActionResult Receipts(Search<InventoryReceipt> search)
        {
            if (ModelState.IsValid)
            {
                search = GetInventoryReceipts(search);
            }

            if (Request.IsAjaxRequest())
            {
                return PartialView("_Receipts", search);
            }
            else
            {
                return View(search);
            }
        }

        Search<InventoryReceipt> GetInventoryReceipts(Search<InventoryReceipt> search)
        {
            if (search.Pattern == null)
            {
                var qry = from x in InventoryReceipt.Queryable
                          orderby x.Id descending
                          select x;

                search.Total = qry.Count();
                search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
            }
            else
            {
                var qry = from x in InventoryReceipt.Queryable
                          where x.Warehouse.Name.Contains(search.Pattern)
                          orderby x.Id descending
                          select x;

                search.Total = qry.Count();
                search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
            }

            return search;
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
            var item = InventoryReceipt.TryFind (id);

			if (item == null || item.IsCompleted || item.IsCancelled)
				return RedirectToAction("Receipts");

            item.IsCompleted = true;
			item.ModificationTime = DateTime.Now;

			using (var scope = new TransactionScope()) {
				item.UpdateAndFlush ();

				foreach(var x in item.Details) {
					var kardex = new Kardex {
						Warehouse = item.Warehouse,
						Product = x.Product,
						Source = TransactionType.InventoryReceipt,
						Quantity = x.Quantity,
						Date = item.ModificationTime,
						Reference = item.Id
					};

					kardex.CreateAndFlush ();
				}
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

            Search<InventoryIssue> search = new Search<InventoryIssue>();
            search.Limit = Configuration.PageSize;
            search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
            search.Total = qry.Count();

            return View(search);
        }

        // POST: /Issues/

        [HttpPost]
        public ActionResult Issues(Search<InventoryIssue> search)
        {
            if (ModelState.IsValid)
            {
                search = GetInventoryIssues(search);
            }

            if (Request.IsAjaxRequest())
            {
                return PartialView("_Issues", search);
            }
            else
            {
                return View(search);
            }
        }

        Search<InventoryIssue> GetInventoryIssues(Search<InventoryIssue> search)
        {
            if (search.Pattern == null)
            {
                var qry = from x in InventoryIssue.Queryable
                          orderby x.Id descending
                          select x;

                search.Total = qry.Count();
                search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
            }
            else
            {
                var qry = from x in InventoryIssue.Queryable
                          where x.Warehouse.Name.Contains(search.Pattern)
                          orderby x.Id descending
                          select x;

                search.Total = qry.Count();
                search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
            }

            return search;
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

        public ActionResult EditIssue (int id)
		{
			InventoryIssue item = InventoryIssue.Find (id);

			if (item.IsCompleted || item.IsCancelled) {
				return RedirectToAction ("Issue", new { id = item.Id });
			}

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_IssueEditor", item);
			} else {
				return View (item);
			}
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
            InventoryIssue item = InventoryIssue.TryFind (id);
			
			if (item == null || item.IsCompleted || item.IsCancelled)
				return RedirectToAction ("Issues");

            item.IsCompleted = true;
			item.ModificationTime = DateTime.Now;

			using (var scope = new TransactionScope()) {
				item.UpdateAndFlush ();

                foreach (var x in item.Details) {
                    var kardex = new Kardex {
                        Warehouse = item.Warehouse,
                        Product = x.Product,
                        Source = TransactionType.InventoryIssue,
                        Quantity = -x.Quantity,
						Date = item.ModificationTime,
                        Reference = item.Id
                    };

                    kardex.CreateAndFlush();
                }
			}

            return RedirectToAction ("Issues");
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
                      orderby x.Id descending
                      select x;

            Search<InventoryTransfer> search = new Search<InventoryTransfer>();
            search.Limit = Configuration.PageSize;
            search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
            search.Total = qry.Count();

            return View(search);
        }

        // POST: /InventoryTransfer/

        [HttpPost]
        public ActionResult Transfers(Search<InventoryTransfer> search)
        {
            if (ModelState.IsValid)
            {
                search = GetInventoryTransfers(search);
            }

            if (Request.IsAjaxRequest())
            {
                return PartialView("_Transfers", search);
            }
            else
            {
                return View(search);
            }
        }

        Search<InventoryTransfer> GetInventoryTransfers(Search<InventoryTransfer> search)
        {
            if (search.Pattern == null)
            {
                var qry = from x in InventoryTransfer.Queryable
                          orderby x.Id descending
                          select x;

                search.Total = qry.Count();
                search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
            }
            else
            {
                var qry = from x in InventoryTransfer.Queryable
                          where x.To.Name.Contains(search.Pattern) ||
                          x.From.Name.Contains(search.Pattern)
                          orderby x.Id descending
                          select x;

                search.Total = qry.Count();
                search.Results = qry.Skip(search.Offset).Take(search.Limit).ToList();
            }

            return search;
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

            var item = new InventoryTransferDetail {
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
            var item = InventoryTransfer.TryFind (id);
			
			if (item == null || item.IsCompleted || item.IsCancelled)
				return RedirectToAction("Transfers");
			
			item.IsCompleted = true;
			item.ModificationTime = DateTime.Now;

			using (var scope = new TransactionScope()) {
				item.UpdateAndFlush ();

                foreach (var x in item.Details) {
                    var input = new Kardex {
                        Warehouse = item.To,
                        Product = x.Product,
                        Source = TransactionType.InventoryTransfer,
                        Quantity = x.Quantity,
						Date = item.ModificationTime,
                        Reference = item.Id
                    };

                    input.Create();

                    var output = new Kardex {
                        Warehouse = item.From,
                        Product = x.Product,
                        Source = TransactionType.InventoryTransfer,
                        Quantity = -x.Quantity,
						Date = item.ModificationTime,
                        Reference = item.Id
                    };

                    output.Create();
                }
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
