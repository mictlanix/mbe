using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Business.Essentials.Model;

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
            return View();
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
                Price = p.Price1,
                Discount = 0,
                TaxRate = p.TaxRate,
                Quantity = 1,
            };

            detail.Create();

            result = Json(new
            {
                id = detail.Id,
                product = detail.Product.Id,
                name = detail.Product.Name,
                code = detail.Product.Code,
                sku = detail.Product.SKU,
                url = string.Format("/Photos/{0}", detail.Product.Photo),
                quantity = detail.Quantity,
                price = detail.Price,
                discount = detail.Discount,
                taxRate = detail.TaxRate
            });

            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            return result;
        }

        [HttpPost]
        public JsonResult EditDetailQuantity(int id, decimal quantity)
        {
            SalesOrderDetail detail = SalesOrderDetail.Find(id);

            if (!(quantity > 0))
            {
                return Json(new { id = id, quantity = detail.Quantity }, JsonRequestBehavior.AllowGet);
            }

            detail.Quantity = quantity;
            detail.Save();

            return Json(new { id = id, quantity = detail.Quantity }, JsonRequestBehavior.AllowGet);
        }
    }
}
