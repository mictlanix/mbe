// 
// KardexController.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Castle.ActiveRecord;
using NHibernate;
using NHibernate.Exceptions;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Helpers;

namespace Mictlanix.BE.Web.Controllers
{
	[Authorize]
    public class ReportsController : Controller
    {
		#region Kardex
		
		public ViewResult Kardex ()
		{
			return View ();
		}

        [HttpPost]
        public ActionResult Kardex(Warehouse item)
        {
			var warehouse = Warehouse.Find (item.Id);
            var qry = from x in Model.Kardex.Queryable
                      where x.Warehouse.Id == item.Id
					  group x by x.Product into g
					  select new Kardex {
						Product = g.Key,
						Quantity = g.Sum(y => y.Quantity)
					  };

			return PartialView("_Kardex", new MasterDetails<Warehouse, Kardex> { 
				Master = warehouse,
				Details = qry.ToList().OrderBy(x => x.Product.Name).ToList()
			});
        }

		public ViewResult KardexDetails(int warehouse, int product)
        {
            var item = new DateRange {
				StartDate = DateTime.Now,
				EndDate = DateTime.Now
			};

            ViewBag.Warehouse = Warehouse.Find(warehouse);
            ViewBag.Product = Product.Find(product);

            return View(item);
        }

        [HttpPost]
		public ActionResult KardexDetails(int warehouse, int product, DateRange item)
        {
			var balance = from x in Model.Kardex.Queryable
						  where x.Warehouse.Id == warehouse && x.Product.Id == product &&
								x.Date < item.StartDate.Date
						  select x.Quantity;
            var qry = from x in Model.Kardex.Queryable
                      where x.Warehouse.Id == warehouse && x.Product.Id == product &&
                            x.Date >= item.StartDate.Date && x.Date <= item.EndDate.Date.Add(new TimeSpan(23, 59, 59))
					  orderby x.Date
                      select x;

			ViewBag.OpeningBalance = balance.Count() > 0 ? balance.Sum () : 0m;

			return PartialView("_KardexDetails", qry.ToList());
        }

		#endregion

		#region Serial Numbers
		
		public ViewResult SerialNumbers ()
		{
			return View ();
		}

		[HttpPost]
		public ActionResult SerialNumbers (int warehouse, int product, DateRange item)
		{
			var balance = from x in LotSerialTracking.Queryable
						  where x.Warehouse.Id == warehouse && x.Product.Id == product &&
								x.Date < item.StartDate.Date
						  select x.Quantity;

			var qry = from x in LotSerialTracking.Queryable
					  where x.Warehouse.Id == warehouse && x.Product.Id == product &&
							x.Date >= item.StartDate.Date && x.Date <= item.EndDate.Date.Add(new TimeSpan(23, 59, 59))
					  orderby x.Date
					  select x;
			
			ViewBag.OpeningBalance = balance.Count() > 0 ? balance.Sum () : 0m;
			
			return PartialView ("_SerialNumbers", qry.ToList());
		}
		
		#endregion

		#region Payments

		public ViewResult ReceivedPayments ()
		{
			return View (new DateRange(DateTime.Now, DateTime.Now));
		}

		[HttpPost]
		public ActionResult ReceivedPayments (int store, DateRange dates)
		{
			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			var qry = from x in CustomerPayment.Queryable
					  where x.Store.Id == store &&
							x.Date >= start && x.Date <= end &&
							x.Amount > 0
					  select new ReceivedPayment {
						Date = x.Date,
						SalesOrder = x.SalesOrder.Id,
						Serial = x.SalesOrder.Serial,
						Customer = x.Customer,
						Method = x.Method,
						Amount = x.Amount };



			return PartialView("_ReceivedPayments", qry.ToList());
		}

		#endregion

		#region Sales
				


		#endregion

		#region Gross Profits

		public ViewResult GrossProfitsByCustomer ()
		{
			ViewBag.EditorField = "store";
			ViewBag.EditorTemplate = "StoreSelector";
			ViewBag.Title = Resources.GrossProfitsByCustomer;
			return View ("SummaryReport", new DateRange(DateTime.Now, DateTime.Now));
		}
		
		[HttpPost]
		public ActionResult GrossProfitsByCustomer (int store, DateRange dates)
		{
			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			var qry = from x in SalesOrder.Queryable
					from y in x.Details
					where x.Store.Id == store &&
						x.IsCompleted &&
						x.IsPaid &&
						!x.IsCancelled &&
						x.Date >= start &&
						x.Date <= end
					select new {
						Id = x.Customer.Id,
						Name = x.Customer.Name,
						Units = y.Quantity,
						Total = y.Quantity * (y.Price - y.Cost),
						Subtotal = y.Quantity * (y.Price - y.Cost) / (y.TaxRate + 1m)
					};
			var qry2 = from x in qry.ToList()
						group x by new { x.Id, x.Name } into g
						select new SummaryItem {
							Id = g.Key.Id.ToString (),
							Name = g.Key.Name,
							Units = g.Sum(x => x.Units),
							Total = g.Sum(x => x.Total),
							Subtotal = g.Sum(x => x.Subtotal)
						};
			var items = qry2.OrderByDescending (x => x.Total).ToList();
			
			AnalyzeABC (items);
			
			return PartialView("_SummaryReport", items);
		}
		
