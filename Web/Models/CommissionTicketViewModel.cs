using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Models {
	public class CommissionTicketViewModel {
		public IEnumerable<dynamic> Details { get; set; }
		public DateRange DateRange { get; set; }
		[Display (Name = "Salesperson", ResourceType = typeof (Resources))]
		public Employee CommissionAgent { get; set; }
		public Store Store { get; set; }
	}
}