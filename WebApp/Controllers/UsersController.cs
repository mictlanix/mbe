using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Business.Essentials.Model;

namespace Business.Essentials.WebApp.Controllers
{ 
    public class UsersController : Controller
    {
        //
        // GET: /Users/

        public ActionResult Index()
        {
            var qry = from x in Model.User.Queryable
                      select x;

            return View(qry.ToList());
        }

        //
        // GET: /Users/Details/5

        public ViewResult Details(string id)
        {
            User user = Model.User.Find(id);
            return View(user);
        }
        
        //
        // GET: /Users/Edit/5
 
        public ActionResult Edit(string id)
        {
            User user = Model.User.Find(id);
            return View(user);
        }

        //
        // POST: /Users/Edit/5

        [HttpPost]
        public ActionResult Edit(User item)
        {
            if (!ModelState.IsValid)
            {
                return View(item);
            }

            User user = Model.User.Find(item.UserName);

            user.Email = item.Email;
            user.IsAdministrator = item.IsAdministrator;

            foreach(var i in Enum.GetValues(typeof(SystemObjects)))
            {
                SystemObjects obj = (SystemObjects)i;
                string prefix = Enum.GetName(typeof(SystemObjects), i);
                AccessPrivilege privilege = user.Privileges.SingleOrDefault(x => x.Object == obj);

                if (privilege == null)
                {
                    privilege = new AccessPrivilege { User = user, Object = obj };
                }

                foreach (var j in Enum.GetValues(typeof(AccessRight)))
                {
                    AccessRight right = (AccessRight)j;
                    string name = prefix + Enum.GetName(typeof(AccessRight), j);
                    string value = Request.Params[name];

                    if (value == null)
                        continue;

                    if(value.Contains("true"))
                        privilege.Privileges |= right;
                    else
                        privilege.Privileges &= ~right;
                }

                privilege.Save();
            }

            user.Update();

            return RedirectToAction("Index");

        }

        //
        // GET: /Users/Delete/5
 
        public ActionResult Delete(string id)
        {
            User item = Model.User.Find(id);
            return View(item);
        }

        //
        // POST: /Users/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(string id)
        {
            User item = Model.User.Find(id);

            item.Delete();
            
            return RedirectToAction("Index");
        }
    }
}