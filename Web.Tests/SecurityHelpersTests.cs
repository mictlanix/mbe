//
// SecurityHelpersTests.cs
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
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Helpers;
using NUnit.Framework;

namespace Mictlanix.BE.Web.Tests {
	// Every privilege check -- CustomController.GetAccessPrivilege, Html.GetPrivilege,
	// the menu -- goes through SecurityHelpers.GetPrivilege.
	[TestFixture]
	public class SecurityHelpersTests {
		const AccessRight All = AccessRight.Create | AccessRight.Read | AccessRight.Update | AccessRight.Delete;

		[Test]
		public void GetPrivilege_GivesAnAdministratorEveryRight_WithoutARow ()
		{
			var user = new User { IsAdministrator = true };

			Assert.That (user.GetPrivilege (SystemObjects.Incidences).Privileges, Is.EqualTo (All));
		}

		// A row that says less does not hold an administrator back either.
		[Test]
		public void GetPrivilege_GivesAnAdministratorEveryRight_OverARestrictedRow ()
		{
			var user = new User {
				IsAdministrator = true,
				Privileges = new List<AccessPrivilege> {
					new AccessPrivilege { Object = SystemObjects.PaymentsEditor, Privileges = AccessRight.None }
				}
			};

			Assert.That (user.GetPrivilege (SystemObjects.PaymentsEditor).Privileges, Is.EqualTo (All));
		}

		[Test]
		public void GetPrivilege_GivesAnyoneElseOnlyTheirRow ()
		{
			var user = new User {
				Privileges = new List<AccessPrivilege> {
					new AccessPrivilege { Object = SystemObjects.PaymentsEditor, Privileges = AccessRight.Read }
				}
			};

			Assert.That (user.GetPrivilege (SystemObjects.PaymentsEditor).Privileges, Is.EqualTo (AccessRight.Read));
			Assert.That (user.GetPrivilege (SystemObjects.Incidences).Privileges, Is.EqualTo (AccessRight.None));
		}
	}
}
