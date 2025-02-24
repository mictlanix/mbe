using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Models {

	public class DeliveryDetail {

		public SalesOrderDetail SalesOrderDetail { get; set; }
		public DateTime? DeliveryDate { get; set; }
		public decimal Quantity { get; set; }
		public string ShipTo { get; set; }
	}
}