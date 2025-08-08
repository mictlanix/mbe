
using System;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;
using NHibernate.Mapping;

namespace Mictlanix.BE.Model {
	[ActiveRecord ("credit_note")]
	[Serializable]
	public class CreditNote : ActiveRecordLinqBase<CreditNote> {

		[PrimaryKey (PrimaryKeyType.Identity, "credit_note_id")]
		[DisplayFormat (DataFormatString = "{0:D8}")]
		public int Id { get; set; }

		[BelongsTo ("sales_order", Lazy = FetchWhen.OnInvoke)]
		[Display (Name = "SalesOrder", ResourceType = typeof (Resources))]
		public virtual SalesOrder SalesOrder { get; set; }


		[BelongsTo ("customer_refund", Lazy = FetchWhen.OnInvoke)]
		[Display (Name = "CustomerRefund", ResourceType = typeof (Resources))]
		public virtual CustomerRefund CustomerRefund { get; set; }

		[BelongsTo ("customer_payment", Lazy = FetchWhen.OnInvoke)]
		public virtual CustomerPayment CustomerPayment { get; set; }

		[BelongsTo ("customer")]
		[Display (Name = "Customer", ResourceType = typeof (Resources))]
		public virtual Customer Customer { get; set; }

		[Property]
		[DataType (DataType.Currency)]
		[Range (0.0001, double.MaxValue, ErrorMessageResourceName = "Validation_CannotBeZeroOrNegative",
			ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "Refunded", ResourceType = typeof (Resources))]
		//public decimal Refunded { get { return IsRefundedToCustomer ? Refunded : 0; } set { Refunded = value; } }
		public decimal Refunded { get; set; }


		[BelongsTo ("cash_session")]
		[Display (Name = "CashSession", ResourceType = typeof (Resources))]
		public virtual CashSession CashSession { get; set; }

		[Property ("date")]
		[DataType (DataType.DateTime)]
		[Display (Name = "Date", ResourceType = typeof (Resources))]
		public virtual DateTime Date { get; set; }

		public bool IsRefundedToCustomer { get { return this.CashSession != null; } }


		#region Override Base Methods

		public override string ToString ()
		{
			return string.Format ("{1:c} {0} ({2:yyyy-MM-dd})", Id, Refunded, Date);
		}

		public override bool Equals (object obj)
		{
			CustomerPayment other = obj as CustomerPayment;

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

		public virtual CreditNote GetSerializable () {
			return new CreditNote {
				SalesOrder = SalesOrder,
				Refunded = Refunded,
				CustomerPayment = CustomerPayment,
				Customer = Customer
			};
		}

		#endregion
	}
}
