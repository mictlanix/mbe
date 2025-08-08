using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Models {
	public class DeliveryItineraryAddressViewModel {
		public DeliveriesItinerary Itinerary { get; set; }
		public DeliveryOrder DeliveryOrder { get; set; }
		
	}
}