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
            return View();
        }

        public ActionResult Create()
        {
            return View("Create");
        }

        [HttpPost]
        public JsonResult AddDetail(int order, int product)
        {
            JsonResult result = new JsonResult();
            var item = Product.Find(product);
            var detail = new
            {
                id = (int)DateTime.Now.Ticks,
                product = item.Id,
                name = item.Name,
                code = item.Code,
                sku = item.SKU,
                url = string.Format("/Photos/{0}", item.Photo),
                quantity = 1,
                price = item.Price1,
                discount = 0,
                taxRate = item.TaxRate,
            };

            result = Json(detail);
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            return result;
        }

        [HttpPost]
        public JsonResult EditDetailQuantity(int id, decimal quantity)
        {
            JsonResult result = new JsonResult();
            var detail = new
            {
                id = id,
                quantity = quantity
            };

            result = Json(detail);
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            return result;
        }
    }
}
