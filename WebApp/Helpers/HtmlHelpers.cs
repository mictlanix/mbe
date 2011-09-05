using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using System.Data.Entity;
using System.Web.Routing;
using Business.Essentials.Model;

namespace Business.Essentials.WebApp.Helpers
{
    public static class HtmlHelpers
    {
        public static IList<Contact> GetContacts(this HtmlHelper html)
        {
            List<Contact> items = new List<Contact>();

            items.Add(new Contact
            {
                Id = 1,
                Name = "Contact 01"
            });

            items.Add(new Contact
            {
                Id = 2,
                Name = "Contact 02"
            });

            items.Add(new Contact
            {
                Id = 4,
                Name = "Contact 03"
            });

            items.Add(new Contact
            {
                Id = 5,
                Name = "Contact 04"
            });

            items.Add(new Contact
            {
                Id = 5,
                Name = "Contact 05"
            });

            return items;
        }

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
    }
}