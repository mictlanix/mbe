using System;
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
    public class PaymentsController : Controller
    {
        public ActionResult Index()
        {
            if (GetDrawer() == null)
            {
                return View("InvalidCashDrawer");
            }

            if (GetSession() == null)
            {
                return RedirectToAction("OpenSession");
            }

            var qry = from x in SalesOrder.Queryable
                      where x.IsCompleted && !x.IsPaid && !x.IsCancelled
                      select x;

            return View(qry.ToList());
        }

        public ViewResult OpenSession()
        {
            if (GetDrawer() == null)
            {
                return View("InvalidCashDrawer");
            }

            return View();
        }

        [HttpPost]
        public ActionResult OpenSession(CashSession item)
        {
            item = new CashSession();
            item.CashDrawer = GetDrawer();

            if (item.CashDrawer == null)
            {
                return View("InvalidCashDrawer");
            }

            item.Start = DateTime.Now;
            item.Cashier = SecurityHelpers.GetUser(User.Identity.Name).Employee;
            
            return RedirectToAction("Index");
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
                CashSession = GetSession(),
                SalesOrder = SalesOrder.Find(order),
                Method = (PaymentMethod)type,
                Amount = amount,
                Date = DateTime.Now,
                Reference = reference
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

        CashDrawer GetDrawer()
        {
            var addr = Request.UserHostAddress;

            return CashDrawer.Queryable.SingleOrDefault(x => x.HostAddress == addr);
        }

        CashSession GetSession()
        {
            var addr = Request.UserHostAddress;
            return CashSession.Queryable
                              .Where(x => x.End == null)
                              .SingleOrDefault(x => x.CashDrawer.HostAddress == addr);
        }
    }
}
