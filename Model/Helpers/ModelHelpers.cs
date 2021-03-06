﻿// 
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
	public static class ModelHelpers {
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
			if (taxIncluded && taxRate > 0m) {
				return PriceRounding (price / (1 + taxRate));
			}

			return PriceRounding (price);
		}

		public static decimal Subtotal (decimal quantity, decimal price, decimal exchangeRate,
						decimal taxRate, bool taxIncluded)
		{
			if (taxIncluded && taxRate > 0m) {
				return TotalRounding (quantity * price * exchangeRate / (1m + taxRate));
			}

			return TotalRounding (quantity * price * exchangeRate);
		}

		public static decimal Discount (decimal quantity, decimal price, decimal exchangeRate,
						decimal discountRate, decimal taxRate, bool taxIncluded)
		{
			if (taxIncluded && taxRate > 0m) {
				return TotalRounding (quantity * price * exchangeRate * discountRate / (1m + taxRate));
			}

			return TotalRounding (quantity * price * exchangeRate * discountRate);
		}

		public static decimal Total (decimal quantity, decimal price, decimal exchangeRate,
					     decimal discountRate, decimal taxRate, bool taxIncluded)
		{
			var discount = 0m;

			if (discountRate > 0) {
				discount = TotalRounding (quantity * price * exchangeRate * discountRate);
			}

			if (taxIncluded || taxRate <= 0m) {
				return TotalRounding (quantity * price * exchangeRate - discount);
			}

			return TotalRounding ((quantity * price * exchangeRate - discount) * (1m + taxRate));
		}

		public static decimal TotalRounding (decimal d, int scale)
		{
			return Math.Round (d, scale, MidpointRounding.AwayFromZero);
		}

		public static decimal Subtotal (decimal quantity, decimal price, decimal exchangeRate,
						decimal taxRate, bool isTaxIncluded, int scale)
		{
			if (isTaxIncluded && taxRate > 0m) {
				return TotalRounding (quantity * price * exchangeRate / (1m + taxRate), scale);
			}

			return TotalRounding (quantity * price * exchangeRate, scale);
		}

		public static decimal Discount (decimal quantity, decimal price, decimal exchangeRate,
						decimal discountRate, decimal taxRate, bool taxIncluded, int scale)
		{
			if (taxIncluded && taxRate > 0m) {
				return TotalRounding (quantity * price * exchangeRate * discountRate / (1m + taxRate), scale);
			}

			return TotalRounding (quantity * price * exchangeRate * discountRate, scale);
		}

		public static decimal Total (decimal quantity, decimal price, decimal exchangeRate,
					     decimal discountRate, decimal taxRate, bool taxIncluded, int scale)
		{
			var discount = 0m;

			if (discountRate > 0) {
				discount = TotalRounding (quantity * price * exchangeRate * discountRate, scale);
			}

			if (taxIncluded || taxRate <= 0m) {
				return TotalRounding (quantity * price * exchangeRate - discount, scale);
			}

			return TotalRounding ((quantity * price * exchangeRate - discount) * (1m + taxRate), scale);
		}

		public static decimal PriceTaxIncluded (decimal price, decimal taxRate, bool taxIncluded)
		{
			if (!taxIncluded && taxRate > 0m) {
				return PriceRounding (price * (1 + taxRate));
			}

			return PriceRounding (price);
		}
	}
}
