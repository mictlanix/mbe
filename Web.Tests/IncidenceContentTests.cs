//
// IncidenceContentTests.cs
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
using System.Linq;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using NUnit.Framework;

namespace Mictlanix.BE.Web.Tests {
	// mictlanix/mbe#42: incidence.content was VARCHAR(1000), and a payment snapshot
	// already runs close to that. Under STRICT_TRANS_TABLES a longer one fails the
	// insert, and the correction with it. Needs Schema/changes/mbe-26.10.sql applied.
	[TestFixture]
	public class IncidenceContentTests {
		[Test]
		public void Content_HoldsMoreThanAThousandCharacters ()
		{
			var content = new string ('x', 5000);
			int id;

			using (new SessionScope ()) {
				var item = new Incidence {
					SourceType = SourceType.CustomerPayment,
					Reference = 0,
					Updater = Employee.Queryable.First (),
					PreviousState = content,
					ModificationTime = DateTime.Now,
					Comment = Infrastructure.Seed.Marker
				};

				item.CreateAndFlush ();
				id = item.Id;
			}

			try {
				using (new SessionScope ()) {
					Assert.That (Incidence.Find (id).PreviousState, Is.EqualTo (content));
				}
			} finally {
				using (new SessionScope ()) {
					Incidence.Find (id).DeleteAndFlush ();
				}
			}
		}
	}
}
