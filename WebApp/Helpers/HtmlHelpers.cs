using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using Business.Essentials.Model;

namespace Business.Essentials.WebApp.Helpers
{
    public static class HtmlHelpers
    {
        public static MvcHtmlString ActionImage(this HtmlHelper html,
                                                string action,
                                                object routeValues,
                                                string imagePath,
                                                string alt)
        {
            return ActionImage(html, action, routeValues, imagePath, alt, null);
        }

        public static MvcHtmlString ActionImage(this HtmlHelper html,
                                                string action,
                                                object routeValues,
                                                string imagePath,
                                                string alt,
                                                object htmlAttributes)
        {
            var url = new UrlHelper(html.ViewContext.RequestContext);

            // build the <img> tag
            var imgBuilder = new TagBuilder("img");

            imgBuilder.MergeAttribute("src", url.Content(imagePath));
            imgBuilder.MergeAttribute("alt", alt);

            if (htmlAttributes != null)
            {
                imgBuilder.MergeAttributes(new RouteValueDictionary(htmlAttributes));
            }

            string imgHtml = imgBuilder.ToString(TagRenderMode.SelfClosing);

            // build the <a> tag
            var anchorBuilder = new TagBuilder("a");
            anchorBuilder.MergeAttribute("href", url.Action(action, routeValues));
            anchorBuilder.InnerHtml = imgHtml; // include the <img> tag inside
            string anchorHtml = anchorBuilder.ToString(TagRenderMode.Normal);

            return MvcHtmlString.Create(anchorHtml);
        }

        public static string GetMenuClass(this HtmlHelper html, string controller)
        {
            string ctl = html.ViewContext.Controller.ValueProvider.GetValue("controller").RawValue.ToString();
            return ctl == controller ? "gbz0l" : string.Empty;
        }

        public static string GetMenuClass(this HtmlHelper html, string controller, string action)
        {
            string ctl = html.ViewContext.Controller.ValueProvider.GetValue("controller").RawValue.ToString();
            string atn = html.ViewContext.Controller.ValueProvider.GetValue("action").RawValue.ToString();
            return ctl == controller && atn == action ? "gbz0l" : string.Empty;
        }

        public static string GetDisplayName(this Enum member)
        {
            string display_name = Enum.GetName(member.GetType(), member);

            var prop_info = member.GetType().GetField(display_name);
            var attrs = prop_info.GetCustomAttributes(typeof(DisplayAttribute), false);

            if (attrs.Count() != 0)
                display_name = ((DisplayAttribute)attrs[0]).GetName();

            return display_name;
        }

        public static User CurrentUser(this HtmlHelper helper)
        {
            return GetUser(helper, helper.ViewContext.HttpContext.User.Identity.Name);
        }

        public static User GetUser(this HtmlHelper helper, string username)
        {
            return User.Queryable.SingleOrDefault(x => x.UserName == username);
        }
    }
}