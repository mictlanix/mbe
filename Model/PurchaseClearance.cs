// 
// InventoryReceipt.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2014 Eddy Zavaleta, Mictlanix, and contributors.
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
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Mictlanix.BE.Model {
	[ActiveRecord ("purchase_clearance")]
	public class PurchaseClearance : ActiveRecordLinqBase<PurchaseClearance> {
		IList<PurchaseClearanceDetail> details = new List<PurchaseClearanceDetail> ();

        [PrimaryKey (PrimaryKeyType.Identity, "purchase_clearance_id")]
		[Display (Name = "PurchaseClearanceId", ResourceType = typeof (Resources))]
		[DisplayFormat (DataFormatString = "{0:D8}")]
		public int Id { get; set; }

		//[BelongsTo ("purchase_order", NotNull = true, Fetch = FetchEnum.Join)]
		[Property ("purchase_order")]
		[Display (Name = "PurchaseOrder", ResourceType = typeof (Resources))]
		public int PurchaseOrder { get; set; }

		[Property ("creation_time")]
		[DataType (DataType.DateTime)]
		[Display (Name = "CreationTime", ResourceType = typeof (Resources))]
		public DateTime CreationTime { get; set; }

		[Property ("modification_time")]
		[DataType (DataType.DateTime)]
		[Display (Name = "ModificationTime", ResourceType = typeof (Resources))]
		public DateTime ModificationTime { get; set; }

		[BelongsTo ("creator")]
		[Display (Name = "Creator", ResourceType = typeof (Resources))]
		public virtual Employee Creator { get; set; }

		[BelongsTo ("updater")]
		[Display (Name = "Updater", ResourceType = typeof (Resources))]
		public virtual Employee Updater { get; set; }

		[Property]
		[DataType (DataType.MultilineText)]
		[Display (Name = "Comment", ResourceType = typeof (Resources))]
		[StringLength (500, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string Comment { get; set; }

		[Property ("commission")]
		[DisplayFormat (DataFormatString = "{0:p}")]
		[Display (Name = "Commision", ResourceType = typeof (Resources))]
		public decimal Commission { get; set; }
		
		[Property ("supplier")]
		[Display (Name = "Supplier", ResourceType = typeof (Resources))]
		public string Supplier { get; set; }

		[Property ("lot_code")]
		[Display (Name = "Supplier", ResourceType = typeof (Resources))]
		public string LotCode { get; set; }

		[Property ("completed")]
		[Display (Name = "Completed", ResourceType = typeof (Resources))]
		public bool IsCompleted { get; set; }

		[Property ("cancelled")]
		[Display (Name = "Cancelled", ResourceType = typeof (Resources))]
		public bool IsCancelled { get; set; }

		[HasMany (typeof (PurchaseClearanceDetail), Table = "purchase_clearance_detail", ColumnKey = "purchase_clearance")]
		public IList<PurchaseClearanceDetail> Details {
			get { return details; }
			set { details = value; }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Total", ResourceType = typeof (Resources))]
		public decimal TotalSales {
			get { return Details.Where(x => x.IsCharge).Sum (x => x.TotalSold); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "TotalCommission", ResourceType = typeof (Resources))]
		public decimal TotalCommision {
			get { return TotalSales * Commission; }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Total", ResourceType = typeof (Resources))]
		public decimal TotalExpenses {
			get { return Details.Where(x => !x.IsCharge).Sum (x => x.TotalSold); }
		}

		[DataType (DataType.Currency)]
		[Display (Name = "Total", ResourceType = typeof (Resources))]
		public decimal Total {
			get { return TotalSales - TotalExpenses - TotalCommision; }
		}

		#region Override Base Methods

		public override string ToString ()
		{
			return string.Format ("{0:D8}", Id);
		}

		public override bool Equals (object obj)
		{
			var other = obj as DeliveryOrder;

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
