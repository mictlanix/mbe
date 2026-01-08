using System;
using System.ComponentModel.DataAnnotations;

namespace Mictlanix.BE.Model {
	public enum SourceType : int {
		[Display (Name = "DeliveryOrder", ResourceType = typeof (Resources))]
		DeliveryOrder = 1,
		[Display (Name = "CustomerPayment", ResourceType = typeof (Resources))]
		CustomerPayment = 2,
		[Display (Name = "SalesOrder", ResourceType = typeof (Resources))]
		SalesOrder = 3,
		[Display (Name = "PurchaseRequest", ResourceType = typeof (Resources))]
		PurchaseRequest = 4,
		[Display (Name = "PurchaseOrder", ResourceType = typeof (Resources))]
		PurchaseOrder = 5,
		[Display (Name = "Pricing", ResourceType = typeof (Resources))]
		Pricing = 6,
		[Display (Name = "Customer", ResourceType = typeof (Resources))]
		Customer = 7,
		[Display (Name = "UserSettings", ResourceType = typeof (Resources))]
		UserSettings = 8,
		[Display (Name = "Product", ResourceType = typeof (Resources))]
		Product = 9,

	}
}
