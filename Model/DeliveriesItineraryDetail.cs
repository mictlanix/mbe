using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Model {
	[ActiveRecord ("deliveries_itinerary_detail")]
	public class DeliveriesItineraryDetail : ActiveRecordLinqBase<DeliveriesItineraryDetail> {
		[PrimaryKey (PrimaryKeyType.Identity, "deliveries_itinerary_detail_id")]
		public int Id { get; set; }

		[BelongsTo ("deliveries_itinerary", Lazy = FetchWhen.OnInvoke)]
		[Display (Name = "DeliveriesItinerary", ResourceType = typeof (Resources))]
		public virtual DeliveriesItinerary DeliveriesItinerary { get; set; }

		[BelongsTo ("delivery_order_detail", Lazy = FetchWhen.OnInvoke)]
		[Display (Name = "DeliveryOrderDetail", ResourceType = typeof (Resources))]
		public virtual DeliveryOrderDetail DeliveryOrderDetail { get; set; }

		[Property("quantity")]
		[DisplayFormat (DataFormatString = "{0:0.####}")]
		[Display (Name = "Quantity", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof (Resources))]
		public decimal SentQuantity { get; set; }

		[Property]
		[DataType (DataType.MultilineText)]
		[Display (Name = "Comment", ResourceType = typeof (Resources))]
		[StringLength (500, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string Comment { get; set; }

		//[Property]
		//[DisplayFormat (DataFormatString = "{0:0.####}")]
		//[Display (Name = "Quantity", ResourceType = typeof (Resources))]
		//[Required (ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof (Resources))]
		//public decimal RefundedQuantity { get; set; }

		//[Property ("product_code")]
		//[Display (Name = "ProductCode", ResourceType = typeof (Resources))]
		//[StringLength (25, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		//public virtual string ProductCode { get; set; }

		//[Property ("product_name")]
		//[Display (Name = "ProductName", ResourceType = typeof (Resources))]
		//[StringLength (250, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		//public virtual string ProductName { get; set; }


		#region Override Base Methods

		public override string ToString ()
		{
			return string.Format ("{0} [{1}, {2}]", Id, DeliveriesItinerary, SentQuantity);
		}

		public override bool Equals (object obj)
		{
			var other = obj as DeliveryOrderDetail;

			if (other == null)
				return false;

			if (Id == 0 && other.Id == 0)
				return (object) this == other;
			else
				return Id == other.Id;
		}

		public override int GetHashCode ()
		{
			if (Id == 0)
				return base.GetHashCode ();

			return string.Format ("{0}#{1}", GetType ().FullName, Id).GetHashCode ();
		}

		#endregion
	}
}
