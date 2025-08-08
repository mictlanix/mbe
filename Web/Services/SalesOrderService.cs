using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Transactions;
using System.Web;
using Microsoft.Ajax.Utilities;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;

namespace Mictlanix.BE.Web.Services {
	public static class SalesOrderService {


		public static Result<IEnumerable<SalesOrder>> GetSalesOrders (Employee employee, Search<SalesOrder> search)
		{
			if (employee == null) {
				return Result.Failure<IEnumerable<SalesOrder>> (string.Format (Resources.ItemMissing, Resources.Employee));
			}

			return SalesOrder.Queryable.Where (x => x.Creator == employee).ToList ();
		}

		public static Result<SalesOrder> Cancel (this SalesOrder salesOrder)
		{

			if (salesOrder.IsCancelled || salesOrder.IsCompleted) {
				return Result.Failure<SalesOrder>(Resources.ItemAlreadyCompletedOrCancelled);
			}

			using (var scope = new TransactionScope ()) {
				salesOrder.IsCancelled = true;
				salesOrder.UpdateAndFlush ();
			}

				return (salesOrder);
		}

		public static Result<SalesOrder> Complete (this SalesOrder salesOrder)
		{
			if (salesOrder.IsCompleted || salesOrder.IsCancelled) {
				return Result.Failure<SalesOrder> (Resources.ItemAlreadyCompletedOrCancelled);
			}

			using (var scope = new TransactionScope ()) {
				salesOrder.IsCompleted = true;
				salesOrder.UpdateAndFlush ();
			}

			return salesOrder;
		}

		public static Result<SalesOrder> Update (this SalesOrder salesOrder) {
			if (salesOrder.IsCompleted || salesOrder.IsCancelled) {
				return Result.Failure<SalesOrder> (Resources.ItemAlreadyCompletedOrCancelled);
			}

			using (var scope = new TransactionScope ()) {
				salesOrder.UpdateAndFlush ();
			}

			return salesOrder;
		}

		public static Result<SalesOrder> View ()
		{
			throw new NotImplementedException ();
		}

		public static Result<SalesOrder> Edit ()
		{
			throw new NotImplementedException ();
		}

		public static Result<SalesOrder> Print ()
		{
			throw new NotImplementedException ();
		}
	}
}