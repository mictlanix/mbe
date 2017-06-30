using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Mictlanix.BE.Model
{
	[ActiveRecord("payment_method_charge")]
	public class PaymentMethodCharge : ActiveRecordLinqBase<PaymentMethodCharge>
	{
		[PrimaryKey(PrimaryKeyType.Identity, "payment_method_charge_id")]
		public virtual int Id { get; set; }

		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[Display(Name = "Warehouse", ResourceType = typeof(Resources))]
		[UIHint("WarehouseSelector")]
		public virtual int WarehouseId { get; set; }

		[BelongsTo("warehouse")]
		[Display(Name = "Warehouse", ResourceType = typeof(Resources))]
		public virtual Warehouse Warehouse { get; set; }

		[Property("name")]
		public virtual string Name { get; set; }

		[Property("number_of_payments")]
		public virtual int NumberOfPayments { get; set; }

		[Property("bank_payments_charge")]
		[DisplayFormat(DataFormatString = "{0:p}")]
		public virtual decimal BankPaymentsCharge { get; set; }

		[Property("payment_method")]
		[Display(Name = "PaymentMethod", ResourceType = typeof(Resources))]
		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		public virtual PaymentMethod PaymentMethod { get; set; }

		[Property("commission")]
		[DisplayFormat(DataFormatString = "{0:p}")]
		public virtual decimal CommissionByManage { get; set; }

		[Property("enabled")]
		public virtual bool IsActive { get; set; }

	}
}
