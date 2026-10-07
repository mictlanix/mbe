//
// UsersControllerAccessTests.cs
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
using System.Collections.Generic;
using System.Web.Mvc;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Controllers.Mvc;
using Mictlanix.BE.Web.Security;
using Mictlanix.BE.Web.Tests.Infrastructure;
using NUnit.Framework;

namespace Mictlanix.BE.Web.Tests {
	// /Users had no server-side check at all: the menu hid the link from
	// non-administrators, but any signed-in user could open the URL, grant
	// themselves privileges and tick "Administrador". The check is an action
	// filter, so these run it the way MVC does before any action.
	[TestFixture]
	public class UsersControllerAccessTests {
		const AccessRight All = AccessRight.Create | AccessRight.Read | AccessRight.Update | AccessRight.Delete;

		Seed seed;

		[SetUp]
		public void SetUp ()
		{
			using (new SessionScope ()) {
				seed = new Seed ();
			}
		}

		[TearDown]
		public void TearDown ()
		{
			using (new SessionScope ()) {
				seed.Dispose ();
			}

			FakeHttpContext.Detach ();
		}

		// The principal can claim administrator independently of the stored user,
		// as a login cookie can after the flag is revoked.
		ActionResult Run (string action, bool administrator, bool principal_administrator)
		{
			var controller = new UsersController ();

			using (new SessionScope ()) {
				var user = seed.User (SystemObjects.Users, All, administrator);
				var principal = new CustomPrincipal (user.UserName, user.Email, principal_administrator,
								     seed.Employee, new List<AccessPrivilege> ());

				FakeHttpContext.Attach (controller, principal);
			}

			var descriptor = new ReflectedControllerDescriptor (typeof (UsersController))
				.FindAction (controller.ControllerContext, action);
			var context = new ActionExecutingContext (controller.ControllerContext, descriptor,
								  new Dictionary<string, object> ());

			using (new SessionScope ()) {
				((IActionFilter) controller).OnActionExecuting (context);
			}

			return context.Result;
		}

		[Test]
		public void NonAdministrator_IsTurnedAway_EvenWithEveryUsersPrivilege ()
		{
			foreach (var action in new [] { "Index", "Details", "Edit", "Delete" }) {
				var result = Run (action, administrator: false, principal_administrator: false);

				Assert.That (result, Is.InstanceOf<RedirectToRouteResult> (), action);
				Assert.That (((RedirectToRouteResult) result).RouteValues ["controller"], Is.EqualTo ("Home"), action);

				TearDown ();
				SetUp ();
			}
		}

		[Test]
		public void Administrator_GetsThrough ()
		{
			Assert.That (Run ("Edit", administrator: true, principal_administrator: true), Is.Null);
		}

		[Test]
		public void RevokedAdministrator_IsTurnedAway_DespiteTheirLogin ()
		{
			Assert.That (Run ("Edit", administrator: false, principal_administrator: true),
				     Is.InstanceOf<RedirectToRouteResult> ());
		}
	}
}
