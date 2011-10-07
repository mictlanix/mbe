using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Castle.ActiveRecord;
using Business.Essentials.Model;
using Business.Essentials.WebApp.Models;

namespace Business.Essentials.WebApp.Controllers
{
    public class SalesController : Controller
    {
        //
        // GET: /Sales/

        public ViewResult Index()
        {
            var qry = from x in SalesOrder.Queryable
                      where !x.IsCancelled && !x.IsCompleted
                      select x;

            return View(qry.ToList());
        }

        // GET: /Sales/Details/

        public ViewResult Details(int id)
        {
            SalesOrder order = SalesOrder.Find(id);

            return View(order);
        }

        //
        // GET: /Sales/New

        public ViewResult New()
        {
            return View(new SalesOrder());
        }

        [HttpPost]
        public ActionResult New(SalesOrder item)
        {
            var customer = Customer.Find(item.CustomerId);
            var salesperson = Employee.Find(1); //FIXME use user logged on
            var point_sale = PointOfSale.Find(1); //FIXME use settings

            var addr = Request.UserHostAddress;

            item.Customer = customer;
            item.SalesPerson = salesperson;
            item.PointOfSale = point_sale;
            item.Date = DateTime.Now;
            item.DueDate = item.IsCredit ? item.Date.AddDays(customer.CreditDays) : item.Date;

            using (var session = new SessionScope())
            {
                item.CreateAndFlush();
            }

            while (item.Id == 0)
            {
                System.Diagnostics.Debug.WriteLine("New Sales Id: {0}", item.Id);
                System.Threading.Thread.Sleep(10);
            }

            return RedirectToAction("Edit", new { id = item.Id });
        }

        public ActionResult Edit(int id)
        {
            SalesOrder item = SalesOrder.Find(id);

            if (Request.IsAjaxRequest())
                return PartialView("_Edit", item);
            else
                return View(item);
        }

        //
        // POST: /Customer/Edit/5

        [HttpPost]
        public ActionResult Edit(SalesOrder item)
        {
            var order = SalesOrder.Find(item.Id);
            var customer = Customer.Find(item.CustomerId);

            order.Customer = customer;
            order.IsCredit = item.IsCredit;
            order.DueDate = item.IsCredit ? order.Date.AddDays(customer.CreditDays) : order.Date;

            order.Save();

            return PartialView("_SalesInfo", order);
        }

        [HttpPost]
        public JsonResult AddDetail(int order, int product)
        {
            var p = Product.Find(product);

            var item = new SalesOrderDetail
            {
                SalesOrder = SalesOrder.Find(order),
                Product = p,
                ProductCode = p.Code,
                ProductName = p.Name,
                Discount = 0,
                TaxRate = p.TaxRate,
                Quantity = 1,
            };

            switch (item.SalesOrder.Customer.PriceList.Id)
            {
                case 1:
                    item.Price = p.Price1;
                    break;
                case 2:
                    item.Price = p.Price2;
                    break;
                case 3:
                    item.Price = p.Price3;
                    break;
                case 4:
                    item.Price = p.Price4;
                    break;
            }

            using (var session = new SessionScope())
            {
                item.CreateAndFlush();
            }

            while (item.Id == 0)
            {
                System.Diagnostics.Debug.WriteLine("New Detail Id: {0}", item.Id);
                System.Threading.Thread.Sleep(10);
                item.Refresh();
            }

            return Json(new { id = item.Id });
        }

        [HttpPost]
        public JsonResult EditDetailQuantity(int id, decimal quantity)
        {
            SalesOrderDetail detail = SalesOrderDetail.Find(id);

            if (quantity > 0)
            {
                detail.Quantity = quantity;
                detail.Save();
            }

            return Json(new { id = id, quantity = detail.Quantity, total = detail.Total.ToString("c") });
        }

        [HttpPost]
        public JsonResult EditDetailDiscount(int id, string value)
        {
            SalesOrderDetail detail = SalesOrderDetail.Find(id);
            bool success;
            decimal discount;

            success = decimal.TryParse(value.TrimEnd(new char[] { ' ', '%' }), out discount);
            discount /= 100m;

            if (success && discount >= 0 && discount <= 1)
            {
                detail.Discount = discount;
                detail.Save();
            }

            return Json(new { id = id, discount = detail.Discount.ToString("p"), total = detail.Total.ToString("c") });
        }

        //GET/SalesTotals

        public ActionResult GetSalesTotals(int id)
        {
            //var qry = from x in SalesOrder.Queryable
            //          where id == x.Id;
            //          select new { Total = x.Details.Sum(y => y.Quantity * y.Price), 
            //                       Taxes = x.Details.Sum(y => y.Quantity * y.Price / (1 + y.TaxRate)) };

            var order = SalesOrder.Find(id);

            return PartialView("_SalesTotals", order);
        }

        //GET/SalesTotals/{id}

        public ActionResult GetSalesItem(int id)
        {
            return PartialView("_SalesItem", SalesOrderDetail.Find(id));
        }

        [HttpPost]
        public JsonResult RemoveDetail(int id)
        {
            SalesOrderDetail item = SalesOrderDetail.Find(id);
            item.Delete();
            return Json(new { id = id, result = true });
        }

        public ActionResult PayOrders()
        {
            var qry = from x in SalesOrder.Queryable
                      where x.IsCompleted && !x.IsCancelled
                      select x;

            return View(qry.ToList());
        }

        [HttpPost]
        public ActionResult ConfirmOrder(int id)
        {
            SalesOrder item = SalesOrder.Find(id);

            item.IsCompleted = true;
            item.Save();

            return RedirectToAction("New");
        }

        public ActionResult PayOrder(int id)
        {
            SalesOrder order = SalesOrder.Find(id);

            return View("PayOrder", order);
        }

        [HttpPost]
        public ActionResult CancelOrder(int id)
        {
            SalesOrder item = SalesOrder.Find(id);

            item.IsCancelled = true;
            item.Save();

            return RedirectToAction("New");
        }

        public ActionResult GetSalesOrderBalance(int id)
        {
            var order = SalesOrder.Find(id);

            return PartialView("_SalesOrderBalance", order);
        }

        [HttpPost]
        public JsonResult AddPayment(int order, int type, decimal amount, string reference)
        {
            var item = new CustomerPayment
            {
                SalesOrder = SalesOrder.Find(order),
                Method = (PaymentMethod)type,
                Amount = amount,
                Date = DateTime.Now,
                Reference = reference,
            };

            using (var session = new SessionScope())
            {
                item.CreateAndFlush();
            }

            while (item.Id == 0)
            {
                System.Diagnostics.Debug.WriteLine("New Detail Id: {0}", item.Id);
                System.Threading.Thread.Sleep(10);
                item.Refresh();
            }

            return Json(new { id = item.Id });
        }

        public ActionResult GetPayment(int id)
        {
            return PartialView("_Payment", CustomerPayment.Find(id));
        }

        [HttpPost]
        public JsonResult RemovePayment(int id)
        {
            CustomerPayment item = CustomerPayment.Find(id);
            item.Delete();
            return Json(new { id = id, result = true });
        }

        public JsonResult GetBalance(int id)
        {
            SalesOrder order = SalesOrder.Find(id);

            return Json(new { balance = order.Balance }, JsonRequestBehavior.AllowGet);
        }
        

    }
}
