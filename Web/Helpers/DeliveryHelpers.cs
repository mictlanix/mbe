// 
// InventoryHelpers.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2013 Eddy Zavaleta, Mictlanix, and contributors.
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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;

namespace Mictlanix.BE.Web.Helpers {
	public static class DeliveryHelpers {

		public static List<DeliveryDetail> GetDeliveryDetailsByOrderDetail (SalesOrderDetail detail) {

			var details = new List<DeliveryDetail> ();

			details.AddRange (DeliveryOrderDetail.Queryable.Where (x => x.OrderDetail == detail
			&& !x.DeliveryOrder.IsCancelled && x.DeliveryOrder.IsCompleted)
				.Select(y => new DeliveryDetail {SalesOrderDetail = detail, DeliveryDate = y.DeliveryOrder.Date, Quantity = y.Quantity }));

			return details;
		}
		public static decimal DeliveredQuantity (SalesOrderDetail detail)
		{
			return GetDeliveryDetailsByOrderDetail (detail).Sum (x => (decimal?) x.Quantity ?? 0);
		}

		public static bool IsAddressStore (Address address) {
			var address_ids = MBEQueryable.IQStores.Select (x => x.Address.Id).ToList();
			return address_ids.Contains (address.Id);
		}

		public static decimal GetRemainingQuantity (SalesOrderDetail detail) {
			return detail.Quantity - DeliveredQuantity(detail);
		}

		public static Address GetCurrentStoreAddress (User user) {

			return Mictlanix.BE.Model.User.Queryable
				.Where (x => x.Employee.Id == user.EmployeeId).First ().UserSettings.PointOfSale.Store.Address;

		}

		public static DeliveryOrder[] GetDeliveryOrders (this SalesOrder salesOrder) {
			return DeliveryOrderDetail.Queryable.Where (x => salesOrder.Details
			.Contains (x.OrderDetail)).Select (y => y.DeliveryOrder).Distinct ().ToArray ();
		}
	}
}
