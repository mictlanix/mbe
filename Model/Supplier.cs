// 
// Supplier.cs
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
    [ActiveRecord("supplier")]
    public class Supplier : ActiveRecordLinqBase<Supplier>
    {
        IList<Address> addresses = new List<Address>();
        IList<Contact> contacts = new List<Contact>();
        IList<BankAccount> accounts = new List<BankAccount>();
        IList<SupplierAgreement> agrements = new List<SupplierAgreement>();


        [PrimaryKey(PrimaryKeyType.Identity, "supplier_id")]
        public int Id { get; set; }

        [Property]
        [Display(Name = "Code", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(25, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Code { get; set; }

        [Property]
        [Display(Name = "Name", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(250, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Name { get; set; }

        [Property]
        [Display(Name = "Zone", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(250, MinimumLength = 1, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Zone { get; set; }

        [Property("credit_limit")]
        [DataType(DataType.Currency)]
        [Display(Name = "CreditLimit", ResourceType = typeof(Resources))]
        public decimal CreditLimit { get; set; }

        [Property("credit_days")]
        [Display(Name = "CreditDays", ResourceType = typeof(Resources))]
        public int CreditDays { get; set; }

        [HasAndBelongsToMany(typeof(Address), Table = "supplier_address", ColumnKey = "supplier", ColumnRef = "address", Inverse = true)]
        public IList<Address> Addresses
        {
            get { return addresses; }
            set { addresses = value; }
        }

        [HasAndBelongsToMany(typeof(Contact), Table = "supplier_contact", ColumnKey = "supplier", ColumnRef = "contact", Inverse = true)]
        public IList<Contact> Contacts
        {
            get { return contacts; }
            set { contacts = value; }
        }

        [HasAndBelongsToMany(typeof(BankAccount), Table = "supplier_bank_account", ColumnKey = "supplier", ColumnRef = "bank_account", Inverse = true)]
        public IList<BankAccount> BanksAccounts
        {
            get { return accounts; }
            set { accounts = value; }
        }

        [HasMany( typeof(SupplierAgreement), Table = "supplier_agreement", ColumnKey = "supplier")]
        public IList<SupplierAgreement> Agreements
        {
            get { return agrements; }
            set { agrements = value; }
        }

        
    }
}