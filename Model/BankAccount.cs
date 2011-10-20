// 
// BankAccount.cs
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
using System.ComponentModel.DataAnnotations;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Business.Essentials.Model
{
    [ActiveRecord("bank_account")]
    public class BankAccount : ActiveRecordLinqBase<BankAccount>
    {
        IList<Supplier> suppliers = new List<Supplier>();

        [PrimaryKey(PrimaryKeyType.Identity, "bank_account_id")]
        public int Id { get; set; }

        [Property("bank_name")]
        [Display(Name = "BankName", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(250, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string BankName { get; set; }

        [Property("account_number")]
        [Display(Name = "AccountNumber", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(20, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string AccountNumber { get; set; }

        [Property]
        [Display(Name = "Reference", ResourceType = typeof(Resources))]
        [StringLength(20, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Reference { get; set; }

        [Property("routing_number")]
        [Display(Name = "RoutingNumber", ResourceType = typeof(Resources))]
        [StringLength(18, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string RoutingNumber { get; set; }
        
        [Property]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Comment", ResourceType = typeof(Resources))]
        [StringLength(500, MinimumLength = 0)]
        public string Comment { get; set; }

        [HasAndBelongsToMany(typeof(Supplier), Table = "supplier_bank_account", ColumnKey = "bank_account", ColumnRef = "supplier", Inverse = true)]
        public IList<Supplier> Suppliers
        {
            get { return suppliers; }
            set { suppliers = value; }
        }

    }
}