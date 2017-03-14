using System.Web;
using System.Web.Optimization;

namespace Mictlanix.BE.Web {
	public class BundleConfig {
		public static void RegisterBundles (BundleCollection bundles)
		{
			bundles.Add (new ScriptBundle ("~/Scripts/jquery").Include (
				"~/Scripts/jquery-{version}.js",
				"~/Scripts/jquery.unobtrusive*",
				"~/Scripts/jquery.validate*",
				"~/Scripts/jquery.tokeninput.js"
			));

			bundles.Add (new ScriptBundle ("~/Scripts/select2").Include (
				"~/Scripts/select2.min.js",
				"~/Scripts/select2_locale_es.js"
			));

			bundles.Add (new ScriptBundle ("~/Scripts/bootstrap").Include (
				"~/Scripts/bootstrap.js",
				"~/Scripts/bootstrap-datepicker.js",
				"~/Scripts/locales/bootstrap-datepicker.es.min.js",
				"~/Scripts/bootstrap-editable.js",
				"~/Scripts/bootstrap-clockpicker.min.js"
			));

			bundles.Add (new ScriptBundle ("~/Scripts/app").Include (
				"~/Scripts/helper.formatters.js",
				"~/Scripts/helper.functions.js"
			));

			bundles.Add (new ScriptBundle ("~/Scripts/jquery-alone").Include (
				"~/Scripts/jquery-{version}.js"
			));

			bundles.Add (new StyleBundle ("~/Content/select2").Include (
				"~/Content/select2.css"));

			bundles.Add (new StyleBundle ("~/Content/bootstrap").Include (
				"~/Content/bootstrap.css",
				"~/Content/bootstrap-datepicker3.css",
				"~/Content/bootstrap-editable.css",
				"~/Content/select2-bootstrap.css",
				"~/Content/bootstrap-clockpicker.min.css"
			));

			bundles.Add (new StyleBundle ("~/Content/main").Include (
				"~/Content/token-input.css",
				"~/Content/menu.css",
				"~/Content/main.css",
				"~/Content/buttons.css"
			));

			bundles.Add (new StyleBundle ("~/Content/print").Include (
				"~/Content/bootstrap.css",
				"~/Content/print.css"
			));

			bundles.Add (new StyleBundle ("~/Content/ticket").Include (
				"~/Content/bootstrap.css",
				"~/Content/ticket.css"
			));
		}
	}
}