// 
// CustomersController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
//   Eduardo Nieto <enieto@mictlanix.com>
// 
// Copyright (C) 2011-2018 Mictlanix SAS de CV and contributors.
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
using System.Linq;
using System.Web.Mvc;
using Castle.ActiveRecord;
using NHibernate;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Helpers;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Mvc;
using System.IO;
using System.Collections.Generic;

namespace Mictlanix.BE.Web.Controllers.Mvc {
	[Authorize]
	public class CustomersController : CustomController {
		public ViewResult Index ()
		{
			var qry = from x in Customer.Queryable
				  orderby x.Name
				  select x;

			var search = new Search<Customer> ();
			search.Limit = WebConfig.PageSize;
			search.Results = qry.Skip (search.Offset).Take (search.Limit).ToList ();
			search.Total = qry.Count ();

			return View (search);
		}

		[HttpPost]
		public ActionResult Index (Search<Customer> search)
		{
			if (ModelState.IsValid) {
				search = GetCustomers (search);
			}

			if (Request.IsAjaxRequest ()) {
				return PartialView ("_Index", search);
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

		public ViewResult Details (int id)
		{
			var item = Customer.Find (id);
			return View (item);
		}

		public ActionResult Create ()
		{
			return PartialView ("_Create", new Customer ());
		}

		[HttpPost]
		public ActionResult Create (Customer item)
		{
			item.PriceList = PriceList.TryFind (item.PriceListId);
			if (item.SalesPersonId.HasValue) {
				item.SalesPerson = Employee.TryFind (item.SalesPersonId.Value);
			}
			
			if (!ModelState.IsValid)
				return PartialView ("_Create", item);

			using (var scope = new TransactionScope ()) {
				item.CreateAndFlush ();
			}

			return PartialView ("_CreateSuccesful", item);
		}

		public ActionResult Edit (int id)
		{
			var item = Customer.Find (id);
			//item.PriceList = PriceList.Find (item.PriceListId);
			//item.SalesPerson = Employee.Find(item.SalesPersonId.Value);
			return PartialView ("_Edit", item);
		}

		[HttpPost]
		public ActionResult Edit (Customer item)
		{
			item.PriceList = PriceList.TryFind (item.PriceListId);
			if (item.SalesPersonId.HasValue) {
				item.SalesPerson = Employee.TryFind (item.SalesPersonId.Value);
			}
			
			if (!ModelState.IsValid)
				return PartialView ("_Edit", item);

			var entity = Customer.Find (item.Id);

			entity.Code = item.Code;
			entity.Name = item.Name;
			entity.Zone = item.Zone;
			entity.PriceList = item.PriceList;
			entity.PriceListId = item.PriceListId;
			entity.CreditDays = item.CreditDays;
			entity.CreditLimit = item.CreditLimit;
			entity.Shipping = item.Shipping;
			entity.ShippingRequiredDocument = item.ShippingRequiredDocument;
			entity.Comment = item.Comment;
			entity.SalesPerson = item.SalesPerson;
			
			using (var scope = new TransactionScope ()) {
				entity.UpdateAndFlush ();
			}

			return PartialView ("_Refresh");
		}

		public ActionResult Delete (int id)
		{
			var item = Customer.Find (id);
			return PartialView ("_Delete", item);
		}

		[HttpPost, ActionName ("Delete")]
		public ActionResult DeleteConfirmed (int id)
		{
			var item = Customer.Find (id);

			try {
				using (var scope = new TransactionScope ()) {
					foreach (var discount in item.Discounts) {
						discount.Delete ();
					}
					scope.Flush ();
					item.DeleteAndFlush ();
				}
				return PartialView ("_DeleteSuccesful", item);
			} catch (Exception) {
				return PartialView ("DeleteUnsuccessful");
			}
		}

		public ActionResult Addresses (int id)
		{
			var item = Customer.Find (id);
			return PartialView ("../Addresses/_Index", item.Addresses);
		}

		public ActionResult Contacts (int id)
		{
			var item = Customer.Find (id);
			return PartialView ("../Contacts/_Index", item.Contacts);
		}

		public ActionResult Taxpayers (int id)
		{
			var item = Customer.Find (id);
			return PartialView ("_Taxpayers", item.Taxpayers);
		}

		public ActionResult Discounts (int id)
		{
			var item = Customer.Find (id);
			return PartialView ("_Discounts", item.Discounts);
		}

		public JsonResult GetSuggestions (string pattern)
		{
			var qry = from x in Customer.Queryable
				  where x.Code.Contains (pattern) ||
				      x.Name.Contains (pattern) ||
				      x.Zone.Contains (pattern)
				  select new {
					  id = x.Id,
					  name = x.Name,
					  code = x.Code,
					  hasCredit = (x.CreditDays > 0 && x.CreditLimit > 0)
				  };

			return Json (qry.ToList (), JsonRequestBehavior.AllowGet);
		}

		public JsonResult ListTaxpayers (int id)
		{
			JsonResult result = new JsonResult ();
			var qry = from x in Customer.Queryable
				  from y in x.Taxpayers
				  where x.Id == id
				  select new { id = y.Id, name = string.Format ("{1} ({0})", y.Id, y.Name) };

			result = Json (qry.ToList ());
			result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

			return result;
		}

		[HttpPost]
		public ActionResult AddTaxpayer (int id, string value)
		{
			var item = Customer.TryFind (id);
			var taxpayer = TaxpayerRecipient.TryFind (value);

			if (item == null) {
				Response.StatusCode = 400;
				return Content (Resources.CustomerNotFound);
			}

			if (taxpayer == null) {
				Response.StatusCode = 400;
				return Content (Resources.TaxpayerRecipientNotFound);
			}

			using (var scope = new TransactionScope ()) {
				item.Taxpayers.Add (taxpayer);
				item.Update ();
			}

			return Json (new { id = id, result = true });
		}

		[HttpPost]
		public ActionResult RemoveTaxpayer (int id, string value)
		{
			var item = Customer.TryFind (id);

			if (item == null) {
				Response.StatusCode = 400;
				return Content (Resources.CustomerNotFound);
			}

			var taxpayer = item.Taxpayers.SingleOrDefault (x => x.Id == value);

			if (taxpayer == null) {
				Response.StatusCode = 400;
				return Content (Resources.TaxpayerRecipientNotFound);
			}

			using (var scope = new TransactionScope ()) {
				item.Taxpayers.Remove (taxpayer);
				item.Update ();
			}

			return Json (new { id = id, result = true });
		}

		public ActionResult NewDiscount (int id)
		{
			return PartialView ("_NewDiscount");
		}

		[HttpPost]
		public ActionResult NewDiscount (int id, CustomerDiscount item)
		{
			if (!ModelState.IsValid) {
				return PartialView ("_NewDiscount", item);
			}

			item.Discount /= 100m;

			if (item.Discount > 1) {
				item.Discount = 1;
			} else if (item.Discount < 0) {
				item.Discount = 0;
			}

			using (var scope = new TransactionScope ()) {
				item.Customer = Customer.Find (id);
				item.Product = Product.Find (item.ProductId);
				item.CreateAndFlush ();
			}

			return PartialView ("_DiscountsRefresh");
		}

		[HttpPost]
		public ActionResult SetDiscount (int id, string value)
		{
			var entity = CustomerDiscount.Find (id);
			bool success;
			decimal val;

			success = decimal.TryParse (value.TrimEnd (new char [] { ' ', '%' }), out val);
			val /= 100m;

			if (success && val >= 0 && val <= 1) {
				entity.Discount = val;

				using (var scope = new TransactionScope ()) {
					entity.UpdateAndFlush ();
				}
			}

			return Json (new {
				id = entity.Id,
				value = entity.FormattedValueFor (x => x.Discount)
			});
		}

		[HttpPost]
		public ActionResult RemoveDiscount (int id)
		{
			var entity = CustomerDiscount.Find (id);

			if (entity == null) {
				Response.StatusCode = 400;
				return Content (Resources.ItemNotFound);
			}

			using (var scope = new TransactionScope ()) {
				entity.DeleteAndFlush ();
			}

			return Json (new { id = id, result = true });
		}

		public JsonResult ListEmails (int id)
		{
			var qry1 = from x in Customer.Queryable
				   from y in x.Taxpayers
				   where x.Id == id
				   select new { id = y.Email, name = string.Format ("{1} <{0}>", y.Email, y.Name) };
			var qry2 = from x in Customer.Queryable
				   from y in x.Contacts
				   where x.Id == id
				   select new { id = y.Email, name = string.Format ("{1} <{0}>", y.Email, y.Name) };

			return Json (qry1.ToList ().Union (qry2.ToList ()).ToList (), JsonRequestBehavior.AllowGet);
		}

		public ActionResult Merge ()
		{
			return View ();
		}

		[HttpPost]
		public ActionResult Merge (int customer, int duplicate)
		{
			var prod = Customer.TryFind (customer);
			var dup = Customer.TryFind (duplicate);
			string sql = @"	UPDATE customer_address SET customer = :customer WHERE customer = :duplicate;
					UPDATE customer_contact SET customer = :customer WHERE customer = :duplicate;
					UPDATE customer_discount SET customer = :customer WHERE customer = :duplicate;
					UPDATE customer_payment SET customer = :customer WHERE customer = :duplicate;
					UPDATE customer_refund SET customer = :customer WHERE customer = :duplicate;
					UPDATE customer_taxpayer SET customer = :customer WHERE customer = :duplicate;
					UPDATE delivery_order SET customer = :customer WHERE customer = :duplicate;
					UPDATE fiscal_document SET customer = :customer WHERE customer = :duplicate;
					UPDATE sales_order SET customer = :customer WHERE customer = :duplicate;
					UPDATE sales_quote SET customer = :customer WHERE customer = :duplicate;
					UPDATE tech_service_request SET customer = :customer WHERE customer = :duplicate;
					DELETE FROM customer WHERE customer_id = :duplicate;";

			ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				int ret;

				using (var tx = session.BeginTransaction ()) {
					var query = session.CreateSQLQuery (sql);

					query.AddScalar ("customer", NHibernateUtil.Int32);
					query.AddScalar ("duplicate", NHibernateUtil.Int32);

					query.SetInt32 ("customer", customer);
					query.SetInt32 ("duplicate", duplicate);

					ret = query.ExecuteUpdate ();

					tx.Commit ();
				}

				return ret;
			}, null);

			return View (new Pair<Customer, Customer> { First = prod, Second = dup });
		}

