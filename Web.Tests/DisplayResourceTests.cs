//
// DisplayResourceTests.cs
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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Resources;
using Mictlanix.BE.Model;
using NUnit.Framework;

namespace Mictlanix.BE.Web.Tests {
	// mictlanix/mbe#52: a [Display] key missing from Resources.resx only fails when
	// something renders it -- Html.LabelFor, GetDisplayName -- so a model nobody has
	// built a screen for can carry a broken key for years. This walks every [Display]
	// on a property or enum member in the Model and Web assemblies and resolves its key.
	[TestFixture]
	public class DisplayResourceTests {
		const BindingFlags Members = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

		static IEnumerable<string> Unresolved (Assembly assembly)
		{
			foreach (var type in assembly.GetTypes ()) {
				foreach (var member in type.GetMembers (Members).Where (m => m is PropertyInfo || m is FieldInfo)) {
					var display = member.GetCustomAttribute<DisplayAttribute> ();

					if (display == null || display.ResourceType == null) {
						continue;
					}

					foreach (var key in new [] { display.Name, display.ShortName, display.Description, display.Prompt, display.GroupName }) {
						if (key != null && !Resolves (display.ResourceType, key)) {
							yield return $"{type.Name}.{member.Name} -> {key}";
						}
					}
				}
			}
		}

		// DisplayAttribute reads the generated static property; the property reads the
		// .resx. Either can be missing on its own, so both are checked.
		static bool Resolves (System.Type resource_type, string key)
		{
			var property = resource_type.GetProperty (key, BindingFlags.Public | BindingFlags.Static);
			var manager = (ResourceManager) resource_type.GetProperty ("ResourceManager").GetValue (null);

			return property != null && manager.GetString (key) != null;
		}

		[Test]
		public void EveryDisplayKeyInTheModelResolves ()
		{
			var missing = Unresolved (typeof (Incidence).Assembly).ToList ();

			Assert.That (missing, Is.Empty, string.Join ("\n", missing));
		}

		[Test]
		public void EveryDisplayKeyInTheWebAppResolves ()
		{
			var missing = Unresolved (typeof (MvcApplication).Assembly).ToList ();

			Assert.That (missing, Is.Empty, string.Join ("\n", missing));
		}
	}
}
