//
// EntitiesEditorControllerTests.cs
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
using System.Web.Mvc;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Controllers.Mvc;
using Mictlanix.BE.Web.Security;
using Mictlanix.BE.Web.Tests.Infrastructure;
using NUnit.Framework;

namespace Mictlanix.BE.Web.Tests {
	// mictlanix/mbe#64: once a cashier picked Entrega Única en Mostrador there was no
	// way back. The admin tool that resets it posted to a controller that does not
	// exist, checked for an administrator only on that POST and only from the login
	// cookie, reset any delivery mode, and logged "Completado" with no previous state.
	//
	// The tool is now three steps: find the order, preview the change (which writes
	// nothing), confirm.
	[TestFixture]
	public class EntitiesEditorControllerTests {
		Seed seed;
		User user;

		[SetUp]
		public void SetUp ()
		{
			using (new SessionScope ()) {
				seed = new Seed ();
			}

			user = null;
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
		// as a login cookie can after the flag is revoked. One user per test: a test
		// that walks several steps logs in once per step.
		EntitiesEditorController LogIn (bool administrator, bool principal_administrator)
		{
			var controller = new EntitiesEditorController ();

			using (new SessionScope ()) {
				user = user ?? seed.User (SystemObjects.SalesOrders, AccessRight.None, administrator);
				Assert.That (user.IsAdministrator, Is.EqualTo (administrator), "one kind of user per test");

				var principal = new CustomPrincipal (user.UserName, user.Email, principal_administrator,
								     seed.Employee, new List<AccessPrivilege> ());

				FakeHttpContext.Attach (controller, principal);
			}

			return controller;
		}

		// Runs the filter the way MVC does before an action, then the action only if
		// the filter let it through.
		ActionResult Run (EntitiesEditorController controller, string action, Type [] signature, Func<ActionResult> body)
		{
			var method = typeof (EntitiesEditorController).GetMethod (action, signature);
			var descriptor = new ReflectedActionDescriptor (method, method.Name,
				new ReflectedControllerDescriptor (typeof (EntitiesEditorController)));
			var context = new ActionExecutingContext (controller.ControllerContext, descriptor,
								  new Dictionary<string, object> ());

			using (new SessionScope ()) {
				((IActionFilter) controller).OnActionExecuting (context);
				return context.Result ?? body ();
			}
		}

		ActionResult Find (bool administrator, bool principal_administrator)
		{
			var controller = LogIn (administrator, principal_administrator);
			return Run (controller, "RestoreDeliveryMode", Type.EmptyTypes, () => controller.RestoreDeliveryMode ());
		}

		ActionResult Preview (int? id, bool administrator = true)
		{
			var controller = LogIn (administrator, administrator);
			return Run (controller, "RestoreDeliveryModePreview", new [] { typeof (int?) },
				    () => controller.RestoreDeliveryModePreview (id));
		}

		ActionResult Confirm (int id, bool administrator = true)
		{
			var controller = LogIn (administrator, administrator);
			return Run (controller, "RestoreDeliveryMode", new [] { typeof (int) },
				    () => controller.RestoreDeliveryMode (id));
		}

		int Order (DeliveryMode mode)
		{
			using (new SessionScope ()) {
				var order = seed.Order (paid: true);
				order.DeliveryMode = mode;
				order.UpdateAndFlush ();
				return order.Id;
			}
		}

		static DeliveryMode ModeOf (int id)
		{
			using (new SessionScope ()) {
				return SalesOrder.Find (id).DeliveryMode;
			}
		}

		static List<Incidence> IncidencesOf (int id)
		{
			using (new SessionScope ()) {
				return Incidence.Queryable.Where (x => x.SourceType == SourceType.SalesOrder && x.Reference == id).ToList ();
			}
		}

		static string ErrorOf (ActionResult result)
		{
			return (string) ((ViewResult) result).ViewData ["Error"];
		}

		static string ViewNameOf (ActionResult result)
		{
			return ((ViewResult) result).ViewName;
		}

		static readonly string NothingToChange = string.Format (Resources.RestoreDeliveryModeNothingToChange, Resources.ToBeDefined);
		static readonly string OnlyPickUp = string.Format (Resources.RestoreDeliveryModeOnlyPickUp, Resources.PartialDeliveries, Resources.PickUp);

		[Test]
		public void NonAdministrator_IsTurnedAway_FromEveryStep ()
		{
			var id = Order (DeliveryMode.PickUp);

			foreach (var result in new [] { Find (false, false), Preview (id, false), Confirm (id, false) }) {
				Assert.That (result, Is.InstanceOf<RedirectToRouteResult> ());
				Assert.That (((RedirectToRouteResult) result).RouteValues ["controller"], Is.EqualTo ("Home"));
			}

			Assert.That (ModeOf (id), Is.EqualTo (DeliveryMode.PickUp));
			Assert.That (IncidencesOf (id), Is.Empty);
		}

		[Test]
		public void RevokedAdministrator_IsTurnedAway_DespiteTheirLogin ()
		{
			Assert.That (Find (administrator: false, principal_administrator: true), Is.InstanceOf<RedirectToRouteResult> ());
		}

		[Test]
		public void Administrator_SeesTheSearchForm ()
		{
			Assert.That (Find (administrator: true, principal_administrator: true), Is.InstanceOf<ViewResult> ());
		}

		[Test]
		public void Preview_ShowsAPickUpOrder_AndChangesNothing ()
		{
			var id = Order (DeliveryMode.PickUp);
			var result = (ViewResult) Preview (id);

			Assert.That (((SalesOrder) result.Model).Id, Is.EqualTo (id));
			Assert.That (ErrorOf (result), Is.Null);
			Assert.That (ModeOf (id), Is.EqualTo (DeliveryMode.PickUp));
			Assert.That (IncidencesOf (id), Is.Empty);
		}

		[Test]
		public void Preview_ExplainsWhyAnOrderCannotBeReset ()
		{
			Assert.That (ErrorOf (Preview (Order (DeliveryMode.ToBeDefined))), Is.EqualTo (NothingToChange));
			Assert.That (ErrorOf (Preview (Order (DeliveryMode.PartialDeliveries))), Is.EqualTo (OnlyPickUp));
		}

		[Test]
		public void Preview_SendsAMissingOrderBackToTheSearch ()
		{
			foreach (var id in new int? [] { int.MaxValue, null }) {
				var result = Preview (id);

				Assert.That (ViewNameOf (result), Is.EqualTo ("RestoreDeliveryMode"));
				Assert.That (ErrorOf (result), Is.EqualTo (Resources.SalesOrderNotFound));
			}
		}

		[Test]
		public void Confirm_ResetsAPickUpOrder_AndLeavesAnIncidence ()
		{
			var id = Order (DeliveryMode.PickUp);
			var result = Confirm (id);

			Assert.That (ViewNameOf (result), Is.EqualTo ("RestoreDeliveryModeDone"));
			Assert.That (ModeOf (id), Is.EqualTo (DeliveryMode.ToBeDefined));

			var incidences = IncidencesOf (id);
			Assert.That (incidences, Has.Count.EqualTo (1));
			Assert.That (incidences [0].PreviousState, Is.EqualTo (Resources.PickUp));
			Assert.That (incidences [0].Comment, Is.EqualTo (Resources.PickUp + " → " + Resources.ToBeDefined));
			Assert.That (incidences [0].Updater.Id, Is.EqualTo (seed.Employee.Id));
		}

		// The order may change between the preview and the confirmation, or the form
		// may be submitted twice.
		[Test]
		public void Confirm_RefusesAnythingButPickUp_AndChangesNothing ()
		{
			foreach (var item in new [] {
				new { Mode = DeliveryMode.PartialDeliveries, Error = OnlyPickUp },
				new { Mode = DeliveryMode.ToBeDefined, Error = NothingToChange } }) {
				var id = Order (item.Mode);
				var result = Confirm (id);

				Assert.That (ViewNameOf (result), Is.EqualTo ("RestoreDeliveryModePreview"), item.Mode.ToString ());
				Assert.That (ErrorOf (result), Is.EqualTo (item.Error), item.Mode.ToString ());
				Assert.That (ModeOf (id), Is.EqualTo (item.Mode), item.Mode.ToString ());
				Assert.That (IncidencesOf (id), Is.Empty, item.Mode.ToString ());
			}
		}

		[Test]
		public void Confirm_SendsAMissingOrderBackToTheSearch ()
		{
			var result = Confirm (int.MaxValue);

			Assert.That (ViewNameOf (result), Is.EqualTo ("RestoreDeliveryMode"));
			Assert.That (ErrorOf (result), Is.EqualTo (Resources.SalesOrderNotFound));
		}
	}
}
