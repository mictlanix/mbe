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
	// Only new rows are ever written. Stores, customers, employees and points of sale
	// are read from whatever the target database already holds and are never modified.
	public sealed class Seed : IDisposable {
		public const string Marker = "MBE-TEST";

		readonly List<SalesOrderPayment> allocations = new List<SalesOrderPayment> ();
		readonly List<CustomerPayment> payments = new List<CustomerPayment> ();
		readonly List<SalesOrder> orders = new List<SalesOrder> ();

		public Store Store { get; private set; }
		public PointOfSale PointOfSale { get; private set; }
		public Employee Employee { get; private set; }
		public Customer Customer { get; private set; }

		public Seed ()
		{
			PointOfSale = PointOfSale.Queryable.First ();
			Store = PointOfSale.Store;
			Employee = Employee.Queryable.First (x => x.IsSalesPerson);
			Customer = Customer.Queryable.First ();
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

		public CustomerPayment Payment (PaymentType type, decimal amount)
		{
			var now = DateTime.Now;
			var item = new CustomerPayment {
				Customer = Customer,
				Store = Store,
				Amount = amount,
				Method = PaymentMethod.Cash,
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
						     decimal amount, bool confirmed = false)
		{
			var item = new SalesOrderPayment {
				SalesOrder = order,
				Payment = payment,
				PaymentId = payment.Id,
				Amount = amount,
				Change = 0m,
				Date = DateTime.Now,
				IsConfirmed = confirmed
			};

			item.CreateAndFlush ();
			allocations.Add (item);

			return item;
		}

		// Deletes children before parents. Rows the code under test already removed
		// are skipped rather than treated as an error -- that is the expected outcome
		// of a successful removal test.
		public void Dispose ()
		{
			foreach (var item in allocations) {
				Remove (SalesOrderPayment.TryFind (item.Id));
			}

			foreach (var item in payments) {
				Remove (CustomerPayment.TryFind (item.Id));
			}

			foreach (var item in orders) {
				Remove (SalesOrder.TryFind (item.Id));
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
