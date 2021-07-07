using System;
using System.Linq;
using System.Web.Mvc;
using Mictlanix.BE.Model;
using Castle.ActiveRecord;
using Mictlanix.BE.Web.Helpers;
using Mictlanix.BE.Web.Models;

namespace Mictlanix.BE.Web.Controllers.Mvc
{
    public class AbastosCustomerRefundsController : CustomerRefundsController
    {
      [HttpPost]
	public override ActionResult Confirm (int id)
	{
		var dt = DateTime.Now;
		bool changed = false;
		var entity = CustomerRefund.Find (id);

		if (entity.IsCancelled || entity.IsCompleted) {
			return RedirectToAction ("Index");
		}

		using (var scope = new TransactionScope ()) {
			foreach (var item in entity.Details) {
				var qty = GetRefundableQuantity (item.SalesOrderDetail.Id);

				if (qty < item.Quantity) {
					changed = true;

					if (qty > 0.0m) {
						item.Quantity = qty;
						item.Update ();
					} else {
						item.Delete ();
					}
				}
			}

			if (changed) {
				entity.Updater = CurrentUser.Employee;
				entity.ModificationTime = dt;
				entity.UpdateAndFlush ();

				return RedirectToAction ("Edit", new { id = entity.Id, notify = true });
			}
		}

		using (var scope = new TransactionScope ()) {
				

			foreach (var detail in entity.Details.Where (x => !(x.Quantity > 0.0m)).ToList ()) {
				detail.DeleteAndFlush ();
			}
				
			foreach (var x in entity.Details) {
				AbastosInventoryHelpers.RefundProductToLot(x, CurrentUser.Employee);
			}

			entity.Updater = CurrentUser.Employee;
			entity.ModificationTime = dt;
			entity.Date = dt;
			entity.IsCompleted = true;
			entity.UpdateAndFlush ();
		}

		return RedirectToAction ("View", new { id = entity.Id });
	}

		protected override Search<CustomerRefund> SearchRefunds (Search<CustomerRefund> search)
		{
			IQueryable<CustomerRefund> query;
			var item = WebConfig.Store;

			if (string.IsNullOrEmpty (search.Pattern)) {
				query = from x in CustomerRefund.Queryable
					where x.Store.Id == item.Id
					orderby (x.IsCompleted || x.IsCancelled ? 1 : 0), x.Date descending, x.Id descending
					select x;
			} else {
				query = from x in CustomerRefund.Queryable
					where x.Store.Id == item.Id &&
						  x.Customer.Name.Contains (search.Pattern)
					orderby (x.IsCompleted || x.IsCancelled ? 1 : 0), x.Date descending, x.Id descending
					select x;
			}

			search.Total = query.Count ();
			search.Results = query.Skip (search.Offset).Take (search.Limit).ToList ();

			return search;
		}
	}
}