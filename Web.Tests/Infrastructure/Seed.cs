//
// Seed.cs
//
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
//
// Copyright (C) 2026 Eddy Zavaleta, Mictlanix, and contributors.
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
using Castle.ActiveRecord;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Tests.Infrastructure {
	// Creates the handful of rows a payment test needs and removes them again.
	//
	// Castle ActiveRecord's TransactionScope defaults to TransactionMode.New, so a
	// scope opened inside a controller action does NOT join one opened by the test --
	// it commits on its own. Rolling back an outer transaction therefore isolates
	// nothing, and every row a test creates has to be deleted by id instead.
	//
	// Only new rows are ever written. Stores, employees and points of sale are read
	// from whatever the target database already holds and are never modified; the
	// customer is created, so no real payment of its own is ever in reach.
	public sealed class Seed : IDisposable {
		public const string Marker = "MBE-TEST";

		readonly List<FiscalDocumentRelation> relations = new List<FiscalDocumentRelation> ();
		readonly List<FiscalDocumentDetail> fiscal_details = new List<FiscalDocumentDetail> ();
		readonly List<FiscalDocument> fiscal_documents = new List<FiscalDocument> ();
		readonly List<DeliveryOrderDetail> delivery_details = new List<DeliveryOrderDetail> ();
		readonly List<DeliveryOrder> deliveries = new List<DeliveryOrder> ();
		readonly List<SalesOrderPayment> allocations = new List<SalesOrderPayment> ();
		readonly List<SalesOrderDetail> details = new List<SalesOrderDetail> ();
		readonly List<CustomerPayment> payments = new List<CustomerPayment> ();
		readonly List<SalesOrder> orders = new List<SalesOrder> ();
		readonly List<CashSession> sessions = new List<CashSession> ();
		readonly List<CashDrawer> drawers = new List<CashDrawer> ();
		readonly List<AccessPrivilege> privileges = new List<AccessPrivilege> ();
		readonly List<User> users = new List<User> ();
		readonly List<Customer> customers = new List<Customer> ();

		public Store Store { get; private set; }
		public PointOfSale PointOfSale { get; private set; }
		public Employee Employee { get; private set; }
		public Customer Customer { get; private set; }

		public Seed ()
		{
			PointOfSale = PointOfSale.Queryable.First ();
			Store = PointOfSale.Store;
			Employee = Employee.Queryable.First (x => x.IsSalesPerson);
			Customer = TestCustomer (Customer.Queryable.First ().PriceList);
		}

		// A customer of its own: the apply-payment actions draw on every unallocated
		// payment of the order's customer, and a real one may have plenty.
		Customer TestCustomer (PriceList prices)
		{
			var item = new Customer {
				Code = Marker,
				Name = "Test Customer",
				CreditLimit = 0m,
				CreditDays = 0,
				PriceList = prices,
				Creator = Employee
			};

			item.CreateAndFlush ();
			customers.Add (item);

			return item;
		}

		// PaymentsController.AddPayment needs an open cash session: it reads
		// session.CashDrawer.Store and stamps the session onto the customer payment.
		// GetSession finds it from the CashDrawer cookie, which FakeHttpContext sets.
		public CashSession OpenCashSession ()
		{
			var drawer = new CashDrawer {
				Store = Store,
				Code = "MBE-TEST",
				Name = "Test Cash Drawer"
			};

			drawer.CreateAndFlush ();
			drawers.Add (drawer);

			var session = new CashSession {
				CashDrawer = drawer,
				Cashier = Employee,
				Start = DateTime.Now,
				End = null
			};

			session.CreateAndFlush ();
			sessions.Add (session);

			return session;
		}

		// A payment taken in a session that has since been counted. It ended a minute
		// ago: datetime columns keep whole seconds, and a correction made in the same
		// second as the close wouldn't count as made after it.
		public CashSession ClosedCashSession ()
		{
			var session = OpenCashSession ();

			session.Start = DateTime.Now.AddHours (-1);
			session.End = DateTime.Now.AddMinutes (-1);
			session.UpdateAndFlush ();

			return session;
		}

		// CustomController.GetAccessPrivilege reads privileges from the user row named by
		// the principal, not from the principal itself, so a privilege test needs a real
		// user, and the principal's name has to match its UserName.
		public User User (SystemObjects obj, AccessRight rights, bool administrator = false)
		{
			var user = new User {
				UserName = "mbetest",
				Password = Marker,
				Email = "tester@example.com",
				Employee = Employee,
				IsAdministrator = administrator
			};

			user.CreateAndFlush ();
			users.Add (user);

			var privilege = new AccessPrivilege {
				User = user,
				Object = obj,
				Privileges = rights
			};

			privilege.CreateAndFlush ();
			privileges.Add (privilege);

			return user;
		}

		public SalesOrder Order (bool paid = false, bool cancelled = false)
		{
			var now = DateTime.Now;
			var item = new SalesOrder {
				Store = Store,
				PointOfSale = PointOfSale,
				SalesPerson = Employee,
				Customer = Customer,
				CustomerName = Customer.Name,
				Terms = PaymentTerms.Immediate,
				Date = now,
				PromiseDate = now,
				DueDate = now,
				Currency = CurrencyCode.MXN,
				ExchangeRate = 1m,
				Priority = Priority.Normal,
				DeliveryMode = DeliveryMode.ToBeDefined,
				IsCompleted = true,
				IsCancelled = cancelled,
				IsPaid = paid,
				IsDelivered = false,
				Comment = Marker,
				Creator = Employee,
				Updater = Employee,
				CreationTime = now,
				ModificationTime = now
			};

			item.CreateAndFlush ();
			orders.Add (item);

			return item;
		}

		// A single line with no tax and no discount, so the order total is just
		// quantity x price and the balance arithmetic stays readable.
		public SalesOrderDetail Detail (SalesOrder order, decimal price, decimal quantity = 1m)
		{
			var product = Product.Queryable.First ();
			var item = new SalesOrderDetail {
				SalesOrder = order,
				Product = product,
				ProductCode = product.Code,
				ProductName = product.Name,
				Quantity = quantity,
				Cost = 0m,
				Price = price,
				DiscountRate = 0m,
				TaxRate = 0m,
				IsTaxIncluded = false,
				Currency = CurrencyCode.MXN,
				ExchangeRate = 1m,
				IsDelivery = false
			};

			item.CreateAndFlush ();
			details.Add (item);

			return item;
		}

		public CustomerPayment Payment (PaymentType type, decimal amount,
						PaymentMethod method = PaymentMethod.Cash,
						CashSession session = null)
		{
			var now = DateTime.Now;
			var item = new CustomerPayment {
				Customer = Customer,
				Store = Store,
				Amount = amount,
				Method = method,
				CashSession = session,
				Date = now,
				Currency = CurrencyCode.MXN,
				PaymentType = type,
				Serial = 0,
				Reference = Marker,
				Creator = Employee,
				Updater = Employee,
				CreationTime = now,
				ModificationTime = now
			};

			item.CreateAndFlush ();
			payments.Add (item);

			return item;
		}

		public SalesOrderPayment Allocation (SalesOrder order, CustomerPayment payment,
						     decimal amount, bool confirmed = false,
						     decimal change = 0m)
		{
			var item = new SalesOrderPayment {
				SalesOrder = order,
				Payment = payment,
				PaymentId = payment.Id,
				Amount = amount,
				Change = change,
				Date = DateTime.Now,
				IsConfirmed = confirmed
			};

			item.CreateAndFlush ();
			allocations.Add (item);

			return item;
		}

		// A stamped invoice for one order line, as ModelHelpers.InvoiceSerials finds it:
		// PUE when terms are Immediate, PPD when NetD.
		public FiscalDocument Invoice (SalesOrderDetail line, PaymentTerms terms, bool cancelled = false)
		{
			var invoice = Document (FiscalDocumentType.Invoice, terms, cancelled);
			var item = new FiscalDocumentDetail {
				Document = invoice,
				OrderDetail = line,
				Product = line.Product,
				ProductCode = line.ProductCode,
				ProductName = line.ProductName,
				Quantity = line.Quantity,
				Price = line.Price,
				DiscountRate = 0m,
				TaxRate = 0m,
				IsTaxIncluded = false,
				Currency = CurrencyCode.MXN,
				ExchangeRate = 1m
			};

			item.CreateAndFlush ();
			fiscal_details.Add (item);

			return invoice;
		}

		// A payment complement (REP) for a PPD invoice.
		public FiscalDocument PaymentReceipt (FiscalDocument invoice, bool cancelled = false)
		{
			var receipt = Document (FiscalDocumentType.PaymentReceipt, PaymentTerms.Immediate, cancelled);
			var item = new FiscalDocumentRelation {
				Document = receipt,
				Relation = invoice,
				ExchangeRate = 1m,
				Installment = 1,
				PreviousBalance = 0m,
				Amount = 0m
			};

			item.CreateAndFlush ();
			relations.Add (item);

			return receipt;
		}

		FiscalDocument Document (FiscalDocumentType type, PaymentTerms terms, bool cancelled)
		{
			var now = DateTime.Now;
			var item = new FiscalDocument {
				Type = type,
				Terms = terms,
				Batch = "TEST",
				Serial = fiscal_documents.Count + 1,
				Issuer = TaxpayerIssuer.Queryable.First (),
				Customer = Customer,
				Recipient = "XAXX010101000",
				Store = Store,
				IssuedLocation = Marker,
				PaymentMethod = PaymentMethod.Cash,
				Currency = CurrencyCode.MXN,
				ExchangeRate = 1m,
				Version = 4.0m,
				Provider = FiscalCertificationProvider.None,
				IsCompleted = true,
				IsCancelled = cancelled,
				Reference = Marker,
				Creator = Employee,
				Updater = Employee,
				CreationTime = now,
				ModificationTime = now
			};

			item.CreateAndFlush ();
			fiscal_documents.Add (item);

			return item;
		}

		// A delivery of one order line, as PaymentsController.Cancel finds it.
		public DeliveryOrder Delivery (SalesOrderDetail line, bool completed = true)
		{
			var now = DateTime.Now;
			var delivery = new DeliveryOrder {
				Store = Store,
				Serial = 0,
				Customer = Customer,
				Date = now,
				IsCompleted = completed,
				IsCancelled = false,
				Comment = Marker,
				Creator = Employee,
				Updater = Employee,
				CreationTime = now,
				ModificationTime = now
			};

			delivery.CreateAndFlush ();
			deliveries.Add (delivery);

			var item = new DeliveryOrderDetail {
				DeliveryOrder = delivery,
				OrderDetail = line,
				Product = line.Product,
				ProductCode = line.ProductCode,
				ProductName = line.ProductName,
				Quantity = line.Quantity
			};

			item.CreateAndFlush ();
			delivery_details.Add (item);

			return delivery;
		}

		// Rows created by the code under test rather than by Seed still have to be
		// cleaned up, and only the test knows how to find them.
		public void Track (params SalesOrderPayment [] items)
		{
			foreach (var item in items) {
				allocations.Add (item);

				if (item.Payment != null) {
					payments.Add (item.Payment);
				}
			}
		}

		// Deletes children before parents. Rows the code under test already removed
		// are skipped rather than treated as an error -- that is the expected outcome
		// of a successful removal test.
		public void Dispose ()
		{
			foreach (var item in relations) {
				Remove (FiscalDocumentRelation.TryFind (item.Id));
			}

			foreach (var item in fiscal_details) {
				Remove (FiscalDocumentDetail.TryFind (item.Id));
			}

			foreach (var item in fiscal_documents) {
				Remove (FiscalDocument.TryFind (item.Id));
			}

			foreach (var item in delivery_details) {
				Remove (DeliveryOrderDetail.TryFind (item.Id));
			}

			foreach (var item in deliveries) {
				Remove (DeliveryOrder.TryFind (item.Id));
			}

			foreach (var item in allocations) {
				Remove (SalesOrderPayment.TryFind (item.Id));
			}

			foreach (var item in details) {
				Remove (SalesOrderDetail.TryFind (item.Id));
			}

			// PaymentsController.EditPayment records the payment's prior state as an incidence.
			foreach (var item in payments) {
				var id = item.Id;

				foreach (var incidence in Incidence.Queryable.Where (x => x.SourceType == SourceType.CustomerPayment && x.Reference == id).ToList ()) {
					Remove (incidence);
				}

				Remove (CustomerPayment.TryFind (item.Id));
			}

			// EntitiesEditorController.RestoreDeliveryMode records the reset as an incidence.
			foreach (var item in orders) {
				var id = item.Id;

				foreach (var incidence in Incidence.Queryable.Where (x => x.SourceType == SourceType.SalesOrder && x.Reference == id).ToList ()) {
					Remove (incidence);
				}

				Remove (SalesOrder.TryFind (item.Id));
			}

			foreach (var item in sessions) {
				Remove (CashSession.TryFind (item.Id));
			}

			foreach (var item in drawers) {
				Remove (CashDrawer.TryFind (item.Id));
			}

			foreach (var item in privileges) {
				Remove (AccessPrivilege.TryFind (item.Id));
			}

			foreach (var item in users) {
				Remove (Model.User.TryFind (item.UserName));
			}

			foreach (var item in customers) {
				Remove (Model.Customer.TryFind (item.Id));
			}
		}

		static void Remove (ActiveRecordBase item)
		{
			if (item == null) {
				return;
			}

			try {
				item.DeleteAndFlush ();
			} catch (Exception e) {
				Console.WriteLine ("Seed cleanup failed: " + e.Message);
			}
		}
	}
}
