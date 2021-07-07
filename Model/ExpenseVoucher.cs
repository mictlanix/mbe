using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;


namespace Mictlanix.BE.Model
{

	[ActiveRecord("expense_voucher", Lazy = true)]

	public class ExpenseVoucher : ActiveRecordLinqBase<ExpenseVoucher>
	{

		IList<ExpenseVoucherDetail> details = new List<ExpenseVoucherDetail>();

		[PrimaryKey(PrimaryKeyType.Identity, "expense_voucher_id")]
		[DisplayFormat(DataFormatString = "{0:D8}")]
		public virtual int Id { get; set; }

		[BelongsTo("creator", Lazy = FetchWhen.OnInvoke)]
		[Display(Name = "Creator", ResourceType = typeof(Resources))]
		public virtual Employee Creator { get; set; }

		[BelongsTo("updater", Lazy = FetchWhen.OnInvoke)]
		[Display(Name = "Updater", ResourceType = typeof(Resources))]
		public virtual Employee Updater { get; set; }

		[BelongsTo("cash_session", Lazy = FetchWhen.OnInvoke)]
		[Display(Name = "CashSession", ResourceType = typeof(Resources))]
		public virtual CashSession CashSession { get; set; }

		[Property("date")]
		[Display(Name = "Date", ResourceType = typeof(Resources))]
		public virtual DateTime Date { get; set; }

		[Property("creation_time")]
		[Display(Name = "CreationTime", ResourceType = typeof(Resources))]
		public virtual DateTime CreationTime { get; set; }

		[Property("modification_time")]
		[Display(Name = "ModificationTime", ResourceType = typeof(Resources))]
		public virtual DateTime ModificationTime { get; set; }

		[BelongsTo("store")]
		[Display(Name = "Store", ResourceType = typeof(Resources))]
		public virtual Store Store { get; set; }

		[Property("comment")]
		[DataType(DataType.MultilineText)]
		[Display(Name = "Comment", ResourceType = typeof(Resources))]
		[StringLength(500, MinimumLength = 0, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public virtual string Comment { get; set; }

		[HasMany(typeof(ExpenseVoucherDetail), Table = "expense_voucher_detail", ColumnKey = "expense_voucher", Lazy = true)]
		public virtual IList<ExpenseVoucherDetail> Details
		{
			get { return details; }
			set { details = value; }
		}

		[DataType(DataType.Currency)]
		[Display(Name = "Total", ResourceType = typeof(Resources))]
		public virtual decimal Total { get { return Details.Sum(x => x.Amount); } }

		[Property("completed")]
		[Display(Name = "Completed", ResourceType = typeof(Resources))]
		public virtual bool IsCompleted { get; set; }

		[Property("cancelled")]
		[Display(Name = "Cancelled", ResourceType = typeof(Resources))]
		public virtual bool IsCancelled { get; set; }

		[BelongsTo ("purchase_order", Lazy = FetchWhen.OnInvoke)]
		[Display (Name = "PurchaseOrder", ResourceType = typeof (Resources))]
		public virtual PurchaseOrder PurchaseOrder { get; set; }
	}
}
