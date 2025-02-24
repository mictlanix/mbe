using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;

namespace Mictlanix.BE.Web.Services {
	public class SalesOrderService {

		public SalesOrderService () {
		}

		public Result<IEnumerable<SalesOrder>> GetSalesOrders (Employee employee, Store store) {

			return SalesOrder.Queryable.Where(x => x.Creator == employee).ToList();
		}
	}
}