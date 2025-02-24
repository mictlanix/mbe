using System.ComponentModel.DataAnnotations;


namespace Mictlanix.BE.Web.Utils {
	public enum PurchaseRequestStatus {
		[Display (Name = "Edit", ResourceType = typeof (Resources))]
		OnEdition = 0,
		[Display (Name = "OnApproval", ResourceType = typeof (Resources))]
		OnApproval = 5,
		[Display (Name = "OnRequest", ResourceType = typeof (Resources))]
		OnRequest = 10,
		[Display (Name = "OnPurchase", ResourceType = typeof (Resources))]
		OnPurchase = 20,
		[Display (Name = "OnReception", ResourceType = typeof (Resources))]
		OnReception = 30,
		[Display (Name = "OnStock", ResourceType = typeof (Resources))]
		OnStock = 40,
		[Display (Name = "Cancelled", ResourceType = typeof (Resources))]
		Cancelled = 50
	}
}