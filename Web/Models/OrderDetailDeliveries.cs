using System;
using System.Collections.Generic;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Models {
	public class OrderDetailDeliveries {

		public int Id { get; set; }
		public int SalesOrderId { get; set; }
		public DateTime Date { get; set; }
		public string ProductName { get; set; }
		public decimal Quantity { get; set; }
		public decimal QuantityRemain { get; set; }
		public decimal QuantityDelivered { get; set; }
		public List<DeliveryOrderDetail> Details { get; set; }
		public string UnitOfMeasure { get; set; }

	}
}