using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
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
            var point_sale = PointSale.Find(1); //FIXME use settings

            item.Customer = customer;
            item.SalesPerson = salesperson;
            item.PointOfSale = point_sale;
            item.Date = DateTime.Now;
            item.DueDate = item.IsCredit ? item.Date.AddDays(customer.CreditDays) : item.Date;

            item.Create();

            if(Request.IsAjaxRequest())
            {
                return PartialView("_SalesInfo", item);
            }

            return RedirectToAction("Index");
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
            JsonResult result;
            var p = Product.Find(product);

            var detail = new SalesOrderDetail
            {
                SalesOrder = SalesOrder.Find(order),
                Product = p,
                ProductCode = p.Code,
                ProductName = p.Name,
                Discount = 0,
                TaxRate = p.TaxRate,
                Quantity = 1,
            };

            switch (detail.SalesOrder.Customer.PriceList.Id)
            {
                case 1:
                    detail.Price = p.Price1;
                    break;
                case 2:
                    detail.Price = p.Price2;
                    break;
                case 3:
                    detail.Price = p.Price3;
                    break;
                case 4:
                    detail.Price = p.Price4;
                    break;
            }

            detail.Create();

            result = Json(new
            {
                id = detail.Id,
                //product = detail.Product.Id,
                //name = detail.Product.Name,
                //code = detail.Product.Code,
                //sku = detail.Product.SKU,
                //url = string.Format("/Photos/{0}", detail.Product.Photo),
                //quantity = detail.Quantity,
                //price = detail.Price,
                //discount = detail.Discount,
                //taxRate = detail.TaxRate
            });

            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            return result;
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

    }
}
