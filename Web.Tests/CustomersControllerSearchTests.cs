//
// CustomersControllerSearchTests.cs
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
using System.Web.Mvc;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Controllers.Mvc;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Tests.Infrastructure;
using NUnit.Framework;

namespace Mictlanix.BE.Web.Tests {
	// The customer list matches on the salesperson's first name, last name and
	// nickname as well as on the customer's own code, name and zone.
	//
	// The salesperson is resolved to a list of employee ids and filtered with IN,
	// rather than by walking Customer.SalesPerson inside the where clause: on
	// NHibernate 3 an implicit association join is an INNER join, and
	// customer.salesperson is nullable, so the direct form would silently drop every
	// customer without a salesperson from the whole result set. That is what
	// Search_ByAPatternMatchingBoth_KeepsCustomersWithoutASalesPerson guards.
	[TestFixture]
	public class CustomersControllerSearchTests {
		const string Marker = "MBE-TEST";

		CustomersController controller;
		readonly List<int> customers = new List<int> ();
		int salesperson;
		int assigned;
		int unassigned;
		string token;

		[SetUp]
		public void SetUp ()
		{
			// Unique per run, so the patterns below cannot collide with the customers
			// and employees the target database already holds.
			token = "Zz" + DateTime.Now.Ticks.ToString ("x");

			using (new SessionScope ()) {
				var employee = new Employee {
					FirstName = "First" + token,
					LastName = "Last" + token,
					Nickname = "Nick" + token,
					Gender = GenderEnum.Female,
					Birthday = new DateTime (1990, 1, 1),
					StartJobDate = new DateTime (2020, 1, 1),
					IsSalesPerson = true,
					IsActive = true,
					Comment = Marker
				};

				employee.CreateAndFlush ();
				salesperson = employee.Id;

				// Carries the token nowhere in its own columns, so matching it proves the
				// search reached the salesperson.
				assigned = NewCustomer (Marker + "-A", "Assigned Customer " + Marker, employee);

				// Matches the token by name and has no salesperson at all.
				unassigned = NewCustomer (Marker + "-U", "Unassigned " + token, null);
			}

			controller = new CustomersController ();
			FakeHttpContext.Attach (controller, null);
		}

		[TearDown]
		public void TearDown ()
		{
			using (new SessionScope ()) {
				foreach (var id in customers) {
					Remove (Customer.TryFind (id));
				}

				Remove (Employee.TryFind (salesperson));
			}

			customers.Clear ();
			FakeHttpContext.Detach ();
		}

		[Test]
		public void Search_ByTheSalesPersonFirstName_FindsTheirCustomers ()
		{
			using (new SessionScope ()) {
				var result = Run ("First" + token);

				Assert.That (Ids (result), Is.EquivalentTo (new [] { assigned }));
				Assert.That (result.Total, Is.EqualTo (1));
			}
		}

		[Test]
		public void Search_ByTheSalesPersonLastName_FindsTheirCustomers ()
		{
			using (new SessionScope ()) {
				var result = Run ("Last" + token);

				Assert.That (Ids (result), Is.EquivalentTo (new [] { assigned }));
				Assert.That (result.Total, Is.EqualTo (1));
			}
		}

		[Test]
		public void Search_ByTheSalesPersonNickname_FindsTheirCustomers ()
		{
			using (new SessionScope ()) {
				var result = Run ("Nick" + token);

				Assert.That (Ids (result), Is.EquivalentTo (new [] { assigned }));
				Assert.That (result.Total, Is.EqualTo (1));
			}
		}

		[Test]
		public void Search_ByAPatternMatchingBoth_KeepsCustomersWithoutASalesPerson ()
		{
			using (new SessionScope ()) {
				var result = Run (token);

				Assert.That (Ids (result), Is.EquivalentTo (new [] { assigned, unassigned }),
					     "a customer with no salesperson must still match on its own name");
				Assert.That (result.Total, Is.EqualTo (2));
			}
		}

		[Test]
		public void Search_WhenNoSalesPersonMatches_StillMatchesOnTheCustomer ()
		{
			using (new SessionScope ()) {
				var result = Run (Marker + "-U");

				Assert.That (Ids (result), Is.EquivalentTo (new [] { unassigned }),
					     "an empty salesperson match must not turn into an empty IN clause");
				Assert.That (result.Total, Is.EqualTo (1));
			}
		}

		Search<Customer> Run (string pattern)
		{
			var result = controller.Index (new Search<Customer> {
				Pattern = pattern,
				Limit = 50
			});

			Assert.That (result, Is.InstanceOf<ViewResult> ());

			return (Search<Customer>) ((ViewResult) result).Model;
		}

		static IEnumerable<int> Ids (Search<Customer> search)
		{
			return search.Results.Select (x => x.Id).ToList ();
		}

		int NewCustomer (string code, string name, Employee sales_person)
		{
			var price_list = PriceList.Queryable.First ();
			var item = new Customer {
				Code = code,
				Name = name,
				Zone = null,
				CreditLimit = 0m,
				CreditDays = 0,
				Comment = Marker,
				PriceList = price_list,
				PriceListId = price_list.Id,
				SalesPerson = sales_person,
				SalesPersonId = sales_person == null ? (int?) null : sales_person.Id
			};

			item.CreateAndFlush ();
			customers.Add (item.Id);

			return item.Id;
		}

		static void Remove (ActiveRecordBase item)
		{
			if (item == null) {
				return;
			}

			try {
				item.DeleteAndFlush ();
			} catch (Exception e) {
				Console.WriteLine ("cleanup failed: " + e.Message);
			}
		}
	}
}
