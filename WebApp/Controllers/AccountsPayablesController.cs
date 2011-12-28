// 
// AccountsPayablesController.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Castle.ActiveRecord;
using Business.Essentials.Model;
using Business.Essentials.WebApp.Models;
using Business.Essentials.WebApp.Helpers;


namespace Business.Essentials.WebApp.Controllers
{
    public class AccountsPayablesController : Controller
    {
        //
        // GET: /Statements/

        // TODO: Obtimise DB qry al memory
        public ActionResult Index()
        {
            var results = new List<AccountsPayableSummary>();
            var qry_payments = (from x in SupplierPayment.Queryable
                            group x by x.Supplier into c
                            select new
                            {
                                Supplier = c.Key,
                                Payments = c.ToList()
                            }).ToList();

            var payments = (from x in qry_payments
                            select new AccountsPayableSummary
                            {
                                Supplier = x.Supplier,
                                TotalPayments = x.Payments.Sum(y => y.Amount)
                            }).ToList();

            qry_payments = null;

            var qry_purchases = (from x in PurchaseOrder.Queryable
                      where x.IsCompleted
                      group x by x.Supplier into c
                      select new
                      {
                          Supplier = c.Key,
                          Purchases = c.ToList()
                      }).ToList();

            var purchases = from x in qry_purchases
                        select new AccountsPayableSummary
                        {
                            Supplier = x.Supplier,
                            TotalPurchases = x.Purchases.Sum(y => y.Total)
                        };

            qry_purchases = null;

            foreach (var item in purchases)
            {
                var temp = payments.SingleOrDefault(x => x.Supplier.Id == item.Supplier.Id);
                
                if (temp != null)
                {
                    item.TotalPayments = temp.TotalPayments;
                    payments.Remove(temp);
                }

                results.Add(item);
            }

            results.AddRange(payments);
            results = results.OrderBy(x => x.Supplier.Name).ToList();

            return View(results);
        }

        public ViewResult AccountStatement(int id)
        {
            Supplier item = Supplier.Find(id);
            var results = new List<AccountsPayableEntry>();
            var qry_purchases = (from x in PurchaseOrder.Queryable
                             where x.IsCompleted && x.Supplier.Id == item.Id
                             select x).ToList();
            var purchases = (from x in qry_purchases
                        select new AccountsPayableEntry
                        {
                            Number = x.Id,
                            Date = x.ModificationTime,
                            Amount = x.Total,
                            Type = DebitCreditEnum.Credit,
                            Description = string.Format(Resources.Format_PurchaseDescription, x.ModificationTime)
                        }).ToList();

            qry_purchases.Clear();
            qry_purchases = null;

            var qry_payments = (from x in SupplierPayment.Queryable
                                where x.Supplier.Id == item.Id
                                select x).ToList();

            var payments = (from x in qry_payments
                           select new AccountsPayableEntry
                           {
                               Number = x.Id,
                               Date = x.Date,
                               Amount = x.Amount,
                               Type = DebitCreditEnum.Debit,
                               Description = string.Format(string.IsNullOrEmpty(x.Reference) ? Resources.Format_PaymentDescription : Resources.Format_PaymentWithRefDescription, x.Method.GetDisplayName(), x.Reference)
                           }).ToList();

            qry_payments.Clear();
            qry_payments = null;

            results.AddRange(payments);
            results.AddRange(purchases);
            results = results.OrderBy(x => x.Date).ToList();

            var sum = 0m;
            foreach (var x in results)
            {
                sum += x.Type == DebitCreditEnum.Credit ? -x.Amount : x.Amount;
                x.Balance = sum;
            }

            return View(new MasterDetails<AccountsPayableSummary, AccountsPayableEntry>
            {
                Master = new AccountsPayableSummary {
                    Supplier = item,
                    TotalPurchases = purchases.Sum(x => x.Amount),
                    TotalPayments = payments.Sum(x => x.Amount)
                },
                Details = results
            });
        }
    }
}
