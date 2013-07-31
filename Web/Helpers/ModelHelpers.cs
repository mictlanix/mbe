// 
// ModelHelpers.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
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
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Mvc;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Helpers
{
    public static class ModelHelpers
    {
		public static string GetDisplayName (this Enum member)
		{
			string display_name = Enum.GetName (member.GetType (), member);

			var prop_info = member.GetType ().GetField (display_name);
			var attrs = prop_info.GetCustomAttributes (typeof (DisplayAttribute), false);

			if (attrs.Length > 0)
				display_name = ((DisplayAttribute)attrs [0]).GetName ();

			return display_name;
		}

		public static string FormattedValueFor<TModel, TResult> (this TModel entity, Expression<Func<TModel, TResult>> expression)
		{
			var view_data = new ViewDataDictionary<TModel> (entity);
			var metadata = ModelMetadata.FromLambdaExpression (expression, view_data);
			var format_string = metadata.DisplayFormatString ?? "{0}";

			if (format_string != null)
				return string.Format (format_string, metadata.Model);

			return string.Empty;
		}

		public static decimal Debt (this Customer entity)
		{
			IQueryable<decimal> qry;

			qry = from x in CustomerPayment.Queryable
				  where x.SalesOrder == null && x.Customer.Id == entity.Id
				  select x.Amount;
			var paid = qry.Count () > 0 ? qry.ToList ().Sum () : 0;

			qry = from x in SalesOrder.Queryable
				  from y in x.Details
				  where x.Terms == PaymentTerms.NetD &&
						x.IsCompleted && !x.IsCancelled &&
						x.Customer.Id == entity.Id
				  select y.Quantity * y.Price * y.ExchangeRate * (1 - y.Discount) * (y.IsTaxIncluded ? 1m : (1m + y.TaxRate));
			var bought = qry.Count () > 0 ? qry.ToList ().Sum () : 0;

			return bought - paid;
		}

		public static bool IsOverCreditLimit (this SalesOrder entity)
		{
			return (entity.Customer.Debt () + entity.TotalEx) > entity.Customer.CreditLimit;
		}

		public static decimal AmountOverCreditLimit (this SalesOrder entity)
		{
			return entity.Customer.Debt () + entity.TotalEx - entity.Customer.CreditLimit;
		}
    }
}