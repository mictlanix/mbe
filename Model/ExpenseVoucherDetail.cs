using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Mictlanix.BE.Model;
using System.ComponentModel.DataAnnotations;


namespace Mictlanix.BE.Model{

    [ActiveRecord("expense_voucher_detail")]
    public class ExpenseVoucherDetail:ActiveRecordLinqBase<ExpenseVoucherDetail>{

        [PrimaryKey(PrimaryKeyType.Identity, "expense_voucher_detail_id")]
        public virtual int Id { get; set; }

        [BelongsTo("expense", Lazy = FetchWhen.OnInvoke)]
        [Display(Name = "Expense", ResourceType = typeof(Resources))]
        public virtual Expense Expense { get; set; }

        [BelongsTo("expense_voucher")]
        [Display(Name = "ExpenseVoucher", ResourceType = typeof(Resources))]
        public virtual ExpenseVoucher ExpenseVoucher { get; set; }

        [Property("amount")]
        [Display(Name = "Amount")]
		  [DataType(DataType.Currency)]
        public virtual decimal Amount { get; set; }

		  [Property("comment")]
		  [Display(Name = "Comment")]
		  public virtual string Comment { get; set; }

    }
}
