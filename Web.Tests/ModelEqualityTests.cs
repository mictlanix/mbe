//
// ModelEqualityTests.cs
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
using System.Reflection;
using Mictlanix.BE.Model;
using NUnit.Framework;

namespace Mictlanix.BE.Web.Tests {
	// mictlanix/mbe#51: the models' Equals overrides were copied from one another,
	// and four kept the original's cast -- Incidence compared itself as a
	// DeliveryOrder, so two incidences were never equal, and an incidence equalled
	// a delivery order with the same id. This checks every model that overrides
	// Equals and is keyed by an integer Id.
	[TestFixture]
	public class ModelEqualityTests {
		static IEnumerable<Type> Models ()
		{
			return typeof (Incidence).Assembly.GetTypes ()
				.Where (t => t.IsClass && !t.IsAbstract && t.GetConstructor (Type.EmptyTypes) != null)
				.Where (t => t.GetMethod ("Equals", new [] { typeof (object) }).DeclaringType == t)
				.Where (t => {
					var id = t.GetProperty ("Id");
					return id != null && id.PropertyType == typeof (int) && id.CanWrite;
				});
		}

		static object WithId (Type type, int id)
		{
			var item = Activator.CreateInstance (type);
			type.GetProperty ("Id").SetValue (item, id);
			return item;
		}

		[Test]
		public void SameTypeAndId_AreEqual ()
		{
			var broken = Models ().Where (t => !WithId (t, 5).Equals (WithId (t, 5)))
					      .Select (t => t.Name).ToList ();

			Assert.That (Models ().Count (), Is.GreaterThan (10), "the sweep found the models");
			Assert.That (broken, Is.Empty, string.Join (", ", broken));
		}

		[Test]
		public void DifferentTypes_WithTheSameId_AreNotEqual ()
		{
			var models = Models ().ToList ();
			var broken = (from a in models
				      from b in models
				      where a != b && WithId (a, 5).Equals (WithId (b, 5))
				      select a.Name + " = " + b.Name).ToList ();

			Assert.That (broken, Is.Empty, string.Join (", ", broken));
		}
	}
}