		public ViewResult GrossProfitsBySalesPerson ()
		{
			ViewBag.EditorField = "store";
			ViewBag.EditorTemplate = "StoreSelector";
			ViewBag.Title = Resources.GrossProfitsBySalesPerson;
			return View ("SummaryReport", new DateRange(DateTime.Now, DateTime.Now));
		}
		
		[HttpPost]
		public ActionResult GrossProfitsBySalesPerson (int store, DateRange dates)
		{
			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			var qry = from x in SalesOrder.Queryable
					from y in x.Details
					where x.Store.Id == store &&
						x.IsCompleted &&
						x.IsPaid &&
						!x.IsCancelled &&
						x.Date >= start &&
						x.Date <= end
					select new {
						Id = x.SalesPerson.Id,
						Name = x.SalesPerson.FirstName + " " + x.SalesPerson.LastName,
						Units = y.Quantity,
						Total = y.Quantity * (y.Price - y.Cost),
						Subtotal = y.Quantity * (y.Price - y.Cost) / (y.TaxRate + 1m)
					};
			var qry2 = from x in qry.ToList()
						group x by new { x.Id, x.Name } into g
						select new SummaryItem {
							Id = g.Key.Id.ToString (),
							Name = g.Key.Name,
							Units = g.Sum(x => x.Units),
							Total = g.Sum(x => x.Total),
							Subtotal = g.Sum(x => x.Subtotal)
						};
			var items = qry2.OrderByDescending (x => x.Total).ToList();
			
			AnalyzeABC (items);
			
			return PartialView("_SummaryReport", items);
		}

		public ViewResult GrossProfitsByProduct ()
		{
			ViewBag.EditorField = "store";
			ViewBag.EditorTemplate = "StoreSelector";
			ViewBag.Title = Resources.GrossProfitsByProduct;
			return View ("SummaryReport", new DateRange(DateTime.Now, DateTime.Now));
		}
		
		[HttpPost]
		public ActionResult GrossProfitsByProduct (int store, DateRange dates)
		{
			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			var qry = from x in SalesOrder.Queryable
						from y in x.Details
							where x.Store.Id == store &&
							x.IsCompleted &&
							x.IsPaid &&
							!x.IsCancelled &&
							x.Date >= start &&
							x.Date <= end
						select new {
							Id = y.ProductCode,
							Name = y.ProductName,
							Units = y.Quantity,
							Total = y.Quantity * (y.Price - y.Cost),
							Subtotal = y.Quantity * (y.Price - y.Cost) / (y.TaxRate + 1m)
						};
			var qry2 = from x in qry.ToList()
						group x by new { x.Id, x.Name } into g
						select new SummaryItem {
							Id = g.Key.Id,
							Name = g.Key.Name,
							Units = g.Sum(x => x.Units),
							Total = g.Sum(x => x.Total),
							Subtotal = g.Sum(x => x.Subtotal),
						};
			var items = qry2.OrderByDescending (x => x.Total).ToList();
			
			AnalyzeABC (items);
			
			return PartialView("_SummaryReport", items);
		}

		#endregion

		#region Best Selling Products

		public ViewResult BestSellingProductsByCustomer ()
		{
			ViewBag.EditorField = "customer";
			ViewBag.EditorTemplate = "CustomerSelector";
			ViewBag.Title = Resources.BestSellingProductsByCustomer;
			return View ("SummaryReport", new DateRange(DateTime.Now, DateTime.Now));
		}
		
		[HttpPost]
		public ActionResult BestSellingProductsByCustomer (int customer, DateRange dates)
		{
			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			var qry = from x in SalesOrder.Queryable
						from y in x.Details
						where x.Customer.Id == customer &&
							x.IsCompleted &&
							x.IsPaid &&
							!x.IsCancelled &&
							x.Date >= start &&
							x.Date <= end
						select new {
							Id = y.ProductCode,
							Name = y.ProductName,
							Units = y.Quantity,
							Total = y.Quantity * y.Price,
							Subtotal = y.Quantity * y.Price / (y.TaxRate + 1m)
						};
			var qry2 = from x in qry.ToList()
						group x by new { x.Id, x.Name } into g
						select new SummaryItem {
							Id = g.Key.Id,
							Name = g.Key.Name,
							Units = g.Sum(x => x.Units),
							Total = g.Sum(x => x.Total),
							Subtotal = g.Sum(x => x.Subtotal),
						};
			var items = qry2.OrderByDescending (x => x.Total).ToList();
			
			AnalyzeABC (items);
			
			return PartialView("_SummaryReport", items);
		}
		
