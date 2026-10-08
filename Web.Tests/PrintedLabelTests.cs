//
// PrintedLabelTests.cs
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
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Mictlanix.BE.Web.Tests {
	// mictlanix/mbe#62: two printed labels named the wrong value. Razor views are
	// compiled on first request, not by msbuild, so these read the view source --
	// the same approach as CloseSessionViewTests.
	[TestFixture]
	public class PrintedLabelTests {
		static string Source (string folder, string view)
		{
			var directory = new DirectoryInfo (TestContext.CurrentContext.TestDirectory);

			while (directory != null) {
				var candidate = Path.Combine (directory.FullName, "Web", "Views", folder, view);

				if (File.Exists (candidate)) {
					return File.ReadAllText (candidate);
				}

				directory = directory.Parent;
			}

			throw new FileNotFoundException ("Could not locate Web/Views/" + folder + "/" + view);
		}

		// The label is the last resource named before the value is displayed.
		static string LabelBefore (string view, string value)
		{
			var at = view.IndexOf (value);

			Assert.That (at, Is.GreaterThan (-1), value + " is displayed");

			var labels = Regex.Matches (view.Substring (0, at), @"@Resources\.(\w+)");

			return labels [labels.Count - 1].Groups [1].Value;
		}

		// SalesOrder.Date is [DataType (DataType.Date)]: no time is printed.
		[Test]
		public void SalesOrderDocument_LabelsTheOrderDateAsADate ()
		{
			var view = Source ("SalesOrders", "Print.cshtml");

			Assert.That (LabelBefore (view, "DisplayFor(x => x.Date)"), Is.EqualTo ("Date"));
		}

		// "Efectivo Contado" is counted cash; this row is cash sales.
		[Test]
		public void CashCountTicket_LabelsTheCashSalesRowAsCashSales ()
		{
			var view = Source ("Payments", "_CashCountTicket.cshtml");

			Assert.That (LabelBefore (view, "DisplayFor(model => model.CashSales)"), Is.EqualTo ("CashSales"));
		}
	}
}
