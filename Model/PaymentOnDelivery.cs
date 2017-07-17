using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;
using System;

namespace Mictlanix.BE.Model
{
	[ActiveRecord("payment_on_delivery")]
	public class PaymentOnDelivery : ActiveRecordLinqBase<PaymentOnDelivery>
	{
		[PrimaryKey(PrimaryKeyType.Identity, "payment_on_delivery_id")]
		public int Id { get; set; }

		[BelongsTo ("cash_session")]
		public virtual CashSession CashSession { get; set; }

		[BelongsTo ("customer_payment", Lazy = FetchWhen.OnInvoke)]
		public virtual CustomerPayment CustomerPayment { get; set; }

		[Property("paid")]
		[Display(Name = "Paid", ResourceType = typeof(Resources))]
		public bool IsPaid { get; set; }

		[Property]
		[DataType (DataType.DateTime)]
		[Display (Name = "Date", ResourceType = typeof (Resources))]
		public virtual DateTime Date { get; set; }

	}
}