		public ViewResult BestSellingProductsBySalesPerson ()
		{
			ViewBag.EditorField = "employee";
			ViewBag.EditorTemplate = "EmployeeSelector";
			ViewBag.Title = Resources.BestSellingProductsBySalesPerson;
			return View ("SummaryReport", new DateRange(DateTime.Now, DateTime.Now));
		}
		
		[HttpPost]
		public ActionResult BestSellingProductsBySalesPerson (int employee, DateRange dates)
		{
			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			var qry = from x in SalesOrder.Queryable
					from y in x.Details
					where x.SalesPerson.Id == employee &&
						x.IsCompleted &&
						x.IsPaid &&
						!x.IsCancelled &&
						x.Date >= start &&
						x.Date <= end
					select new {
						Id = y.ProductCode,
						Name = y.ProductName,
						Units = y.Quantity,
						Total = y.Quantity * y.Price,
						Subtotal = y.Quantity * y.Price / (y.TaxRate + 1m)
					};
			var qry2 = from x in qry.ToList()
					group x by new { x.Id, x.Name } into g
					select new SummaryItem {
						Id = g.Key.Id,
						Name = g.Key.Name,
						Units = g.Sum(x => x.Units),
						Total = g.Sum(x => x.Total),
						Subtotal = g.Sum(x => x.Subtotal)
					};
			var items = qry2.OrderByDescending (x => x.Total).ToList();
			
			AnalyzeABC (items);
			
			return PartialView("_SummaryReport", items);
		}

		#endregion

		public ViewResult CustomerDebt ()
		{
			ViewBag.EditorField = "customer";
			ViewBag.EditorTemplate = "CustomerSelector";
			ViewBag.Title = Resources.CustomerDebt;
			return View ("SummaryReport", new DateRange(DateTime.Now.AddDays(1-DateTime.Now.Day), DateTime.Now));
		}

		[HttpPost]
		public ActionResult CustomerDebt (int customer, DateRange dates)
		{
			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			var query = from x in SalesOrder.Queryable
						where x.Customer.Id == customer &&
							x.IsCompleted && !x.IsCancelled && !x.IsPaid &&
							x.Date >= start && x.Date <= end
						orderby x.Date
						select x;

			return PartialView ("_CustomerDebt", query.ToList ());
		}

        //public ActionResult CustomersReport ()
        //{
        //    return View ("CustomersReport", new Search<Customer> ());
        //}

        public ViewResult CustomersReport ()
        {
            var qry = from x in Customer.Queryable
                      orderby x.Name
                      select x;

            var search = new Search<Customer> ();
            search.Limit = Configuration.PageSize;
            search.Results = qry.Skip (search.Offset).Take (search.Limit).ToList ();
            search.Total = qry.Count ();

            return View (search);
        }

        [HttpPost]
        public ActionResult CustomersReport (Search<Customer> search)
        {
            if (ModelState.IsValid) {
                search = GetCustomers (search);
            }

            if (Request.IsAjaxRequest ()) {
                return PartialView("_CustomersReport", search);
            } else {
                return View (search);
            }
        }

        Search<Customer> GetCustomers (Search<Customer> search)
        {
            if (search.Pattern == null) {
                var qry = from x in Customer.Queryable
                          orderby x.Name
                          select x;

                search.Total = qry.Count ();
                search.Results = qry.Skip (search.Offset).Take (search.Limit).ToList ();
            } else {
                var qry = from x in Customer.Queryable
                          where x.Name.Contains (search.Pattern) ||
                              x.Code.Contains (search.Pattern) ||
                              x.Zone.Contains (search.Pattern)
                          orderby x.Name
                          select x;

                search.Total = qry.Count ();
                search.Results = qry.Skip (search.Offset).Take (search.Limit).ToList ();
            }

            return search;
        }

		public ViewResult FiscalDocuments ()
		{
			ViewBag.EditorField = "taxpayer";
			ViewBag.EditorTemplate = "TaxpayerSelector";
			ViewBag.Title = Resources.FiscalDocumentsReport;

			ViewBag.FieldId = Configuration.Store.Taxpayer.Id;
			ViewBag.FieldText = Configuration.Store.Taxpayer.Name;

			return View ("SummaryReport", new DateRange ());
		}

		[HttpPost]
		public ActionResult FiscalDocuments (string taxpayer, DateRange dates)
		{
			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			var query = from x in FiscalDocument.Queryable
			            where x.Issuer.Id == taxpayer && x.IsCompleted &&
			                ((x.Issued >= start && x.Issued <= end) || (x.CancellationDate >= start && x.CancellationDate <= end))
			            orderby x.Issued
			            select x;

			return PartialView ("_FiscalDocuments", query.ToList ());
		}

		public ViewResult CustomerSalesOrders ()
		{
			ViewBag.EditorField = "customer";
			ViewBag.EditorTemplate = "CustomerSelector";
			ViewBag.Title = Resources.CustomerSalesOrders;
			return View ("SummaryReport", new DateRange ());
		}

