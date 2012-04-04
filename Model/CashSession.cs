// 
// CashSession.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.org>
//   Eduardo Nieto <enieto@mictlanix.org>
// 
// Copyright (C) 2011 Eddy Zavaleta, Mictlanix, and contributors.
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
    [ActiveRecord("cash_session")]
    public class CashSession : ActiveRecordLinqBase<CashSession>
    {
        IList<CashCount> cash_counts = new List<CashCount>();

        [PrimaryKey(PrimaryKeyType.Identity, "cash_session_id")]
        public int Id { get; set; }

        [BelongsTo("cashier")]
        [Display(Name = "Cashier", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        public virtual Employee Cashier { get; set; }

        [BelongsTo("cash_drawer")]
        [Display(Name = "CashDrawer", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        public virtual CashDrawer CashDrawer { get; set; }

        [Property]
        [DataType(DataType.DateTime)]
        [Display(Name = "Start", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        public DateTime Start { get; set; }

        [Property]
        [DataType(DataType.DateTime)]
        [Display(Name = "End", ResourceType = typeof(Resources))]
        public DateTime? End { get; set; }

        [HasMany(typeof(CashCount), Table = "cash_count", ColumnKey = "session")]
        public IList<CashCount> CashCounts
        {
            get { return cash_counts; }
            set { cash_counts = value; }
        }

        [DataType(DataType.Currency)]
        [Display(Name = "StartingCash", ResourceType = typeof(Resources))]
        public decimal StartingCash
        {
            get { return CashCounts.Where(x => x.Type == CashCountType.StartingCash).Sum(x => x.Total); }
        }

        [DataType(DataType.Currency)]
        [Display(Name = "CountedCash", ResourceType = typeof(Resources))]
        public decimal CountedCash
        {
            get { return CashCounts.Where(x => x.Type == CashCountType.CountedCash).Sum(x => x.Total); }
        }

        #region Override Base Methods

        public override string ToString()
        {
            return string.Format("{0} [{1} - {2}]", CashDrawer, Start, End);
        }

        public override bool Equals(object obj)
        {
            CashSession other = obj as CashSession;

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
