//
// DatabaseFixture.cs
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
using System.IO;
using System.Xml.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using Mictlanix.BE.Model;
using NUnit.Framework;

namespace Mictlanix.BE.Web.Tests {
	// Boots Castle ActiveRecord once for the whole assembly, against the same
	// database the developer's Web/ActiveRecord.config points at. That file is
	// gitignored, so no connection string or password lives in the repository.
	// Override with the MBE_TEST_CONNECTION environment variable.
	[SetUpFixture]
	public class DatabaseFixture {
		public const string ConnectionVariable = "MBE_TEST_CONNECTION";

		[OneTimeSetUp]
		public void InitializeActiveRecord ()
		{
			if (ActiveRecordStarter.IsInitialized) {
				return;
			}

			var settings = ReadSettings ();
			var source = new InPlaceConfigurationSource ();

			// The web application runs with isWeb="true", which binds sessions to
			// HttpContext.Current. There is no real request here, so sessions have to
			// be scoped by the test instead.
			source.IsRunningInWebApp = false;
			source.Add (typeof (ActiveRecordBase), settings);

			ActiveRecordStarter.Initialize (typeof (Product).Assembly, source);
		}

		static IDictionary<string, string> ReadSettings ()
		{
			var path = FindConfigFile ();
			var settings = new Dictionary<string, string> ();

			foreach (var entry in XDocument.Load (path).Descendants ("add")) {
				settings [entry.Attribute ("key").Value] = entry.Attribute ("value").Value;
			}

			var overridden = Environment.GetEnvironmentVariable (ConnectionVariable);

			if (!string.IsNullOrWhiteSpace (overridden)) {
				settings ["connection.connection_string"] = overridden;
			}

			return settings;
		}

		static string FindConfigFile ()
		{
			var directory = new DirectoryInfo (TestContext.CurrentContext.TestDirectory);

			while (directory != null) {
				var candidate = Path.Combine (directory.FullName, "Web", "ActiveRecord.config");

				if (File.Exists (candidate)) {
					return candidate;
				}

				directory = directory.Parent;
			}

			throw new FileNotFoundException (
				"Web/ActiveRecord.config was not found. Copy Web/ActiveRecord.config.example " +
				"and point it at a database you are willing to write to, or set " +
				ConnectionVariable + ".");
		}
	}
}
