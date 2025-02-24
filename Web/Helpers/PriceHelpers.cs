// 
// ModelHelpers.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2011-2017 Eddy Zavaleta, Mictlanix, and contributors.
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
using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Mvc;
using Mictlanix.BE.Model;
using static NHibernate.Engine.Query.CallableParser;

namespace Mictlanix.BE.Web.Helpers {
	public static class PriceHelpers {

		public static decimal GetMinimalPrice (this Product product) {

			var item = ProductPrice.Queryable.SingleOrDefault (x => x.Product == product && x.List == WebConfig.CostsList);
			
			//if (item != null && item.Value > 0) {
			//	return item.Value;
			//}

			decimal cost = product.GetCost ();

			//return Math.Ceiling(cost / (1 - item.List.LowProfitMargin));
			return Math.Ceiling(cost * (1 + item.LowProfitRate));

		}

		public static decimal GetMaximumPrice (this Product product) {
			var item = ProductPrice.Queryable.SingleOrDefault (x => x.Product == product && x.List == WebConfig.CostsList);

			//if (item != null && item.Value > 0) {
			//	return item.Value;
			//}

			decimal cost = product.GetCost ();

			//return Math.Ceiling(cost / (1 - item.List.HighProfitMargin));
			return Math.Ceiling(cost * (1 + item.HighProfitRate));

		}

		public static decimal GetPrice (this Product product, int price_list) {
			var item = ProductPrice.Queryable.SingleOrDefault (x => x.Product == product && x.List.Id == price_list);

			//if (item != null) {
			//	return item.Value;
			//}

			//decimal cost = product.GetCost ();

			//return cost != 0 ? Math.Ceiling(cost / (1 - (item.List.HighProfitMargin + item.List.LowProfitMargin)/2 )):0;
			return item.Value;
		}

		public static decimal GetCost (this Product product)
		{
			return ProductPrice.Queryable.Single (x => x.Product == product && x.List == WebConfig.CostsList).Value;

		}

		public static bool IsPriceInRange (this SalesOrderDetail detail) {
			var min = detail.Product.GetMinimalPrice ();
			var max = detail.Product.GetMaximumPrice ();
			var price = detail.Price;

			if (price < min)
				return false;

			if(max <= 0.0m)
				return true;

			if (price > max) return false;

			return false;
		}

		public static bool IsPriceInRange (this SalesQuoteDetail detail)
		{
			var min_rate = detail.Product.Prices.Single(x => x.List == WebConfig.CostsList).LowProfitRate;
			var max_rate = detail.Product.Prices.Single(x => x.List == WebConfig.CostsList).HighProfitRate;
			var max = detail.Product.GetMaximumPrice ();
			var min = detail.Product.GetMinimalPrice ();
			var cost = detail.Product.GetCost ();

			var price = detail.UnitPriceWithDiscount;

			if ((price < min && min_rate > 0.0m) || (price > max && max_rate > 0.0m))
				return false;

			return true;
		}

		public static string GetPriceRange (this Product product) {
			
			var min_rate = product.Prices.Single (x => x.List == WebConfig.CostsList).LowProfitRate;
			var max_rate = product.Prices.Single (x => x.List == WebConfig.CostsList).HighProfitRate;
			var max = product.GetMaximumPrice ();
			var min = product.GetMinimalPrice ();

			string text = "( ";
			text += min_rate > 0.0m ? min.ToString ("C2") : " - ";
			text += ", ";
			text += max_rate > 0.0m ? max.ToString ("C2") : " - ";
			text += " )";

			return text;
		}
	}
}