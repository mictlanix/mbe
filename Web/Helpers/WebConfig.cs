// 
// WebConfig.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
//   Eduardo Nieto <enieto@mictlanix.com>
// 
// Copyright (C) 2011-2016 Eddy Zavaleta, Mictlanix, and contributors.
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
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using System.Configuration;
using Mictlanix.BE.Model;
using System.Net.Mail;

namespace Mictlanix.BE.Web.Helpers {
	public static class WebConfig {
		public const string StoreCookieKey = "Store";
		public const string PointOfSaleCookieKey = "PointOfSale";
		public const string CashDrawerCookieKey = "CashDrawer";
		static PaymentMethod [] cashier_payment_options;
		static string [] default_email_cc;

		#region Application Global Settings

		public static string ApplicationTitle {
			get { return ConfigurationManager.AppSettings["ApplicationTitle"]; }
		}

		public static string LogoTitle {
			get { return ConfigurationManager.AppSettings["LogoTitle"]; }
		}

		public static string Company {
			get { return ConfigurationManager.AppSettings["Company"]; }
		}

		public static string PromissoryNoteContent {
			get { return ConfigurationManager.AppSettings["PromissoryNoteContent"]; }
		}

		public static string PhotosPath {
			get { return ConfigurationManager.AppSettings["PhotosPath"]; }
		}

		public static string DefaultPhotoFile {
			get { return ConfigurationManager.AppSettings["DefaultPhotoFile"]; }
		}

		public static decimal DefaultVAT {
			get { return Convert.ToDecimal (ConfigurationManager.AppSettings["DefaultVAT"]); }
		}

		public static bool IsTaxIncluded {
			get { return Convert.ToBoolean (ConfigurationManager.AppSettings["IsTaxIncluded"]); }
		}

		public static int PageSize {
			get { return int.Parse (ConfigurationManager.AppSettings["PageSize"]); }
		}

		public static CurrencyCode BaseCurrency {
			get {
				var currency = CurrencyCode.MXN;
				Enum.TryParse (ConfigurationManager.AppSettings["BaseCurrency"], out currency);
				return currency;
			}
		}

		public static CurrencyCode DefaultCurrency {
			get {
				var currency = CurrencyCode.MXN;
				Enum.TryParse (ConfigurationManager.AppSettings["DefaultCurrency"], out currency);
				return currency;
			}
		}

		public static int DefaultCustomer {
			get { return Convert.ToInt32 (ConfigurationManager.AppSettings["DefaultCustomer"]); }
		}

		public static PriceType DefaultPriceType {
			get {
				var val = PriceType.Fixed;
				Enum.TryParse<PriceType> (ConfigurationManager.AppSettings["DefaultPriceType"], out val);
				return val;
			}
		}

		public static string MainLayout {
			get { return ConfigurationManager.AppSettings["MainLayout"]; }
		}

		public static string PrintLayout {
			get { return ConfigurationManager.AppSettings["PrintLayout"]; }
		}

		public static string ReceiptLayout {
			get { return ConfigurationManager.AppSettings["ReceiptLayout"]; }
		}

		public static string Language {
			get { return CultureInfo.CurrentCulture.TwoLetterISOLanguageName; }
		}

		public static string FiscoClicUrl {
			get { return ConfigurationManager.AppSettings["FiscoClicUrl"]; }
		}

		public static string FiscoClicUser {
			get { return ConfigurationManager.AppSettings["FiscoClicUser"]; }
		}

		public static string FiscoClicPasswd {
			get { return ConfigurationManager.AppSettings["FiscoClicPasswd"]; }
		}

		public static string ServisimUrl {
			get { return ConfigurationManager.AppSettings["ServisimUrl"]; }
		}

		public static string ServisimUser {
			get { return ConfigurationManager.AppSettings["ServisimUser"]; }
		}

		public static string ServisimPasswd {
			get { return ConfigurationManager.AppSettings["ServisimPasswd"]; }
		}

		public static string ServisimPartnerCode {
			get { return ConfigurationManager.AppSettings["ServisimPartnerCode"]; }
		}

		public static string ProFactUser {
			get { return ConfigurationManager.AppSettings["ProFactUser"]; }
		}

		public static string ProFactUrl {
			get { return ConfigurationManager.AppSettings["ProFactUrl"]; }
		}

		public static string ProFactUrlV32 {
			get { return ConfigurationManager.AppSettings ["ProFactUrlV32"]; }
		}

		public static string ProFactCode {
			get { return ConfigurationManager.AppSettings["ProFactCode"]; }
		}

		public static string DFactureUrl {
			get { return ConfigurationManager.AppSettings ["DFactureUrl"]; }
		}

		public static string DFactureUser {
			get { return ConfigurationManager.AppSettings ["DFactureUser"]; }
		}

		public static string DFacturePassword {
			get { return ConfigurationManager.AppSettings ["DFacturePassword"]; }
		}

		public static string LogFilePattern {
			get { return ConfigurationManager.AppSettings["LogFilePattern"]; }
		}