		[HttpPost]
		public ActionResult CustomerSalesOrders (int customer, DateRange dates)
		{
			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			var query = from x in SalesOrder.Queryable
						where x.Customer.Id == customer &&
							x.IsCompleted && !x.IsCancelled &&
							x.Date >= start && x.Date <= end
						orderby x.Date
						select x;

			return PartialView ("_CustomerSalesOrders", query.ToList ());
		}

		public ViewResult ProductSalesByCustomer ()
		{
			ViewBag.EditorField = "customer";
			ViewBag.EditorTemplate = "CustomerSelector";
			ViewBag.Title = Resources.ProductSalesByCustomer;
			return View ("SummaryReport", new DateRange ());
		}

		[HttpPost]
		public ActionResult ProductSalesByCustomer (int customer, DateRange dates)
		{
			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			var query = from x in SalesOrder.Queryable
						from y in x.Details
						where x.Customer.Id == customer &&
							x.IsCompleted && !x.IsCancelled &&
							x.Date >= start && x.Date <= end
						select new {
							SalesOrder = x.Id,
							Code = y.ProductCode,
							Name = y.ProductName,
							Quantity = y.Quantity,
							Price = y.Price,
							ExchangeRate = y.ExchangeRate,
							Discount = y.Discount,
							TaxRate = y.TaxRate,
							IsTaxIncluded = y.IsTaxIncluded
						};
			var items = from x in query.ToList()
						select new SummaryItem {
							Category = x.SalesOrder.ToString (),
							Id = x.Code,
							Name = x.Name,
							Units = x.Quantity,
							Total = Model.ModelHelpers.Total (x.Quantity, x.Price, x.ExchangeRate, x.Discount, x.TaxRate, x.IsTaxIncluded),
							Subtotal = Model.ModelHelpers.Subtotal (x.Quantity, x.Price, x.ExchangeRate, x.Discount, x.TaxRate, x.IsTaxIncluded)
						};

			return PartialView ("_ProductSalesByCustomer", items);
		}

		public ViewResult ProductSalesByModel ()
		{
			ViewBag.EditorField = "productModel";
			ViewBag.EditorTemplate = "ProductModelSelector";
			ViewBag.Title = Resources.ProductSalesByModel;
			return View ("SummaryReport", new DateRange ());
		}

		[HttpPost]
		public ActionResult ProductSalesByModel (string productModel, DateRange dates)
		{
			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			var query = from x in SalesOrder.Queryable
						from y in x.Details
						where x.IsCompleted && !x.IsCancelled &&
							x.Date >= start && x.Date <= end &&
							y.Product.Model.Contains (productModel)
						orderby y.ProductName
						select new {
							Model = y.Product.Model,
							Brand = y.Product.Brand,
							Code = y.Product.Code,
							Name = y.Product.Name,
							Quantity = y.Quantity,
							Price = y.Price,
							ExchangeRate = y.ExchangeRate,
							Discount = y.Discount,
							TaxRate = y.TaxRate,
							IsTaxIncluded = y.IsTaxIncluded
						};
			var items = from x in query.ToList ()
						group x by new { x.Model, x.Brand, x.Code, x.Name } into g
						select new SummaryItem {
							Id = g.Key.Brand,
							Category = g.Key.Model,
							Code = g.Key.Code,
							Name = g.Key.Name,
							Units = g.Sum (y => y.Quantity),
							Total = g.Sum (y => Model.ModelHelpers.Total (y.Quantity, y.Price, y.ExchangeRate, y.Discount, y.TaxRate, y.IsTaxIncluded)),
							Subtotal = g.Sum (y => Model.ModelHelpers.Subtotal (y.Quantity, y.Price, y.ExchangeRate, y.Discount, y.TaxRate, y.IsTaxIncluded))
						};

			return PartialView ("_ProductSalesByCategory", items);
		}

		public ViewResult ProductSalesByBrand ()
		{
			ViewBag.EditorField = "brand";
			ViewBag.EditorTemplate = "ProductBrandSelector";
			ViewBag.Title = Resources.ProductSalesByBrand;
			return View ("SummaryReport", new DateRange ());
		}

		[HttpPost]
		public ActionResult ProductSalesByBrand (string brand, DateRange dates)
		{
			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			var query = from x in SalesOrder.Queryable
						from y in x.Details
						where x.IsCompleted && !x.IsCancelled &&
							x.Date >= start && x.Date <= end &&
							y.Product.Brand.Contains (brand)
						orderby y.ProductName
						select new {
							Model = y.Product.Model,
							Brand = y.Product.Brand,
							Code = y.Product.Code,
							Name = y.Product.Name,
							Quantity = y.Quantity,
							Price = y.Price,
							ExchangeRate = y.ExchangeRate,
							Discount = y.Discount,
							TaxRate = y.TaxRate,
							IsTaxIncluded = y.IsTaxIncluded
						};
			var items = from x in query.ToList ()
						group x by new { x.Model, x.Brand, x.Code, x.Name } into g
						select new SummaryItem {
							Id = g.Key.Brand,
							Category = g.Key.Model,
							Code = g.Key.Code,
							Name = g.Key.Name,
							Units = g.Sum (y => y.Quantity),
							Total = g.Sum (y => Model.ModelHelpers.Total (y.Quantity, y.Price, y.ExchangeRate, y.Discount, y.TaxRate, y.IsTaxIncluded)),
							Subtotal = g.Sum (y => Model.ModelHelpers.Subtotal (y.Quantity, y.Price, y.ExchangeRate, y.Discount, y.TaxRate, y.IsTaxIncluded))
						};

			return PartialView ("_ProductSalesByCategory", items);
		}

