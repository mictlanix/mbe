//
// ViewExtensions.cs
//
// Author:
//       eddy <eddy@mictlanix.com>
//
// Copyright (c) 2020 ${CopyrightHolder}
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;

namespace Mictlanix.BE.Web {
	public static class ViewExtensions {
		public static string RenderToString (this PartialViewResult partialView)
		{
			var httpContext = HttpContext.Current;

			if (httpContext == null) {
				throw new NotSupportedException ("An HTTP context is required to render the partial view to a string");
			}

			var controllerName = httpContext.Request.RequestContext.RouteData.Values ["controller"].ToString ();

			var controller = (ControllerBase) ControllerBuilder.Current.GetControllerFactory ().CreateController (httpContext.Request.RequestContext, controllerName);

			var controllerContext = new ControllerContext (httpContext.Request.RequestContext, controller);

			var view = ViewEngines.Engines.FindPartialView (controllerContext, partialView.ViewName).View;

			var sb = new StringBuilder ();

			using (var sw = new StringWriter (sb)) {
				using (var tw = new HtmlTextWriter (sw)) {
					view.Render (new ViewContext (controllerContext, view, partialView.ViewData, partialView.TempData, tw), tw);
				}
			}

			return sb.ToString ();
		}
	}
}
