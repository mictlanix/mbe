using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Models {
	public class PurchaseClearanceSummary {

		public PurchaseClearance PurchaseClearance;
		public List<PurchaseClearanceDetailSummary> DetailSummaries;
		public PurchaseClearanceDetail ClearanceExpenses;
		public PurchaseOrder PurchaseOrder;
		public decimal ChargePercent;
		public PurchaseClearanceSummary (int id) {
			PurchaseClearance = PurchaseClearance.Find(id);
			PurchaseOrder = PurchaseOrder.Find(PurchaseClearance.PurchaseOrder);
			ClearanceExpenses = PurchaseClearance.Details.Last();
			DetailSummaries = new List<PurchaseClearanceDetailSummary> ();
			foreach (var detail in PurchaseClearance.Details) {
				DetailSummaries.Add (
					new PurchaseClearanceDetailSummary(detail.Id)
					);
			}

			ChargePercent = 0.1m;
		}

		[Display (Name = "Id", ResourceType = typeof (Resources))]
		public int Id { get { return PurchaseClearance.Id; } }

		[DataType (DataType.Currency)]
		public decimal TotalSales { get { return (decimal?) DetailSummaries.Sum (x => x.Total) ?? 0; } }

		[DataType (DataType.Currency)]
		public decimal TotalExpenses { get { return (decimal?) ClearanceExpenses.Details.Sum (x => x.Price) ?? 0; } }

		//[DataType (DataType.Currency)]
		//public decimal SubTotal { get { return TotalSales - TotalExpenses; } }

		[DataType (DataType.Currency)]
		public decimal Commision { get { return TotalSales * ChargePercent; } }

		[DataType (DataType.Currency)]
		public decimal Total { get { return TotalSales - Commision - TotalExpenses; } }
	}

	public class PurchaseClearanceDetailSummary {

		public List<PurchaseClearanceDetailEntry> Details;
		public PurchaseClearanceDetail PurchaseClearanceDetail;
		public PurchaseClearanceDetailSummary (int purchase_clearance_detail) {
			PurchaseClearanceDetail = PurchaseClearanceDetail.Find(purchase_clearance_detail);
			Details = PurchaseClearanceDetail.Details.ToList();
		}
		[Display (Name = "Id", ResourceType = typeof (Resources))]
		public int Id { get { return PurchaseClearanceDetail.Id; } }

		[DataType (DataType.Currency)]
		[Display (Name = "Total", ResourceType = typeof (Resources))]
		public decimal Total { get { return Details.Sum (x => (decimal?) (x.Price * x.Quantity)) ?? 0; } }

		[DataType (DataType.Currency)]
		[Display (Name = "AveragePrice", ResourceType = typeof (Resources))]
		public decimal AveragePrice { get { return PurchaseClearanceDetail.Quantity > 0 ? (decimal?) Total / PurchaseClearanceDetail.Quantity ?? 0: 0 ; } }

		[DataType (DataType.Currency)]
		[DisplayFormat (DataFormatString = "{0:0.##}")]
		[Display (Name = "PurchasedQuantity", ResourceType = typeof (Resources))]
		public decimal PurchasedQuantity { get { return PurchaseClearanceDetail.Quantity; } }

		[DataType (DataType.Currency)]
		[DisplayFormat (DataFormatString = "{0:0.##}")]
		[Display (Name = "SoldQuantity", ResourceType = typeof (Resources))]
		public decimal SoldQuantity { get { return Details.Sum (x => x.Quantity); } }

		[DataType (DataType.Currency)]
		[DisplayFormat (DataFormatString = "{0:0.##}")]
		[Display (Name = "RemainingQuantity", ResourceType = typeof (Resources))]
		public decimal RemainingQuantity { get { return PurchasedQuantity - Details.Sum (x => x.Quantity); } }

		[DataType (DataType.Currency)]
		[DisplayFormat (DataFormatString = "{0:0.##}")]
		[Display (Name = "DecreasedProduct", ResourceType = typeof (Resources))]
		public decimal DecreasedProduct { get { return Details.Where(x => !(x.Price > 0)).Sum (x => x.Quantity); } }

	}
}