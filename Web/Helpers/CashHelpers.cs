// 
// CashHelpers.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.org>
//   Eduardo Nieto <enieto@mictlanix.org>
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
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Helpers
{
	public static class CashHelpers
	{
		public static IList<CashCount> ListDenominations ()
		{
			string[] denominations = Resources.Denominations.Split (',');
			IList<CashCount> items = new List<CashCount> (denominations.Length);

			foreach (var item in denominations) {
				items.Add (new CashCount
                {
                    Denomination = decimal.Parse (item)
                });
			}

			return items;
		}

		static decimal GetExchangeRateA (DateTime date, CurrencyCode baseCurrency, CurrencyCode targetCurrency)
		{
			if (baseCurrency == targetCurrency)
				return decimal.One;
			
			var item = ExchangeRate.Queryable.SingleOrDefault(x => x.Date == date && x.Base == baseCurrency &&
			                                                  x.Target == targetCurrency);
			
			if (item != null)
				return item.Rate;
			
			item = ExchangeRate.Queryable.SingleOrDefault(x => x.Date == date && x.Base == targetCurrency &&
			                                              x.Target == baseCurrency);
			
			if (item != null)
				return decimal.One / item.Rate;

			return decimal.Zero;
		}

		public static decimal GetExchangeRate (DateTime date, CurrencyCode baseCurrency, CurrencyCode targetCurrency)
		{
			var val = GetExchangeRateA (date, baseCurrency, targetCurrency);

			if (val != decimal.Zero)
				return val;

			var val1 = GetExchangeRateA (date, baseCurrency, Configuration.BaseCurrency);
			var val2 = GetExchangeRateA (date, targetCurrency, Configuration.BaseCurrency);

			return val2 == decimal.Zero ? decimal.Zero : (val1 / val2);
		}
		
		public static decimal GetTodayExchangeRate (CurrencyCode baseCurrency, CurrencyCode targetCurrency)
		{
			return GetExchangeRate (DateTime.Today, baseCurrency, targetCurrency);
		}
		
		public static decimal GetTodayExchangeRate (CurrencyCode baseCurrency)
		{
			return GetExchangeRate (DateTime.Today, baseCurrency, Configuration.BaseCurrency);
		}

		public static decimal GetTodayDefaultExchangeRate()
		{
			return GetExchangeRate (DateTime.Today, Configuration.DefaultCurrency, Configuration.BaseCurrency);
		}

		public static bool ValidateExchangeRate ()
		{
			return GetTodayDefaultExchangeRate () != 0m;
		}
	}
}
