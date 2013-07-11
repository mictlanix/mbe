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

namespace Mictlanix.BE.Model
{
	public static class ModelHelpers
	{
		public static decimal Round (decimal d)
		{
			return Math.Round (d, 2, MidpointRounding.AwayFromZero);
		}
		
		public static decimal Subtotal (decimal quantity, decimal price, decimal exchangeRate,
		                                decimal discount, decimal taxRate, bool taxIncluded)
		{
			if (taxIncluded) {
				return Round (quantity * price * exchangeRate * (1m - discount) / (1m + taxRate));
			} else {
				return Round (quantity * price * exchangeRate * (1m - discount));
			}
		}

		public static decimal Total (decimal quantity, decimal price, decimal exchangeRate,
		                             decimal discount, decimal taxRate, bool taxIncluded)
		{
			if (taxIncluded) {
				return Round (quantity * price * exchangeRate * (1m - discount));
			} else {
				return Round (quantity * price * exchangeRate * (1m - discount) * (1m + taxRate));
			}
		}
	}
}