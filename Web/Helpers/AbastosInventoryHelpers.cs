using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Mictlanix.BE.Model;
using NHibernate;
using Castle.ActiveRecord;

namespace Mictlanix.BE.Web.Helpers {
	public static class AbastosInventoryHelpers {
		public static bool TakeProductFromLot (SalesOrderDetail detail, Employee employee) {

			if (detail.Lot == null) {
				return false;
			}

			var product_remaining = LotSerialTracking.Queryable.Where (x => x.LotNumber == detail.Lot.LotNumber && x.Product == detail.Product && x.Warehouse == detail.Warehouse).Sum (y => y.Quantity);
			if (product_remaining < detail.Quantity)
				return false;

			using (var scope = new TransactionScope ()) {
				var warehouse = detail.SalesOrder.PointOfSale.Warehouse;
				var dt = DateTime.Now;

				new LotSerialTracking {
					Source = TransactionType.SalesOrder,
					Date = DateTime.Now,
					Product = detail.Product,
					Quantity = -detail.Quantity,
					Warehouse = warehouse,
					Reference = detail.Id,
					LotNumber = detail.Lot.LotNumber,
					ExpirationDate = detail.Lot.ExpirationDate
				}.Save ();
			}
			return true;
		}

		public static bool RefundProductToLot (CustomerRefundDetail detail, Employee employee) {

			if (detail.SalesOrderDetail.Lot == null)
				return false;

			using (var scope = new TransactionScope ()) {
				var warehouse = detail.SalesOrderDetail.SalesOrder.PointOfSale.Warehouse;
				var dt = DateTime.Now;

				new LotSerialTracking {
					Source = TransactionType.CustomerRefund,
					Date = DateTime.Now,
					Product = detail.Product,
					Quantity = detail.Quantity,
					Warehouse = warehouse,
					Reference = detail.Id,
					LotNumber = detail.SalesOrderDetail.Lot.LotNumber
				}.Save ();
			}
			return true;
		}

		public static void EnterProductLot (PurchaseOrderDetail detail, Employee employee) {

			using (var scope = new TransactionScope ()) {
				var warehouse = detail.Warehouse;
				var dt = DateTime.Now;

				new LotSerialTracking {
					Source = TransactionType.PurchaseOrder,
					Date = DateTime.Now,
					Product = detail.Product,
					Quantity = detail.Quantity,
					Warehouse = warehouse,
					Reference = detail.Id,
					LotNumber = GetLotCode (detail.Order)
				}.Save ();
			}
		}

		public static bool CancelSalesOrder (SalesOrder order, Employee employee) {

			if (order.IsCancelled || order.IsPaid)
				return false;

			using (var scope = new TransactionScope ()) {
				var warehouse = order.PointOfSale.Warehouse;
				var dt = DateTime.Now;

				if (order.IsCompleted) {
					foreach (var detail in order.Details) {

						new LotSerialTracking {
							Source = TransactionType.CancelledSaleProduct,
							Date = DateTime.Now,
							Product = detail.Product,
							Quantity = detail.Quantity,
							Warehouse = warehouse,
							Reference = detail.Id,
							LotNumber = detail.Lot.LotNumber
						}.Save ();
					}
				}
				order.IsCancelled = true;
				order.Updater = employee;
				order.ModificationTime = DateTime.Now;
				order.UpdateAndFlush ();
			}

			return true;

		}

		public static bool CancelPurchase (PurchaseOrder order, Employee employee) {
			if (order.IsCancelled)
				return false;

			var lot_code = GetLotCode (order);
			foreach (var detail in order.Details) {
				var qty = AvailableQuantityProduct (new LotSerialTracking {
					LotNumber = lot_code, Product = detail.Product, Warehouse = detail.Warehouse
				});

				if (detail.Quantity - qty > 0) {
					return false;
				}
			}

			using (var scope = new TransactionScope ()) {
				var dt = DateTime.Now;

				foreach (var detail in order.Details) {

					new LotSerialTracking {
						Source = TransactionType.SupplierReturn,
						Date = DateTime.Now,
						Product = detail.Product,
						Quantity = -detail.Quantity,
						Warehouse = detail.Warehouse,
						Reference = detail.Id,
						LotNumber = lot_code
					}.Save ();
				}
				order.IsCancelled = true;
				order.Updater = employee;
				order.ModificationTime = DateTime.Now;
				order.UpdateAndFlush ();
			}

			return true;
		}

		public static decimal AvailableQuantityProduct (LotSerialTracking lot) {
			if (lot == null) {
				return 0;
			}
			return LotSerialTracking.Queryable.Where (x => x.LotNumber == lot.LotNumber && x.Product == lot.Product && x.Warehouse == lot.Warehouse).Sum (y => (decimal?)y.Quantity)??0;

		}

		public static int NormalizeLotNumbers () {
			int total = 0;
			using (var scope = new TransactionScope ()) {

				foreach (var purchase in PurchaseOrder.Queryable.ToList ()) {
					var previousCode = purchase.Supplier.Code + purchase.CreationTime.ToString ("ddMMyy") + purchase.Id;
					var newCode = GetLotCode (purchase);

					var lots = LotSerialTracking.Queryable.Where (x => x.LotNumber == previousCode);
					total += lots.Count();
					foreach (var lot in lots.ToList ()) {
						lot.LotNumber = newCode;
						lot.UpdateAndFlush ();
					}
				}

			}

			return total;
		}

		public static string GetLotCode (PurchaseOrder purchase) {

			string numbers = "123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
			string newCode = "";
			int index;

			var purchases = PurchaseOrder.Queryable.Where (x => x.CreationTime.Date == purchase.CreationTime.Date && x.Supplier == purchase.Supplier).ToList ();
			index = purchases.Count () > 0 ? purchases.FindIndex (x => x.Id == purchase.Id) : 0;
			var code = new string (purchase.Supplier.Code.Where (x => char.IsLetterOrDigit (x)).ToArray ());
			code = code + "XXX";
			newCode = code.Substring (0, 4) + purchase.CreationTime.ToString ("yyMMdd") + numbers [index];

			return newCode;
		}
	}
}