		public ViewResult SalesBySalesPerson ()
		{
			ViewBag.EditorField = "store";
			ViewBag.EditorTemplate = "StoreSelector";
			ViewBag.Title =  Resources.SalesBySalesPerson;

			ViewBag.FieldId = Configuration.Store.Id;
			ViewBag.FieldText = Configuration.Store.Name;

			return View ("SummaryReport", new DateRange ());
		}

		[HttpPost]
		public ActionResult SalesBySalesPerson (int store, DateRange dates)
		{
			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			string sql = @"SELECT salesperson SalesPersonId, first_name FirstName, last_name LastName,
							SUM(quantity) Units,
							SUM(ROUND(quantity * price * d.exchange_rate * (1 - discount) / IF(tax_included = 0, 1, 1 + tax_rate), 2)) Subtotal,
							SUM(ROUND(quantity * price * d.exchange_rate * (1 - discount) * IF(tax_included = 0, 1 + tax_rate, 1), 2)) Total
						FROM sales_order m
						INNER JOIN sales_order_detail d ON m.sales_order_id = d.sales_order
						INNER JOIN employee e ON m.salesperson = e.employee_id
						WHERE m.store = :store AND m.completed = 1 AND m.cancelled = 0 AND
							m.date >= :start AND m.date <= :end
						GROUP BY salesperson, first_name, last_name";

			var items = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate(ISession session, object instance) {
				return session.CreateSQLQuery (sql)
					.SetParameter ("start", start)
					.SetParameter ("end", end)
					.SetParameter ("store", store)
					.DynamicList();
			}, null);

			return PartialView("_SalesBySalesPerson", items);
		}

		public ViewResult SalesByCustomer ()
		{
			ViewBag.EditorField = "store";
			ViewBag.EditorTemplate = "StoreSelector";
			ViewBag.Title = Resources.SalesByCustomer;

			ViewBag.FieldId = Configuration.Store.Id;
			ViewBag.FieldText = Configuration.Store.Name;

			return View ("SummaryReport", new DateRange());
		}

		[HttpPost]
		public ActionResult SalesByCustomer (int store, DateRange dates)
		{
			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			string sql = @"SELECT customer CustomerId, name Customer,
							SUM(quantity) Units,
							SUM(ROUND(quantity * price * d.exchange_rate * (1 - discount) / IF(tax_included = 0, 1, 1 + tax_rate), 2)) Subtotal,
							SUM(ROUND(quantity * price * d.exchange_rate * (1 - discount) * IF(tax_included = 0, 1 + tax_rate, 1), 2)) Total
						FROM sales_order m
						INNER JOIN sales_order_detail d ON m.sales_order_id = d.sales_order
						INNER JOIN customer c ON m.customer = c.customer_id
						WHERE m.store = :store AND m.completed = 1 AND m.cancelled = 0 AND
							m.date >= :start AND m.date <= :end
						GROUP BY customer";

			var items = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate(ISession session, object instance) {
				return session.CreateSQLQuery (sql)
					.SetParameter ("start", start)
					.SetParameter ("end", end)
					.SetParameter ("store", store)
					.DynamicList();
			}, null);

