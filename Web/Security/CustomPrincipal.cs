// 
// CustomPrincipal.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2016 Eddy Zavaleta, Mictlanix, and contributors.
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
using System.Security.Principal;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Security {
	public class CustomPrincipal : IPrincipal {

		public CustomPrincipal (string username, string email, bool administrator, Employee employee, IList<AccessPrivilege> privileges)
		{
			Employee = employee;
			Email = email;
			IsAdministrator = administrator;
			Identity = new GenericIdentity (username);

			if (privileges == null) {
				Roles = new string [0];
			} else {
				var items = new List<string> (privileges.Count);

				foreach (var p in privileges) {
					if (p.AllowCreate) {
						items.Add (p.Object + ".Create");
					}

					if (p.AllowRead) {
						items.Add (p.Object + ".Read");
					}

					if (p.AllowUpdate) {
						items.Add (p.Object + ".Update");
					}

					if (p.AllowDelete) {
						items.Add (p.Object + ".Delete");
					}
				}

				Roles = items.ToArray ();
			}
		}

		public IIdentity Identity { get; private set; }
		public Employee Employee { get; private set; }
		public string Email { get; private set; }
		public bool IsAdministrator { get; private set; }
		public string[] Roles  { get; private set; }

		public bool IsInRole (string role)
		{
			if (IsAdministrator) {
				return true;
			}

			if (Roles.Contains (role)) {
				return true;
			}

			if (!role.Contains (".")) {
				var my_role = role + ".";
				if (Roles.Any (x => x.StartsWith (my_role))) {
					return true;
				}
			}

			return false;
		}

		public override string ToString ()
		{
			return string.Format ("{0} ({1})", Employee.Name, Email);
		}
	}
}

