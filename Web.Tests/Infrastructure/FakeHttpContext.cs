//
// FakeHttpContext.cs
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
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Helpers;
using Mictlanix.BE.Web.Security;

namespace Mictlanix.BE.Web.Tests.Infrastructure {
	// Gives a controller enough of an HTTP environment to run outside IIS.
	//
	// A real System.Web.HttpContext is used rather than a hand-rolled double,
	// because the code under test reaches for HttpContext.Current directly --
	// WebConfig.CashDrawer reads Request.Cookies, and PaymentsController.GetSession
	// goes through it. A stub that only satisfied Controller.Request would still
	// throw there.
	public static class FakeHttpContext {
		public static void Attach (Controller controller, CustomPrincipal user = null,
					   int? cash_drawer = null)
		{
			var request = new HttpRequest (string.Empty, "http://localhost/", string.Empty);

			// WebConfig.CashDrawer reads this cookie first and only falls back to
			// "the one drawer that exists" when it is absent -- which is no use against
			// a database that has several.
			if (cash_drawer.HasValue) {
				request.Cookies.Add (new HttpCookie (WebConfig.CashDrawerCookieKey,
								     cash_drawer.Value.ToString ()));
			}

			var response = new HttpResponse (new StringWriter ());
			var context = new HttpContext (request, response);

			if (user != null) {
				context.User = user;
			}

			HttpContext.Current = context;

			controller.ControllerContext = new ControllerContext (
				new HttpContextWrapper (context), new RouteData (), controller);
		}

		public static void Detach ()
		{
			HttpContext.Current = null;
		}

		public static CustomPrincipal Principal (Employee employee, bool administrator = true)
		{
			return new CustomPrincipal (employee.Nickname ?? "tester", "tester@example.com",
						    administrator, employee, new List<AccessPrivilege> ());
		}

		public static int StatusCode (Controller controller)
		{
			return controller.ControllerContext.HttpContext.Response.StatusCode;
		}
	}
}
