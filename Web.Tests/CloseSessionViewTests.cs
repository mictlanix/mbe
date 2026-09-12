//
// CloseSessionViewTests.cs
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
using NUnit.Framework;

namespace Mictlanix.BE.Web.Tests {
	// mictlanix/mbe#44 was a binding fault, not an arithmetic one: the close-session
	// screen read a projection that never set the field it displayed and filtered by.
	// CashCountReport.PaymentsReceivedByMethod was correct all along -- the screen simply
	// was not reading it, while the printed ticket already was.
	//
	// Razor views are compiled when they are first requested, not by msbuild, so a screen
	// bound to the wrong property builds cleanly and fails in front of a cashier closing
	// the till. Rendering these views in-process would mean standing up a view engine and
	// a virtual path provider; asserting on the source is the cheap guard that still
	// catches the regression that actually happened.
	[TestFixture]
	public class CloseSessionViewTests {
		static string Source (string view)
		{
			var directory = new DirectoryInfo (TestContext.CurrentContext.TestDirectory);

			while (directory != null) {
				var candidate = Path.Combine (directory.FullName, "Web", "Views", "Payments", view);

				if (File.Exists (candidate)) {
					return File.ReadAllText (candidate);
				}

				directory = directory.Parent;
			}

			throw new FileNotFoundException ("Could not locate Web/Views/Payments/" + view);
		}

		[Test]
		public void CloseSessionScreen_ReadsTheBreakdownFromPaymentsReceivedByMethod ()
		{
			var view = Source ("CloseSessionConfirmed.cshtml");

			Assert.That (view, Does.Contain ("Model.PaymentsReceivedByMethod"));
			Assert.That (view, Does.Not.Contain ("MoneyCounts"),
				     "MoneyCounts was the projection that never assigned Type");
		}

		// The column header is the payment method, so the cell has to be the method too.
		// It used to print Type, which nothing ever assigned, so every row read "N/A".
		[Test]
		public void CloseSessionScreen_LabelsEachRowByMethod ()
		{
			var view = Source ("CloseSessionConfirmed.cshtml");

			Assert.That (view, Does.Contain ("item.Method.GetDisplayName()"));
			Assert.That (view, Does.Not.Contain ("item.Type.GetDisplayName()"));
		}

		// The screen and the printed ticket report the same session and must not drift
		// apart again -- that divergence is what #44 was.
		[Test]
		public void ScreenAndTicket_ReadTheSameProperty ()
		{
			Assert.That (Source ("CloseSessionConfirmed.cshtml"),
				     Does.Contain ("Model.PaymentsReceivedByMethod"));
			Assert.That (Source ("_CashCountTicket.cshtml"),
				     Does.Contain ("Model.PaymentsReceivedByMethod"));
		}
	}
}
