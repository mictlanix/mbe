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
    public class PaymentsController : Controller
    {
        public ViewResult Index()
        {
            var qry = from x in SalesOrder.Queryable
                      where x.IsCompleted && !x.IsPaid && !x.IsCancelled
                      select x;

            return View(qry.ToList());
        }

        public ActionResult PayOrder(int id)
        {
            SalesOrder order = SalesOrder.Find(id);

            return View("PayOrder", order);
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

        [HttpPost]
        public ActionResult ConfirmPayment(int id)
        {
            SalesOrder item = SalesOrder.Find(id);

            item.IsPaid = true;
            item.Save();

            return RedirectToAction("Index");
        }
        
    }
}