			return PartialView("_SalesByCustomer", items);
		}

		public ViewResult SalesByProduct ()
		{
			ViewBag.EditorField = "store";
			ViewBag.EditorTemplate = "StoreSelector";
			ViewBag.Title =  Resources.SalesByProduct;

			ViewBag.FieldId = Configuration.Store.Id;
			ViewBag.FieldText = Configuration.Store.Name;

			return View ("SummaryReport", new DateRange(DateTime.Now, DateTime.Now));
		}

		[HttpPost]
		public ActionResult SalesByProduct (int store, DateRange dates)
		{
			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			string sql = @"SELECT product ProductId, p.model Model, p.name Product,
							SUM(quantity) Units,
							SUM(ROUND(quantity * price * d.exchange_rate * (1 - discount) / IF(d.tax_included = 0, 1, 1 + d.tax_rate), 2)) Subtotal,
							SUM(ROUND(quantity * price * d.exchange_rate * (1 - discount) * IF(d.tax_included = 0, 1 + d.tax_rate, 1), 2)) Total
						FROM sales_order m
						INNER JOIN sales_order_detail d ON m.sales_order_id = d.sales_order
						INNER JOIN product p ON d.product = p.product_id
						WHERE m.store = :store AND m.completed = 1 AND m.cancelled = 0 AND
							m.date >= :start AND m.date <= :end
						GROUP BY product";

			var items = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate(ISession session, object instance) {
				return session.CreateSQLQuery (sql)
					.SetParameter ("start", start)
					.SetParameter ("end", end)
					.SetParameter ("store", store)
					.DynamicList();
			}, null);

			return PartialView("_SalesByProduct", items);
		}

		public ViewResult SalesPersonOrders ()
		{
			ViewBag.EditorField = "employee";
			ViewBag.EditorTemplate = "EmployeeSelector";
			ViewBag.Title = Resources.SalesPersonOrders;
			return View ("SummaryReport", new DateRange ());
		}

		[HttpPost]
		public ActionResult SalesPersonOrders (int employee, DateRange dates)
		{
			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			string sql = @"SELECT sales_order SalesOrder, date Date, name Customer,
							GROUP_CONCAT(DISTINCT (SELECT GROUP_CONCAT(DISTINCT f.batch, f.serial SEPARATOR ' ')
								FROM fiscal_document_detail fd LEFT JOIN fiscal_document f ON fd.document = f.fiscal_document_id
								WHERE fd.order_detail = d.sales_order_detail_id) SEPARATOR ' ') Invoices,
							SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount) / IF(d.tax_included = 0, 1, 1 + d.tax_rate), 2)) Subtotal,
							SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount) * IF(d.tax_included = 0, 1 + d.tax_rate, 1), 2)) Total
						FROM sales_order m
						INNER JOIN sales_order_detail d ON m.sales_order_id = d.sales_order
						INNER JOIN customer c ON m.customer = c.customer_id
						WHERE m.salesperson = :employee AND m.completed = 1 AND m.cancelled = 0 AND
							m.date >= :start AND m.date < :end
						GROUP BY sales_order";

			var items = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate(ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);

				query.AddScalar ("SalesOrder", NHibernateUtil.Int32);
				query.AddScalar ("Date", NHibernateUtil.DateTime);
				query.AddScalar ("Customer", NHibernateUtil.String);
				query.AddScalar ("Invoices", NHibernateUtil.String);
				query.AddScalar ("Subtotal", NHibernateUtil.Decimal);
				query.AddScalar ("Total", NHibernateUtil.Decimal);

				query.SetDateTime ("start", start);
				query.SetDateTime ("end", end);
				query.SetInt32 ("employee", employee);

				return query.DynamicList();
			}, null);

			return PartialView ("_SalesPersonOrders", items);
		}

		public ViewResult SalesOrderSummary ()
		{
			ViewBag.EditorField = "store";
			ViewBag.EditorTemplate = "StoreSelector";
			ViewBag.Title = Resources.SalesOrderSummary;

			ViewBag.FieldId = Configuration.Store.Id;
			ViewBag.FieldText = Configuration.Store.Name;

			return View ("SummaryReport", new DateRange ());
		}

		[HttpPost]
		public ActionResult SalesOrderSummary (int store, DateRange dates)
		{
			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			string sql = @"SELECT date Date, CONCAT(first_name, ' ', last_name) SalesPerson, sales_order SalesOrder, m.due_date DueDate, c.name Customer,
							GROUP_CONCAT(DISTINCT (SELECT GROUP_CONCAT(DISTINCT f.batch, f.serial SEPARATOR ' ')
								FROM fiscal_document_detail fd LEFT JOIN fiscal_document f ON fd.document = f.fiscal_document_id
								WHERE fd.order_detail = d.sales_order_detail_id) SEPARATOR ' ') Invoices,
							SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount) * IF(d.tax_included = 0, 1 + d.tax_rate, 1), 2)) TotalEx,
							SUM(ROUND(d.quantity * d.price * (1 - d.discount) * IF(d.tax_included = 0, 1 + d.tax_rate, 1), 2)) Total,
							m.currency Currency
						FROM sales_order m
						INNER JOIN sales_order_detail d ON m.sales_order_id = d.sales_order
						INNER JOIN employee e ON m.salesperson = e.employee_id
						INNER JOIN customer c ON m.customer = c.customer_id
						WHERE m.store = :store AND m.completed = 1 AND m.cancelled = 0 AND
							m.date >= :start AND m.date <= :end
						GROUP BY sales_order";

			var items = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate(ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);

				query.AddScalar ("Date", NHibernateUtil.DateTime);
				query.AddScalar ("SalesPerson", NHibernateUtil.String);
				query.AddScalar ("SalesOrder", NHibernateUtil.Int32);
				query.AddScalar ("Invoices", NHibernateUtil.String);
				query.AddScalar ("DueDate", NHibernateUtil.DateTime);
				query.AddScalar ("Customer", NHibernateUtil.String);
				query.AddScalar ("TotalEx", NHibernateUtil.Decimal);
				query.AddScalar ("Total", NHibernateUtil.Decimal);
				query.AddScalar ("Currency", NHibernateUtil.Int32);

				query.SetDateTime ("start", start);
				query.SetDateTime ("end", end);
				query.SetInt32 ("store", store);

				return query.DynamicList();
			}, null);

			return PartialView ("_SalesOrderSummary", items);
		}

		public ViewResult ProductSalesBySalesPerson ()
		{
			ViewBag.EditorField = "employee";
			ViewBag.EditorTemplate = "EmployeeSelector";
			ViewBag.Title = Resources.ProductSalesBySalesPerson;
			return View ("SummaryReport", new DateRange ());
		}

		[HttpPost]
		public ActionResult ProductSalesBySalesPerson (int employee, DateRange dates)
		{
			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			string sql = @"SELECT p.brand Brand, p.model Model, p.code Code, p.name Name,
							SUM(quantity) Units,
							SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount) / IF(d.tax_included = 0, 1, 1 + d.tax_rate), 2)) Subtotal,
							SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount) * IF(d.tax_included = 0, 1 + d.tax_rate, 1), 2)) Total
						FROM sales_order m
						INNER JOIN sales_order_detail d ON m.sales_order_id = d.sales_order
						INNER JOIN product p ON d.product = p.product_id
						WHERE m.salesperson = :employee AND m.completed = 1 AND m.cancelled = 0 AND
							m.date >= :start AND m.date < :end
						GROUP BY d.product";

			var items = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate(ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);

				query.AddScalar ("Brand", NHibernateUtil.String);
				query.AddScalar ("Model", NHibernateUtil.String);
				query.AddScalar ("Code", NHibernateUtil.String);
				query.AddScalar ("Name", NHibernateUtil.String);
				query.AddScalar ("Units", NHibernateUtil.Decimal);
				query.AddScalar ("Subtotal", NHibernateUtil.Decimal);
				query.AddScalar ("Total", NHibernateUtil.Decimal);

				query.SetDateTime ("start", start);
				query.SetDateTime ("end", end);
				query.SetInt32 ("employee", employee);

				return query.DynamicList();
			}, null);

			return PartialView ("_ProductSalesBySalesPerson", items);
		}

		public ViewResult ProductSalesBySalesPersonAndLabel ()
		{
			ViewBag.EditorField = "label";
			ViewBag.EditorTemplate = "LabelSelector";
			ViewBag.Title = Resources.ProductSalesBySalesPersonAndLabel;
			return View ("SalesPersonSummaryReport", new DateRange ());
		}

		[HttpPost]
		public ActionResult ProductSalesBySalesPersonAndLabel (int employee, int? label, DateRange dates)
		{
			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			string sql = @"SELECT p.brand Brand, p.model Model, p.code Code, p.name Name,
							SUM(quantity) Units,
							SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount) / IF(d.tax_included = 0, 1, 1 + d.tax_rate), 2)) Subtotal,
							SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount) * IF(d.tax_included = 0, 1 + d.tax_rate, 1), 2)) Total
						FROM sales_order m
						INNER JOIN sales_order_detail d ON m.sales_order_id = d.sales_order
						INNER JOIN product p ON d.product = p.product_id
						JOIN_LABEL
						WHERE m.salesperson = :employee AND m.completed = 1 AND m.cancelled = 0 AND
							m.date >= :start AND m.date < :end WHERE_LABEL
						GROUP BY d.product";

			if (label.HasValue) {
				sql = sql.Replace ("JOIN_LABEL", "INNER JOIN product_label l ON d.product = l.product");
				sql = sql.Replace ("WHERE_LABEL", "AND l.label = :label");
			} else {
				sql = sql.Replace ("JOIN_LABEL", string.Empty).Replace ("WHERE_LABEL", string.Empty);
			}

			var items = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate(ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);

				query.AddScalar ("Brand", NHibernateUtil.String);
				query.AddScalar ("Model", NHibernateUtil.String);
				query.AddScalar ("Code", NHibernateUtil.String);
				query.AddScalar ("Name", NHibernateUtil.String);
				query.AddScalar ("Units", NHibernateUtil.Decimal);
				query.AddScalar ("Subtotal", NHibernateUtil.Decimal);
				query.AddScalar ("Total", NHibernateUtil.Decimal);

				query.SetDateTime ("start", start);
				query.SetDateTime ("end", end);
				query.SetInt32 ("employee", employee);

				if (label.HasValue) {
					query.SetInt32 ("label", label.Value);
				}

				return query.DynamicList();
			}, null);

			return PartialView ("_ProductSalesBySalesPerson", items);
		}

		public ViewResult ProductSalesBySalesPersonAndBrand ()
		{
			ViewBag.EditorField = "brand";
			ViewBag.EditorTemplate = "ProductBrandSelector";
			ViewBag.Title = Resources.ProductSalesBySalesPersonAndBrand;
			return View ("SalesPersonSummaryReport", new DateRange ());
		}

		[HttpPost]
		public ActionResult ProductSalesBySalesPersonAndBrand (int employee, string brand, DateRange dates)
		{
			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			string sql = @"SELECT p.brand Brand, p.model Model, p.code Code, p.name Name,
							SUM(quantity) Units,
							SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount) / IF(d.tax_included = 0, 1, 1 + d.tax_rate), 2)) Subtotal,
							SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount) * IF(d.tax_included = 0, 1 + d.tax_rate, 1), 2)) Total
						FROM sales_order m
						INNER JOIN sales_order_detail d ON m.sales_order_id = d.sales_order
						INNER JOIN product p ON d.product = p.product_id
						WHERE m.salesperson = :employee AND m.completed = 1 AND m.cancelled = 0 AND
							m.date >= :start AND m.date < :end WHERE_BRAND
						GROUP BY d.product";

			if (string.IsNullOrWhiteSpace (brand)) {
				sql = sql.Replace ("WHERE_BRAND", string.Empty);
			} else {
				sql = sql.Replace ("WHERE_BRAND", "AND p.brand = :brand");
			}

			var items = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate(ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);

				query.AddScalar ("Brand", NHibernateUtil.String);
				query.AddScalar ("Model", NHibernateUtil.String);
				query.AddScalar ("Code", NHibernateUtil.String);
				query.AddScalar ("Name", NHibernateUtil.String);
				query.AddScalar ("Units", NHibernateUtil.Decimal);
				query.AddScalar ("Subtotal", NHibernateUtil.Decimal);
				query.AddScalar ("Total", NHibernateUtil.Decimal);

				query.SetDateTime ("start", start);
				query.SetDateTime ("end", end);
				query.SetInt32 ("employee", employee);

				if (!string.IsNullOrWhiteSpace (brand)) {
					query.SetString ("brand", brand);
				}

				return query.DynamicList();
			}, null);

			return PartialView ("_ProductSalesBySalesPerson", items);
		}

		public ViewResult ProductSalesBySalesPersonAndModel ()
		{
			ViewBag.EditorField = "productModel";
			ViewBag.EditorTemplate = "ProductModelSelector";
			ViewBag.Title = Resources.ProductSalesBySalesPersonAndModel;
			return View ("SalesPersonSummaryReport", new DateRange ());
		}

		[HttpPost]
		public ActionResult ProductSalesBySalesPersonAndModel (int employee, string productModel, DateRange dates)
		{
			var start = dates.StartDate.Date;
			var end = dates.EndDate.Date.AddDays (1).AddSeconds (-1);
			string sql = @"SELECT p.brand Brand, p.model Model, p.code Code, p.name Name,
							SUM(quantity) Units,
							SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount) / IF(d.tax_included = 0, 1, 1 + d.tax_rate), 2)) Subtotal,
							SUM(ROUND(d.quantity * d.price * d.exchange_rate * (1 - d.discount) * IF(d.tax_included = 0, 1 + d.tax_rate, 1), 2)) Total
						FROM sales_order m
						INNER JOIN sales_order_detail d ON m.sales_order_id = d.sales_order
						INNER JOIN product p ON d.product = p.product_id
						WHERE m.salesperson = :employee AND m.completed = 1 AND m.cancelled = 0 AND
							m.date >= :start AND m.date < :end WHERE_MODEL
						GROUP BY d.product";

			if (string.IsNullOrWhiteSpace (productModel)) {
				sql = sql.Replace ("WHERE_MODEL", string.Empty);
			} else {
				sql = sql.Replace ("WHERE_MODEL", "AND p.model = :model");
			}

			var items = (IList<dynamic>)ActiveRecordMediator<Product>.Execute (delegate(ISession session, object instance) {
				var query = session.CreateSQLQuery (sql);

				query.AddScalar ("Brand", NHibernateUtil.String);
				query.AddScalar ("Model", NHibernateUtil.String);
				query.AddScalar ("Code", NHibernateUtil.String);
				query.AddScalar ("Name", NHibernateUtil.String);
				query.AddScalar ("Units", NHibernateUtil.Decimal);
				query.AddScalar ("Subtotal", NHibernateUtil.Decimal);
				query.AddScalar ("Total", NHibernateUtil.Decimal);

				query.SetDateTime ("start", start);
				query.SetDateTime ("end", end);
				query.SetInt32 ("employee", employee);

				if (!string.IsNullOrWhiteSpace (productModel)) {
					query.SetString ("model", productModel);
				}

				return query.DynamicList();
			}, null);

			return PartialView ("_ProductSalesBySalesPerson", items);
		}


		#region Helpers
		
		void AnalyzeABC (IEnumerable<SummaryItem> items)
		{
			decimal total = items.Sum(x => x.Total);
			decimal sum = 0;
			decimal pct;
			
			foreach (var item in items) {
				pct = sum / total;
				sum += item.Total;
				
				if(pct < 0m) {
					item.Category = "X";
				} else if(pct < 0.7m) {
					item.Category = "A";
				} else if(pct < 0.95m) {
					item.Category = "B";
				} else {
					item.Category = "C";
				}
			}
		}
		
		#endregion
	}
}
