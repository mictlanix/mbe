// 
// CustomerHelpers.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.org>
//   Eduardo Nieto <enieto@mictlanix.org>
// 
// Copyright (C) 2011 Eddy Zavaleta, Mictlanix, and contributors.
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
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Helpers
{
    public static class CustomerHelpers
    {
        public static decimal CalcDebt(int id)
        {
            IQueryable<decimal> qry;

            qry = from x in CustomerPayment.Queryable
                  where x.SalesOrder == null && x.Customer.Id == id
                  select x.Amount;
            var paid = qry.Count() > 0 ? qry.ToList().Sum() : 0;

            qry = from x in SalesOrder.Queryable
                  from y in x.Details
                  where x.IsCredit && x.IsCompleted &&
                        x.Customer.Id == id 
                  select y.Quantity * y.Price * (1 - y.Discount);
            var bought = qry.Count() > 0 ? qry.ToList().Sum() : 0;

            return bought - paid;
        }
    }
}