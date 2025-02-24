using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Models {
	public class SalesOrderDetailViewModel {
		public SalesOrderDetail Detail { get; set; }
		public List<String> Errors { get; set; }
	}
}