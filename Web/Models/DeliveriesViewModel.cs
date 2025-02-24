using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Models {

	public class DeliveriesOnDay {
		public string Title { get; set; }

		public DateTime Date { get; set; }
		public bool Selected { get; set; }
		public List<DeliveryOrderDetail> Details { get; set; }
	}
	public class DeliveriesViewModel {
		public List<DeliveriesOnDay> DeliveriesOnDay { get; set; }
	}
}