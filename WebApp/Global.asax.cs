using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Business.Essentials.Model;
using Business.Essentials.WebApp.Helpers;

namespace Business.Essentials.WebApp
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
			
            routes.MapRoute(
                "Barcodes", // Route name
                "Barcodes/{action}/{id}.jpg", // URL with parameters
                new {  controller = "Barcodes", action = "Index", id = UrlParameter.Optional } // Parameter defaults
            );
			
            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new {  controller = "Home", action = "Index", id = UrlParameter.Optional } // Parameter defaults
            );

        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);

#if DEBUG
            log4net.Config.XmlConfigurator.Configure();
#endif

            IConfigurationSource source = ConfigurationManager.GetSection("activeRecord") as IConfigurationSource;
            ActiveRecordStarter.Initialize(typeof(Category).Assembly, source);
        }
		
		/*
        protected void Application_BeginRequest(object sender, EventArgs e)
        {
			if(HttpContext.Current.User != null)
			{
				HttpContext.Current.Items.Add("CurrentUser", SecurityHelpers.GetUser(HttpContext.Current.User.Identity.Name));
			}
        }
		*/
		
		protected void Application_EndRequest(Object sender, EventArgs e)
		{
			HttpContext.Current.Items.Remove("CurrentUser");
		}
    }
}