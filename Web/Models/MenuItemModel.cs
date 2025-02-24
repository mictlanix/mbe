using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Models {
	public class MenuItemModel {
		public SystemObjects SystemObject { get; set; }
		public string Resources { get; set; }
		public string Controller { get; set; }
		public string DefaultAction { get; set; }
	}
}