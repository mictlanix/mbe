using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Models {
	public class SalesOrdersSummaryFilters:DateRange {
		[Display(Name ="NetD",ResourceType = typeof(Resources) )]
		public bool NetD { get; set; }
		[Display(Name ="Immediate",ResourceType = typeof(Resources) )]
		public bool Immediate { get; set; }

		public List<SalesOrder> Results { get; set; }

	}
}