// 
// SalesOrder.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.org>
//   Eduardo Nieto <enieto@mictlanix.org>
// 
// Copyright (C) 2011-2013 Eddy Zavaleta, Mictlanix, and contributors.
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

namespace Mictlanix.BE.Model
{
    [ActiveRecord("sales_order", Lazy = true)]
    public class SalesOrder : ActiveRecordLinqBase<SalesOrder>
    {
        IList<SalesOrderDetail> details = new List<SalesOrderDetail>();
        IList<CustomerPayment> payments = new List<CustomerPayment>();

        [PrimaryKey(PrimaryKeyType.Identity, "sales_order_id")]
        [Display(Name = "SalesOrderId", ResourceType = typeof(Resources))]
		[DisplayFormat(DataFormatString="{0:D8}")]
        public virtual int Id { get; set; }
		
        [Property("creation_time")]
		[DataType(DataType.DateTime)]
		[Display(Name = "CreationTime", ResourceType = typeof(Resources))]
		public virtual DateTime CreationTime { get; set; }

        [Property("modification_time")]
		[DataType(DataType.DateTime)]
		[Display(Name = "ModificationTime", ResourceType = typeof(Resources))]
		public virtual DateTime ModificationTime { get; set; }

        [BelongsTo("creator", Lazy = FetchWhen.OnInvoke)]
        [Display(Name = "Creator", ResourceType = typeof(Resources))]
        public virtual Employee Creator { get; set; }

        [BelongsTo("updater", Lazy = FetchWhen.OnInvoke)]
        [Display(Name = "Updater", ResourceType = typeof(Resources))]
        public virtual Employee Updater { get; set; }

		[BelongsTo("store")]
		[Display(Name = "Store", ResourceType = typeof(Resources))]
		public virtual Store Store { get; set; }
		
		[Property("serial")]
		[Display(Name = "Serial", ResourceType = typeof(Resources))]
		[DisplayFormat(DataFormatString="{0:D8}")]
		public virtual int Serial { get; set; }

        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [Display(Name = "Customer", ResourceType = typeof(Resources))]
        [UIHint("CustomerSelector")]
        public virtual int CustomerId { get; set; }

        [BelongsTo("customer", NotNull = true, Fetch = FetchEnum.Join)]
        [Display(Name = "Customer", ResourceType = typeof(Resources))]
        public virtual Customer Customer { get; set; }

		[Display(Name = "ShipTo", ResourceType = typeof(Resources))]
		[UIHint("AddressSelector")]
		public virtual int ShipToId { get; set; }
		
		[BelongsTo("ship_to", Lazy = FetchWhen.OnInvoke)]
		[Display(Name = "ShipTo", ResourceType = typeof(Resources))]
		public virtual Address ShipTo { get; set; }

        [Property]
        [DataType(DataType.DateTime)]
        [Display(Name = "Date", ResourceType = typeof(Resources))]
        public virtual DateTime Date { get; set; }

		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[Display(Name = "SalesPerson", ResourceType = typeof(Resources))]
		[UIHint("EmployeeSelector")]
		public virtual int SalesPersonId { get; set; }

        [BelongsTo("salesperson")]
        [Display(Name = "SalesPerson", ResourceType = typeof(Resources))]
        public virtual Employee SalesPerson { get; set; }
		
        [BelongsTo("point_sale")]
        [Display(Name = "PointOfSale", ResourceType = typeof(Resources))]
        public virtual PointOfSale PointOfSale { get; set; }

        [Property("credit")]
        [Display(Name = "Credit", ResourceType = typeof(Resources))]
        public virtual bool IsCredit { get; set; }

        [Property("due_date")]
        [DataType(DataType.Date)]
        [Display(Name = "DueDate", ResourceType = typeof(Resources))]
        public virtual DateTime DueDate { get; set; }

        [Property("completed")]
        [Display(Name = "Completed", ResourceType = typeof(Resources))]
        public virtual bool IsCompleted { get; set; }

        [Property("paid")]
        [Display(Name = "Paid", ResourceType = typeof(Resources))]
        public virtual bool IsPaid { get; set; }
		
		[Property("delivered")]
		[Display(Name = "Delivery", ResourceType = typeof(Resources))]
		public virtual bool IsDelivered { get; set; }
		
		[Property("cancelled")]
		[Display(Name = "Cancelled", ResourceType = typeof(Resources))]
		public virtual bool IsCancelled { get; set; }

        [HasMany(typeof(SalesOrderDetail), Table = "sales_order_detail", ColumnKey = "sales_order", Lazy = true)]
        public virtual IList<SalesOrderDetail> Details
        {
            get { return details; }
            set { details = value; }
        }

        [HasMany(typeof(CustomerPayment), Table = "customer_payment", ColumnKey = "sales_order", Lazy = true)]
        public virtual IList<CustomerPayment> Payments
        {
            get { return payments; }
            set { payments = value; }
        }
        
        [DataType(DataType.Currency)]
        [Display(Name = "Subtotal", ResourceType = typeof(Resources))]
        public virtual decimal Subtotal
        {
            get { return Details.Sum(x => x.Subtotal); }
        }

        [DataType(DataType.Currency)]
        [Display(Name = "Taxes", ResourceType = typeof(Resources))]
        public virtual decimal Taxes
        {
            get { return Total - Subtotal; }
        }

        [DataType(DataType.Currency)]
        [Display(Name = "Total", ResourceType = typeof(Resources))]
        public virtual decimal Total
        {
            get { return Details.Sum(x => x.Total); }
        }

        [DataType(DataType.Currency)]
        [Display(Name = "Paid", ResourceType = typeof(Resources))]
        public virtual decimal Paid
        {
            get { return Payments.Sum(x => x.Amount + x.Change.GetValueOrDefault()); }
        }

        [DataType(DataType.Currency)]
        [Display(Name = "Balance", ResourceType = typeof(Resources))]
        public virtual decimal Balance
        {
            get { return Paid - Total; }
        }

        #region Override Base Methods

        public override string ToString()
        {
			return string.Format("{0:D8} [{1}, {2}, {3}]", Id, Customer, Date, SalesPerson);
        }

        public override bool Equals(object obj)
        {
            SalesOrder other = obj as SalesOrder;

            if (other == null)
                return false;

            if (Id == 0 && other.Id == 0)
                return (object)this == other;
            else
                return Id == other.Id;
        }

        public override int GetHashCode()
        {
            if (Id == 0)
                return base.GetHashCode();

            return string.Format("{0}#{1}", GetType().FullName, Id).GetHashCode();
        }

        #endregion
    }
}
