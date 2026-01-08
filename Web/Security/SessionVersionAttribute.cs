using System;
using System.Security.Claims;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Linq;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;

public class SessionVersionAttribute : AuthorizeAttribute {
	protected override bool AuthorizeCore (HttpContextBase httpContext)
	{
		var user = httpContext.User;
		if (user?.Identity?.IsAuthenticated != true)
			return false;

		var userId = user.Identity.Name; // o el claim que uses
						 //var tokenVersion = GetTokenVersionFromClaims (user); // método auxiliar
		var tokenVersion = httpContext.Request.Cookies ["SessionVersion"]?.Value;
		// Aquí consultas la BD
		var dbVersion = GetSessionVersionFromDb (userId);

		return tokenVersion == dbVersion.ToString();
	}

	private int GetTokenVersionFromClaims (IPrincipal user)
	{
		var claimsIdentity = user.Identity as ClaimsIdentity;
		var claim = claimsIdentity?.FindFirst ("SessionVersion");
		return claim != null ? int.Parse (claim.Value) : 0;
	}

	private int GetSessionVersionFromDb (string userId)
	{
		//using (var db = new AppDbContext ()) {
		//	var u = db.Users.Find (userId);
		//	return u?.SessionVersion ?? 0;
		//}
		return Mictlanix.BE.Model.User.Queryable.Where (u => u.UserName == userId).Select (u => u.SessionVersion).FirstOrDefault ();
	}
}
