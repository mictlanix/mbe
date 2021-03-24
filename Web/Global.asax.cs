using System;
using System.Configuration;
using System.Linq;
using System.Globalization;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Security;
using System.Web.Optimization;

namespace Mictlanix.BE.Web
{
    public class MvcApplication : System.Web.HttpApplication
	{
		static string [] IGNORE_PATHS = { "/Content", "/Scripts", "/fonts", "/favicon.ico", "/Account/LogOff" };

		public static void RegisterGlobalFilters (GlobalFilterCollection filters)
		{
			filters.Add (new HandleErrorAttribute ());
		}

		public static void RegisterRoutes (RouteCollection routes)
		{
			routes.IgnoreRoute ("{resource}.axd/{*pathInfo}");

			routes.MapMvcAttributeRoutes ();

			routes.MapRoute (
				"Barcodes",
				"Barcodes/{action}/{id}.png",
				new { controller = "Barcodes", action = "Code128" }
			);

			routes.MapRoute (
				"Products_AddLabel",
				"Products/{id}/AddLabel/{value}",
				new { controller = "Products", action = "AddLabel", id = @"\d+", value = @"\d+" }
			);
			
			routes.MapRoute (
				"Products_RemoveLabel",
				"Products/{id}/RemoveLabel/{value}",
				new { controller = "Products", action = "AddLabel", id = @"\d+", value = @"\d+" }
			);
			
			routes.MapRoute (
				"Pricing_EditPrice",
				"Pricing/{product}/EditPrice/{list}/{value}",
				new { controller = "Pricing", action = "EditPrice", product = @"\d+", list = @"\d+", value = UrlParameter.Optional }
			);

			routes.MapRoute (
				"Inventory_AssignLotSerialNumbers",
				"Inventory/AssignLotSerialNumbers/{source}/{reference}",
				new { controller = "Inventory", action = "AssignLotSerialNumbers", source = @"\d+", reference = @"\d+" }
			);

			routes.MapRoute (
				"Inventory_SetWarehouse",
				"Inventory/SetWarehouse/{source}/{reference}",
				new { controller = "Inventory", action = "SetWarehouse", source = @"\d+", reference = @"\d+" }
			);

			routes.MapRoute (
				"Default", // Route name
				"{controller}/{action}/{id}", // URL with parameters
				new { controller = "Home", action = "Index", id = UrlParameter.Optional } // Parameter defaults
			);
		}

		protected void Application_Start ()
		{
			// AreaRegistration.RegisterAllAreas ();

			RegisterGlobalFilters (GlobalFilters.Filters);
			RegisterRoutes (RouteTable.Routes);
			BundleConfig.RegisterBundles (BundleTable.Bundles);

#if DEBUG
			log4net.Config.XmlConfigurator.Configure ();
#endif

			IConfigurationSource source = ConfigurationManager.GetSection ("activeRecord") as IConfigurationSource;
			ActiveRecordStarter.Initialize (typeof(Product).Assembly, source);

			var culture = Thread.CurrentThread.CurrentCulture.Clone () as CultureInfo;

			if (culture != null) {
				culture.DateTimeFormat.ShortDatePattern = Resources.DateFormatString;
			}

			CultureInfo.DefaultThreadCurrentCulture = culture;
			CultureInfo.DefaultThreadCurrentUICulture = culture;
		}

		protected void Application_BeginRequest (object sender, EventArgs e)
		{
			HttpContext.Current.Items.Add ("ar.sessionscope", new SessionScope (FlushAction.Never));
		}

		protected void Application_EndRequest (Object sender, EventArgs e)
		{
			if (HttpContext.Current == null)
				return;

			try {
				SessionScope scope = HttpContext.Current.Items ["ar.sessionscope"] as SessionScope;

				if (scope != null) {
					scope.Dispose ();
				}
			} catch (Exception ex) {
				HttpContext.Current.Trace.Warn ("Error", "EndRequest: " + ex.Message, ex);
			}
			
			HttpContext.Current.Items.Remove ("CurrentUser");
		}

		protected void Application_PostAuthenticateRequest (Object sender, EventArgs e)
		{
			var cookie = Request.Cookies [FormsAuthentication.FormsCookieName];

			if (cookie == null)
				return;

			var ticket = FormsAuthentication.Decrypt (cookie.Value);

			if (ticket == null || ticket.Expired) {
				return;
			}

			if (IGNORE_PATHS.Any (Request.Path.StartsWith)) {
				return;
			}

			var account = Model.User.TryFind (HttpContext.Current.User.Identity.Name);

			if (account == null) {
				Response.RedirectToRoute ("Default", new { controller = "Account", action = "LogOff" });
			} else {
				Principal = new CustomPrincipal (account.UserName, account.Email, account.IsAdministrator,
					account.Employee, account.Privileges.ToList ());
			}
		}

		private CustomPrincipal Principal {
			set {
				Thread.CurrentPrincipal = value;
				HttpContext.Current.User = value;
			}
		}
	}
}