		public ActionResult Download ()
		{
			var ms = new MemoryStream ();
			var customers = (from x in Customer.Queryable
					 orderby x.Name
					 select x).ToList ();
			var taxpayers = (from x in Customer.Queryable
					 from y in x.Taxpayers
					 orderby x.Name
			                 select new { Id = x.Id, Value = y }).ToList ();
			var addresses = (from x in Customer.Queryable
					 from y in x.Addresses
					 orderby x.Name
			                 select new { Id = x.Id, Value = y }).ToList ();
			var contacts = (from x in Customer.Queryable
					from y in x.Contacts
					orderby x.Name
			                select new { Id = x.Id, Value = y }).ToList ();

			using (var package = new ExcelPackage ()) {
				int row = 2;
				var ws = package.Workbook.Worksheets.Add (Resources.Customers);
				var headers = new List<string> () { Resources.Id, Resources.Code, Resources.Name, Resources.Zone, Resources.CreditLimit, Resources.CreditDays,
					Resources.Comment };
				var widths = new List<double> () { 10.0, 25.0, 75.0, 35.0, 18.0, 18.0, 100.0 };

				for (int i = 0; i < headers.Count; i++) {
					ws.Cells [1, i + 1].Value = headers [i];
				}

				for (int i = 0; i < widths.Count; i++) {
					ws.Column (i + 1).Width = widths [i];
				}

				using (var range = ws.Cells [1, 1, 1, headers.Count]) {
					range.Style.Font.Bold = true;
					range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
				}

				foreach (var item in customers) {
					ws.Cells [row, 1].Value = item.Id;
					ws.Cells [row, 2].Value = item.Code;
					ws.Cells [row, 3].Value = item.Name;
					ws.Cells [row, 4].Value = item.Zone;
					ws.Cells [row, 5].Value = item.CreditLimit;
					ws.Cells [row, 6].Value = item.CreditDays;
					ws.Cells [row, 7].Value = item.Comment;

					row++;
				}

				using (var range = ws.Cells [2, 1, customers.Count + 1, 1]) {
					range.Style.Numberformat.Format = "000000";
				}

				using (var range = ws.Cells [2, 2, customers.Count + 1, 2]) {
					range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
				}

				using (var range = ws.Cells [2, 5, customers.Count + 1, 5]) {
					range.Style.Numberformat.Format = "$###,###,##0.00";
				}

				row = 2;
				ws = package.Workbook.Worksheets.Add (Resources.CustomerTaxpayers);
				headers = new List<string> () { Resources.Customer, Resources.TaxpayerId, Resources.TaxpayerName, Resources.Email };
				widths = new List<double> () { 10.0, 20.0, 75.0, 40.0 };

				for (int i = 0; i < headers.Count; i++) {
					ws.Cells [1, i + 1].Value = headers [i];
				}

				for (int i = 0; i < widths.Count; i++) {
					ws.Column (i + 1).Width = widths [i];
				}

				using (var range = ws.Cells [1, 1, 1, headers.Count]) {
					range.Style.Font.Bold = true;
					range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
				}

				foreach (var item in taxpayers) {
					ws.Cells [row, 1].Value = item.Id;
					ws.Cells [row, 2].Value = item.Value.Id;
					ws.Cells [row, 3].Value = item.Value.Name;
					ws.Cells [row, 4].Value = item.Value.Email;

					row++;
				}

				using (var range = ws.Cells [2, 1, customers.Count + 1, 1]) {
					range.Style.Numberformat.Format = "000000";
				}

				using (var range = ws.Cells [2, 2, customers.Count + 1, 2]) {
					range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
				}

				row = 2;
				ws = package.Workbook.Worksheets.Add (Resources.Addresses);
				headers = new List<string> () { Resources.Customer, Resources.Street, Resources.ExteriorNumber, Resources.InteriorNumber, Resources.PostalCode,
					Resources.Neighborhood, Resources.Locality, Resources.Borough, Resources.State, Resources.City, Resources.Country, Resources.Comment };
				widths = new List<double> () { 10.0, 75.0, 8.0, 8.0, 6.0, 30.0, 30.0, 30.0, 25.0, 25.0, 20.0, 100.0 };

				for (int i = 0; i < headers.Count; i++) {
					ws.Cells [1, i + 1].Value = headers [i];
				}

				for (int i = 0; i < widths.Count; i++) {
					ws.Column (i + 1).Width = widths [i];
				}

				using (var range = ws.Cells [1, 1, 1, headers.Count]) {
					range.Style.Font.Bold = true;
					range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
				}

				foreach (var item in addresses) {
					ws.Cells [row, 1].Value = item.Id;
					ws.Cells [row, 2].Value = item.Value.Street;
					ws.Cells [row, 3].Value = item.Value.ExteriorNumber;
					ws.Cells [row, 4].Value = item.Value.InteriorNumber;
					ws.Cells [row, 5].Value = item.Value.PostalCode;
					ws.Cells [row, 6].Value = item.Value.Neighborhood;
					ws.Cells [row, 7].Value = item.Value.Locality;
					ws.Cells [row, 8].Value = item.Value.Borough;
					ws.Cells [row, 9].Value = item.Value.State;
					ws.Cells [row,10].Value = item.Value.City;
					ws.Cells [row,11].Value = item.Value.Country;
					ws.Cells [row,12].Value = item.Value.Comment;

					row++;
				}

				using (var range = ws.Cells [2, 1, customers.Count + 1, 1]) {
					range.Style.Numberformat.Format = "000000";
				}

				row = 2;
				ws = package.Workbook.Worksheets.Add (Resources.Contacts);
				headers = new List<string> () { Resources.Customer, Resources.Name, Resources.JobTitle, Resources.Email, Resources.Phone,
					Resources.PhoneExt, Resources.Mobile, Resources.Fax, Resources.Im, Resources.Sip, Resources.Website, Resources.Comment };
				widths = new List<double> () { 10.0, 50.0, 25.0, 40.0, 20.0, 8.0, 25.0, 25.0, 25.0, 25.0, 25.0, 100.0 };

				for (int i = 0; i < headers.Count; i++) {
					ws.Cells [1, i + 1].Value = headers [i];
				}

				for (int i = 0; i < widths.Count; i++) {
					ws.Column (i + 1).Width = widths [i];
				}

				using (var range = ws.Cells [1, 1, 1, headers.Count]) {
					range.Style.Font.Bold = true;
					range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
				}

				foreach (var item in contacts) {
					ws.Cells [row, 1].Value = item.Id;
					ws.Cells [row, 2].Value = item.Value.Name;
					ws.Cells [row, 3].Value = item.Value.JobTitle;
					ws.Cells [row, 4].Value = item.Value.Email;
					ws.Cells [row, 5].Value = item.Value.Phone;
					ws.Cells [row, 6].Value = item.Value.PhoneExt;
					ws.Cells [row, 7].Value = item.Value.Mobile;
					ws.Cells [row, 8].Value = item.Value.Fax;
					ws.Cells [row, 9].Value = item.Value.Im;
					ws.Cells [row, 10].Value = item.Value.Sip;
					ws.Cells [row, 11].Value = item.Value.Website;
					ws.Cells [row, 12].Value = item.Value.Comment;

					row++;
				}

				using (var range = ws.Cells [2, 1, customers.Count + 1, 1]) {
					range.Style.Numberformat.Format = "000000";
				}

				package.SaveAs (ms);
			}

			return ExcelFile (ms, Resources.CustomersReport + string.Format (" {0:yyyyMMdd-HHmmss}.xlsx", DateTime.Now));
		}
	}
}