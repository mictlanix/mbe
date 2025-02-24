using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Models {
	public static class MBEQueryable {

		public static IQueryable<Product> IQProducts { get { return Product.Queryable.Where (x => !x.IsDisabled).OrderByDescending(x => x.Id); } }
		public static IQueryable<Customer> IQCustomers { get { return Customer.Queryable.Where (x => !x.IsDisabled).OrderByDescending(x => x.Id); } }
		public static IQueryable<Employee> IQEmployees { get { return Employee.Queryable.Where (x => !x.IsDisabled); } }
		public static IQueryable<Store> IQStores { get { return Store.Queryable.Where(x => !x.IsDisabled); } }
		public static IQueryable<Warehouse> IQWarehouses { get { return Warehouse.Queryable.Where (x => !x.Store.IsDisabled); } }
		public static IQueryable<ProductionSite> IQProductionSites { get { return ProductionSite.Queryable.Where (x => !x.Store.IsDisabled); } }
		public static IQueryable<PointOfSale> IQPointsOfSales { get { return PointOfSale.Queryable.Where(x => !x.Warehouse.Store.IsDisabled && !x.IsDisabled ); } }
		public static IQueryable<CashDrawer> IQCashDrawers { get { return CashDrawer.Queryable.Where(x => !x.Store.IsDisabled); } }
		public static IQueryable<SalesOrder> IQSalesOrders { get { return SalesOrder.Queryable; } }
		public static IQueryable<SalesQuote> IQSalesQuotes { get { return SalesQuote.Queryable; } }
		public static IQueryable<Address> IQAddresses { get { return Address.Queryable.Where (x => !x.IsDisabled); } }
		public static IQueryable<User> IQUsers { get { return User.Queryable.Where (x => !x.IsDisabled); } }

	}
}