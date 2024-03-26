using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Models {
	public class DeliveryViewModel {
		public DeliveryOrder DeliveryOrder { get; set; }
		public List<SalesOrder> SalesOrders { get; set; }

		public List<SalesOrderPayment> PaymentsOnDelivery { get; set; }
		
	}
}