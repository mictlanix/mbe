// 
// SecurityHelpers.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.org>
// 
// Copyright (C) 2011-2013 Eddy Zavaleta, Mictlanix, and contributors.
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using System.Security.Cryptography;
using System.Text;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Helpers
{
    public static class SecurityHelpers
    {
        public static User CurrentUser(this HtmlHelper helper)
        {
			User user = helper.ViewContext.HttpContext.Items["CurrentUser"] as User;
			
			if (user == null)
			{
				user = GetUser(helper, helper.ViewContext.HttpContext.User.Identity.Name);
				helper.ViewContext.HttpContext.Items["CurrentUser"] = user;
			}
			
            return user;
        }

        internal static User GetUser(this HtmlHelper helper, string username)
        {
            return GetUser(username);
        }

        internal static User GetUser(string username)
        {
            return User.TryFind(username);
        }

        public static AccessPrivilege GetPrivilege (this HtmlHelper helper, User user, SystemObjects obj)
		{
			return user.Privileges.SingleOrDefault (x => x.Object == obj) ?? new AccessPrivilege ();
		}

		internal static string SHA1 (string text)
		{
			byte[] bytes = Encoding.Default.GetBytes ("" + text);
			SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider ();

			return BitConverter.ToString (sha1.ComputeHash (bytes)).Replace ("-", "");
		}
		
		internal static byte[] DecodeBase64 (string data)
		{
			return System.Convert.FromBase64String (data);
		}

		internal static string EncodeBase64 (byte[] data)
		{
			return System.Convert.ToBase64String (data, Base64FormattingOptions.None);
		}

		internal static string EncodeBase64 (string data)
		{
			return EncodeBase64 (Encoding.UTF8.GetBytes (data));
		}
    }
}