// 
// CashSession.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.org>
//   Eduardo Nieto <enieto@mictlanix.org>
// 
// Copyright (C) 2011 Eddy Zavaleta, Mictlanix (http://www.mictlanix.org)
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
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Business.Essentials.Model
{
    [ActiveRecord("cash_session")]
    public class CashSession : ActiveRecordLinqBase<CashSession>
    {
        [PrimaryKey(PrimaryKeyType.Identity, "cash_session_id")]
        public int Id { get; set; }

        [Property]
        [DataType(DataType.DateTime)]
        [Display(Name = "Start", ResourceType = typeof(Resources))]
        public DateTime? Start { get; set; }

        [Property]
        [DataType(DataType.DateTime)]
        [Display(Name = "End", ResourceType = typeof(Resources))]
        public DateTime? End { get; set; }

        [BelongsTo("cashier")]
        [Display(Name = "Cashier", ResourceType = typeof(Resources))]
        public virtual Employee Cashier { get; set; }

        [BelongsTo("cash_drawer")]
        [Display(Name = "CashDrawer", ResourceType = typeof(Resources))]
        public virtual CashDrawer CashDrawer { get; set; }
    }
}
