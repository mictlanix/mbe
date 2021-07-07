// 
// InventoryHelpers.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2013 Eddy Zavaleta, Mictlanix, and contributors.
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Helpers
{
	public static class InventoryHelpers
	{
		public static void ChangeNotification (TransactionType source, int reference, DateTime dt,
			Warehouse origin, Warehouse destination, Product product, decimal quantity)
		{
			if (!product.IsStockable)
				return;

			if (!(product.IsPerishable || product.IsSeriable)) {
				KardexRegister (source, reference, dt, origin, product, quantity);

				if (destination != null) {
					KardexRegister (source, reference, dt, destination, product, -quantity);
				}
			} else {
				LotSerialRegister (source, reference, origin, product, quantity);
			}
		}

		static void KardexRegister (TransactionType source, int reference, DateTime dt,
		                            Warehouse warehouse, Product product, decimal quantity)
		{
			var item = new LotSerialTracking {
				Source = source,
				Reference = reference,
				Date = DateTime.Now,
				Warehouse = warehouse,
				Product = product,
				Quantity = quantity
			};

			item.Create ();
		}
		
		static void LotSerialRegister (TransactionType source, int reference, Warehouse warehouse,
		                               Product product, decimal quantity)
		{
			var query = from x in LotSerialRequirement.Queryable
						where x.Source == source &&
							x.Reference == reference &&
			            	x.Warehouse.Id == warehouse.Id &&
							x.Product == product
						select x;
			var rqmt = query.SingleOrDefault ();

			if (rqmt != null) {
				rqmt.Quantity += quantity;
				rqmt.Update ();
			} else {
				rqmt = new LotSerialRequirement {
					Source = source,
					Reference = reference,
					Warehouse = warehouse,
					Product = product,
					Quantity = quantity
				};
				
				rqmt.Create ();
			}
		}

		public static string GetLotCode (PurchaseOrder order) {
			return order.Supplier.Code + order.CreationTime.ToString ("ddMMyy") + order.Id;
		}
	}
}
