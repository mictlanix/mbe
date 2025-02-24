// 
// ModelHelpers.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2013 Eddy Zavaleta, Mictlanix, and contributors.
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

namespace Mictlanix.BE.Model {
	public static class ModelHelpersV2 {
		public static decimal PriceRounding (decimal d)
		{
			return Math.Round (d, 6, MidpointRounding.AwayFromZero);
		}

		public static decimal TotalRounding (decimal d)
		{
			return Math.Round (d, 2, MidpointRounding.AwayFromZero);
		}

		public static decimal NetPrice (decimal price, decimal taxRate, bool taxIncluded)
		{
			decimal divisor = taxIncluded ? 1 + taxRate : 1;

			return PriceRounding (price/divisor);
		}

		public static decimal Subtotal (decimal quantity, decimal price, decimal discountRate, decimal exchangeRate,
						decimal taxRate, bool taxIncluded, int scale)
		{
			decimal divisor = taxIncluded ? 1 + taxRate : 1;

			return TotalRounding (quantity * (price *(1 - discountRate)) * exchangeRate / divisor, scale);
		}

		public static decimal Subtotal (decimal quantity, decimal price, decimal discountRate, decimal exchangeRate,
						decimal taxRate, bool taxIncluded)
		{
			decimal divisor = taxIncluded ? 1 + taxRate : 1;

			return TotalRounding (quantity * (price * (1 - discountRate)) * exchangeRate / divisor, 6);
		}

		public static decimal Discount (decimal quantity, decimal price, decimal exchangeRate,
						decimal discountRate)
		{
			return TotalRounding (quantity * price * exchangeRate * discountRate);
		}

		//public static decimal Total (decimal quantity, decimal price, decimal exchangeRate,
		//			     decimal discountRate, decimal taxRate, bool taxIncluded)
		//{
		//	var discount = Discount(quantity, price, exchangeRate, discountRate);
		//	var tax = taxIncluded ? 1 : taxRate + 1;

		//	return TotalRounding ((quantity * (price - discount) * exchangeRate) * tax);
		//}

		public static decimal TotalRounding (decimal d, int scale)
		{
			return Math.Round (d, scale, MidpointRounding.AwayFromZero);
		}

		public static decimal Discount (decimal quantity, decimal price, decimal exchangeRate,
						decimal discountRate, int scale)
		{
			return TotalRounding (quantity * price * exchangeRate * discountRate, scale);
		}

		public static decimal Total (decimal quantity, decimal price, decimal exchangeRate,
					     decimal discountRate, decimal taxRate, bool taxIncluded, int scale)
		{
			//var discount = Discount(quantity,price,exchangeRate,discountRate);
			var tax = taxIncluded ? 1 : 1 + taxRate;

			//if (taxIncluded || taxRate <= 0m) {
			//	return TotalRounding (quantity * price * exchangeRate - discount, scale);
			//}

			return TotalRounding (quantity * price * (1 - discountRate) * exchangeRate * tax, scale);
		}

		public static decimal Total (decimal quantity, decimal price, decimal exchangeRate,
					     decimal discountRate, decimal taxRate, bool taxIncluded)
		{
			return Total (quantity, price, exchangeRate,discountRate, taxRate, taxIncluded, 6);
		}

		public static decimal PriceTaxIncluded (decimal price, decimal taxRate, bool taxIncluded)
		{
			decimal tax = taxIncluded ? 1 : 1 + taxRate; 
			//if (!taxIncluded && taxRate > 0m) {
			//	return PriceRounding (price * (1 + taxRate));
			//}

			return PriceRounding (price * tax);
		}

		public static decimal UnitPriceTotal (decimal minimalQuantity, decimal price, decimal exchangeRate,
			decimal discountRate, decimal taxRate, bool taxIncluded, int scale)
		{
			return Total (minimalQuantity, price, exchangeRate, discountRate, taxRate, taxIncluded, scale);
		}
	}
}
