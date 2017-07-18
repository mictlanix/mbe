using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Mictlanix.BE.Model {
	[ActiveRecord ("payment_method_option")]
	public class PaymentMethodOption : ActiveRecordLinqBase<PaymentMethodOption> {
		[PrimaryKey (PrimaryKeyType.Identity, "payment_method_option_id")]
		public int Id { get; set; }

		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "Store", ResourceType = typeof (Resources))]
		[UIHint ("StoreSelector")]
		public virtual int StoreId { get; set; }

		[BelongsTo ("store")]
		[Display (Name = "Store", ResourceType = typeof (Resources))]
		public virtual Store Store { get; set; }

		[Property ("name")]
		[Display (Name = "Name", ResourceType = typeof (Resources))]
		public string Name { get; set; }

		[Property ("number_of_payments")]
		[Display (Name = "NumberOfPayments", ResourceType = typeof (Resources))]
		[Range (1, int.MaxValue, ErrorMessageResourceName = "Validation_CannotBeZeroOrNegative", ErrorMessageResourceType = typeof (Resources))]
		public int NumberOfPayments { get; set; }

		[Property ("display_on_ticket")]
		[Display (Name = "DisplayOnTicket", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		public bool isDisplayedOnTicket { get; set; }

		[Property ("payment_method")]
		[Display (Name = "PaymentMethod", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		public PaymentMethod PaymentMethod { get; set; }

		[Property ("commission")]
		[DisplayFormat (DataFormatString = "{0:p}")]
		[Display (Name = "CommissionByManage", ResourceType = typeof (Resources))]
		public decimal CommissionByManage { get; set; }

		[Property ("enabled")]
		[Display (Name = "Active", ResourceType = typeof (Resources))]
		public bool IsActive { get; set; }

	}
}
