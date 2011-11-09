﻿// 
// SystemObjects.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.org>
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
    public enum SystemObjects : int
    {
        [Display(Name = "DisplayName_Products", ResourceType = typeof(Resources))]
        Products,
        [Display(Name = "DisplayName_Categories", ResourceType = typeof(Resources))]
        Categories,
        [Display(Name = "DisplayName_Customers", ResourceType = typeof(Resources))]
        Customers,
        [Display(Name = "DisplayName_Suppliers", ResourceType = typeof(Resources))]
        Suppliers,
        [Display(Name = "DisplayName_Warehouses", ResourceType = typeof(Resources))]
        Warehouses,
        [Display(Name = "DisplayName_PriceLists", ResourceType = typeof(Resources))]
        PriceLists,
        [Display(Name = "DisplayName_Employees", ResourceType = typeof(Resources))]
        Employees,
        [Display(Name = "DisplayName_SalesOrders", ResourceType = typeof(Resources))]
        SalesOrders,
        [Display(Name = "DisplayName_CustomerPayments", ResourceType = typeof(Resources))]
        CustomerPayments,
        [Display(Name = "DisplayName_PointsOfSale", ResourceType = typeof(Resources))]
        PointsOfSale,
        [Display(Name = "DisplayName_CashDrawers", ResourceType = typeof(Resources))]
        CashDrawers,
        [Display(Name = "DisplayName_Addresses", ResourceType = typeof(Resources))]
        Addresses,
        [Display(Name = "DisplayName_Contacts", ResourceType = typeof(Resources))]
        Contacts,
        [Display(Name = "DisplayName_BankAccounts", ResourceType = typeof(Resources))]
		BankAccounts,
        [Display(Name = "DisplayName_SupplierAgreements", ResourceType = typeof(Resources))]
        SupplierAgreements,
        [Display(Name = "DisplayName_InventoryReceipts", ResourceType = typeof(Resources))]
        InventoryReceipts,
        [Display(Name = "DisplayName_InventoryIssues", ResourceType = typeof(Resources))]
        InventoryIssues,
        [Display(Name = "DisplayName_InventoryTransfers", ResourceType = typeof(Resources))]
        InventoryTransfers
    }
}