// 
// Configuration.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
//   Eduardo Nieto <enieto@mictlanix.com>
// 
// Copyright (C) 2011-2013 Eddy Zavaleta, Mictlanix, and contributors.
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

namespace Mictlanix.BE.Web.Helpers
{
	public static class Configuration
	{
		public const string StoreCookieKey = "Store";
		public const string PointOfSaleCookieKey = "PointOfSale";
		public const string CashDrawerCookieKey = "CashDrawer";

		#region Application Global Settings
		
		public static string ApplicationTitle {
			get { return ConfigurationManager.AppSettings ["ApplicationTitle"]; }
		}

		public static string LogoTitle {
			get { return ConfigurationManager.AppSettings ["LogoTitle"]; }
		}

		public static string Company {
			get { return ConfigurationManager.AppSettings ["Company"]; }
		}

		public static string PromissoryNoteContent {
			get { return ConfigurationManager.AppSettings ["PromissoryNoteContent"]; }
		}
		
		public static string PhotosPath {
			get { return ConfigurationManager.AppSettings ["PhotosPath"]; }
		}
		
		public static string DefaultPhotoFile {
			get { return ConfigurationManager.AppSettings ["DefaultPhotoFile"]; }
		}
		
		public static decimal DefaultVAT {
			get { return Convert.ToDecimal (ConfigurationManager.AppSettings ["DefaultVAT"]); }
		}
		
		public static bool IsTaxIncluded {
			get { return Convert.ToBoolean (ConfigurationManager.AppSettings ["IsTaxIncluded"]); }
		}

		public static int PageSize {
			get { return int.Parse (ConfigurationManager.AppSettings ["PageSize"]); }
		}

		public static CurrencyCode BaseCurrency {
			get {
				var currency = CurrencyCode.MXN;
				Enum.TryParse (ConfigurationManager.AppSettings ["BaseCurrency"], out currency);
				return currency;
			}
		}

		public static CurrencyCode DefaultCurrency {
			get {
				var currency = CurrencyCode.MXN;
				Enum.TryParse (ConfigurationManager.AppSettings ["DefaultCurrency"], out currency);
				return currency;
			}
		}

		public static int DefaultCustomer {
			get { return Convert.ToInt32 (ConfigurationManager.AppSettings ["DefaultCustomer"]); }
		}
		
		public static PriceType DefaultPriceType {
			get {
				var val = PriceType.Fixed;
				Enum.TryParse<PriceType> (ConfigurationManager.AppSettings ["DefaultPriceType"], out val);
				return val;
			}
		}

		public static string MainLayout {
			get { return ConfigurationManager.AppSettings ["MainLayout"]; }
		}
		
		public static string PrintLayout {
			get { return ConfigurationManager.AppSettings ["PrintLayout"]; }
		}
		
		public static string ReceiptLayout {
			get { return ConfigurationManager.AppSettings ["ReceiptLayout"]; }
		}

		public static string Language {
			get { return CultureInfo.CurrentCulture.TwoLetterISOLanguageName; }
		}
		
		public static string DiverzaUrl {
			get { return ConfigurationManager.AppSettings ["DiverzaUrl"]; }
		}
		
		public static string DiverzaCert {
			get { return ConfigurationManager.AppSettings ["DiverzaCert"]; }
		}

		public static string DiverzaCertPasswd {
			get { return ConfigurationManager.AppSettings ["DiverzaCertPasswd"]; }
		}

		public static string DiverzaPartnerCode {
			get { return ConfigurationManager.AppSettings ["DiverzaPartnerCode"]; }
		}

		#endregion
		
		#region Request's (Local) Settings
		
		public static Store Store {
			get {
				var cookie = System.Web.HttpContext.Current.Request.Cookies [StoreCookieKey];
				
				if (cookie != null) {
					return Store.TryFind (int.Parse (cookie.Value));
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
				
				if (PointOfSale.Queryable.Count () == 1) {
					return PointOfSale.Queryable.FirstOrDefault ();
				}
				
				return null;
			}
		}
		
		public static CashDrawer CashDrawer {
			get {
				var cookie = System.Web.HttpContext.Current.Request.Cookies [CashDrawerCookieKey];
				
				if (cookie != null) {
					return CashDrawer.TryFind (int.Parse (cookie.Value));
				}
				
				if (CashDrawer.Queryable.Count () == 1) {
					return CashDrawer.Queryable.FirstOrDefault ();
				}

				return null;
			}
		}
		
		#endregion
	}
}