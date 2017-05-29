using System;

namespace Mictlanix.BE.Web.Models {
	public class DeliveryItemDescription {

		public int Id { get; set; }
		public int SalesOrderId { get; set; }
		public decimal DeliveredQuantity { get; set; }
		public DateTime Date { get; set; }
		public string ProductName { get; set; }
		public decimal Quantity { get; set; }
		public decimal PendantQuantityToDeliver { get { return Quantity - DeliveredQuantity; } }
		public string UnitOfMeasure { get; set; }

	}
}