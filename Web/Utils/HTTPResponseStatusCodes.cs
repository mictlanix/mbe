using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Mictlanix.BE.Web.Utils {
		public enum HTTPResponseStatusCodes {
		OK = 200,
		Created=201,
		Accepted=202,
		BadRequest = 400,
		Unauthorized = 401,
		Forbidden = 403,
		NotFound = 404,
		InternalServerError = 500,
		BadGateway = 502,
		}
}