		public static string AppServerUrl {
			get { return ConfigurationManager.AppSettings["AppServerUrl"]; }
		}

		public static string SmtpServer {
			get { return ConfigurationManager.AppSettings["SmtpServer"]; }
		}

		public static int SmtpPort {
			get { return int.Parse (ConfigurationManager.AppSettings["SmtpPort"]); }
		}

		public static bool SmtpSsl {
			get { return Convert.ToBoolean (ConfigurationManager.AppSettings["SmtpSsl"]); }
		}

		public static string SmtpUser {
			get { return ConfigurationManager.AppSettings["SmtpUser"]; }
		}

		public static string SmtpPassword {
			get { return ConfigurationManager.AppSettings["SmtpPassword"]; }
		}

		public static string DefaultSender {
			get { return ConfigurationManager.AppSettings["DefaultSender"]; }
		}

		public static string [] DefaultEmailCC {
			get {
				if (default_email_cc == null) {
					var list = new List<string> ();
					var tokens = ConfigurationManager.AppSettings ["DefaultEmailCC"].Split (',');

					foreach (var token in tokens) {
						try {
							var addr = new MailAddress (token.Trim ());
							list.Add (addr.Address);
						} catch {
						}
					}

					default_email_cc = list.ToArray ();
				}

				return default_email_cc;
			}
		}

		public static int DefaultQuotationDueDays {
			get { return int.Parse (ConfigurationManager.AppSettings["DefaultQuotationDueDays"]); }
		}

		public static string DefaultCfdiUsage {
			get { return ConfigurationManager.AppSettings["DefaultCfdiUsage"]; }
		}

		public static PaymentMethod [] CashierPaymentOptions {
			get {
				if (cashier_payment_options == null) {
					var list = new List<PaymentMethod> ();
					var opts = ConfigurationManager.AppSettings ["CashierPaymentOptions"].Split (',');

					foreach (var opt in opts) {
						PaymentMethod method = PaymentMethod.ToBeDefined;

						if (Enum.TryParse (opt, out method)) {
							list.Add (method);
						}
					}

					cashier_payment_options = list.ToArray ();
				}

				return cashier_payment_options;
			}
		}

		public static IList<PaymentMethodOption> StorePaymentOptions {
			get {
				var options = from x in PaymentMethodOption.Queryable
					      where x.Store.Id == CashDrawer.Store.Id && x.IsActive
					      select x;

				return options.ToList ();
			}
		}

		public static CultureInfo Culture {
			get { return System.Threading.Thread.CurrentThread.CurrentCulture; }
		}

		public static string DeliveryOrderTemplate {
			get { return ConfigurationManager.AppSettings ["DeliveryOrderTemplate"]; }
		}

		public static string DeliveryOrderTicket {
			get { return ConfigurationManager.AppSettings ["DeliveryOrderTicket"]; }
		}

		public static bool DeliveryOrdersUseMiniPrinter {
			get { return Convert.ToBoolean (ConfigurationManager.AppSettings ["DeliveryOrdersUseMiniPrinter"]); }
		}

		public static bool ShowSalesOrdersFromAllStores {
			get { return Convert.ToBoolean(ConfigurationManager.AppSettings ["ShowSalesOrdersFromAllStores"]); }
		}

		#endregion

		#region Request's (Local) Settings

		static string DefaultStore {
			get { return ConfigurationManager.AppSettings ["DefaultStore"]; }
		}

		static string DefaultPointOfSale {
			get { return ConfigurationManager.AppSettings ["DefaultPointOfSale"]; }
		}

		public static Store Store {
			get {
				var cookie = System.Web.HttpContext.Current.Request.Cookies [StoreCookieKey];

				if (cookie != null) {
					return Store.TryFind (int.Parse (cookie.Value));
				}

				if (int.TryParse (DefaultStore, out int id)) {
					return Store.TryFind (id);
				}

				if (Store.Queryable.Count () == 1) {
					return Store.Queryable.FirstOrDefault ();
				}

				return null;
			}
		}

		public static PointOfSale PointOfSale {
			get {
				var cookie = System.Web.HttpContext.Current.Request.Cookies [PointOfSaleCookieKey];

				if (cookie != null) {
					return PointOfSale.TryFind (int.Parse (cookie.Value));
				}

				if (int.TryParse (DefaultPointOfSale, out int id)) {
					return PointOfSale.TryFind (id);
				}

				if (PointOfSale.Queryable.Count () == 1) {
					return PointOfSale.Queryable.FirstOrDefault ();
				}

				return null;
			}
		}

		public static CashDrawer CashDrawer {
			get {
				var cookie = System.Web.HttpContext.Current.Request.Cookies[CashDrawerCookieKey];

				if (cookie != null) {
					return CashDrawer.TryFind (int.Parse (cookie.Value));
				}

				if (CashDrawer.Queryable.Count () == 1) {
					return CashDrawer.Queryable.FirstOrDefault ();
				}

				return null;
			}
		}

		public static int ModificationPaymentsDays { get { return int.Parse (ConfigurationManager.AppSettings ["ModificationPaymentsDays"]); } }

		#endregion
								}
}