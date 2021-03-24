﻿// 
// CustomController.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2016-2018 Mictlanix SAS de CV and contributors.
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
using System.Configuration;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Mvc;
using jsreport.Client;
using jsreport.Types;
using Mictlanix.BE.Web.Helpers;
using Mictlanix.BE.Web.Security;
using MimeKit;

namespace Mictlanix.BE.Web.Mvc
{
	public abstract class CustomController : Controller {
		static string url_reports = null;

		public const string MIME_TYPE_PDF = "application/pdf";
		public const string MIME_TYPE_EXCEL_XLSX = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

		public CustomPrincipal CurrentUser {
			get {
				return User as CustomPrincipal;
			}
		}

		public string ReportServerUrl {
			get {
				if (url_reports == null) {
					url_reports = ConfigurationManager.AppSettings ["ReportServerUrl"];
				}

				return url_reports;
			}
		}

		public FileStreamResult PdfView (string viewPath, object model)
		{
			var rgx = new Regex (@"(src|href)\s?=\s?('|"")/");
			var content = RenderView (viewPath, model);
			string result = rgx.Replace (content, string.Format ("$1=$2{0}/", WebConfig.AppServerUrl));
			var reportingService = new ReportingService (ReportServerUrl);
			var report = reportingService.RenderAsync (new RenderRequest {
				Template = new Template {
					Content = result,
					Engine = Engine.None,
					Recipe = Recipe.PhantomPdf,
					Phantom = new Phantom {
						Format = PhantomFormat.Letter,
						HeaderHeight = "0 mm",
						FooterHeight = "0 mm",
						Margin = "6 mm"
					}
				}
			}).Result;

			return File (report.Content, MIME_TYPE_PDF);
		}

		public FileStreamResult PdfTicketView (string viewPath, object model)
		{
			return File (GetPdfTicket (viewPath, model), MIME_TYPE_PDF);
		}

		public Stream GetPdf (string viewPath, object model)
		{
			return GetPdf (viewPath, model, new Phantom {
				Format = PhantomFormat.Letter,
				HeaderHeight = "0 mm",
				FooterHeight = "0 mm",
				Margin = "6 mm"
			});
		}

		public Stream GetPdfTicket (string viewPath, object model)
		{
			return GetPdf (viewPath, model, new Phantom {
				Width = "72 mm",
				Height = "297 mm",
				HeaderHeight = "0 mm",
				FooterHeight = "0 mm",
				Margin = "0 mm"
			});
		}

		public Stream GetPdf (string viewPath, object model, Phantom phantom)
		{
			var rgx = new Regex (@"(src|href)\s?=\s?('|"")/");
			string content = rgx.Replace (RenderView (viewPath, model), string.Format ("$1=$2{0}/", WebConfig.AppServerUrl));
			var reportingService = new ReportingService (ReportServerUrl);

			if (!string.IsNullOrWhiteSpace (phantom.Header)) {
				phantom.Header = rgx.Replace (RenderPartialView (phantom.Header, model), string.Format ("$1=$2{0}/", WebConfig.AppServerUrl));
			}

			if (!string.IsNullOrWhiteSpace (phantom.Footer)) {
				phantom.Footer = rgx.Replace (RenderPartialView (phantom.Footer, model), string.Format ("$1=$2{0}/", WebConfig.AppServerUrl));
			}

			if (string.IsNullOrWhiteSpace (phantom.Margin)) {
				phantom.Margin = "6 mm";
			}

			var report = reportingService.RenderAsync (new RenderRequest {
				Template = new Template {
					Content = content,
					Engine = Engine.None,
					Recipe = Recipe.PhantomPdf,
					Phantom = phantom
				}
			}).Result;

			return report.Content;
		}

		public FileStreamResult PdfView (string viewPath, object model, Phantom phantom)
		{
			return File (GetPdf (viewPath, model, phantom), MIME_TYPE_PDF);
		}

		public FileResult ExcelFile (Stream stream, string fileName)
		{
			if (stream == null) {
				throw new ArgumentNullException (nameof (stream));
			}

			stream.Seek (0, SeekOrigin.Begin);

			return File (stream, MIME_TYPE_EXCEL_XLSX, fileName);
		}

		public FileResult ExcelView (string viewPath, object model)
		{
			var content = RenderPartialView (viewPath, model);

			return File (Encoding.UTF8.GetBytes (content), "application/vnd.ms-excel");
		}

		public string RenderView (string viewPath, object model)
		{
			return RenderViewToStringInternal (viewPath, model, false);
		}

		public string RenderPartialView (string viewPath, object model)
		{
			return RenderViewToStringInternal (viewPath, model, true);
		}

		protected string RenderViewToStringInternal (string viewPath, object model, bool partial = false)
		{
			string result;
			var context = ControllerContext;
			ViewEngineResult viewEngineResult;

			if (partial)
				viewEngineResult = ViewEngines.Engines.FindPartialView (context, viewPath);
			else
				viewEngineResult = ViewEngines.Engines.FindView (context, viewPath, null); 

			if (viewEngineResult == null)
				throw new FileNotFoundException ("View could not be found.");

			context.Controller.ViewData.Model = model;

			using (var sw = new StringWriter ()) {
				var view = viewEngineResult.View;
				var ctx = new ViewContext (context, view, context.Controller.ViewData, context.Controller.TempData, sw);
				view.Render (ctx, sw);
				result = sw.ToString ();
			}

			return result;
		}

		public void SendEmailWithAttachment (string recipient, string subject, string message, string attachmentName, Stream attachmentContent)
		{
			var sender = CurrentUser.Email;

			Task.Factory.StartNew (() => NotificationsHelpers.SendEmail (sender, new string [] { sender },
										     new string [] { recipient }, null, subject,
										     message, attachmentName, attachmentContent));
		}

		public void SendEmailWithAttachments (string sender, string recipient, string subject, string message,
		                                      IEnumerable<MimePart> attachments)
		{
			Task.Factory.StartNew (() => NotificationsHelpers.SendEmail (sender, new string [] { recipient }, null, null,
										     subject, message, attachments));
		}

		public void SendEmailWithAttachments (string sender, string recipient, IEnumerable<string> copyTo, string subject, string message,
						      IEnumerable<MimePart> attachments)
		{
			Task.Factory.StartNew (() => NotificationsHelpers.SendEmail (sender, new string [] { recipient }, copyTo, null,
										     subject, message, attachments));
		}
    }
}
