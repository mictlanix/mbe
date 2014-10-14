// 
// SystemObjects.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
//   Eduardo Nieto <enieto@mictlanix.com>
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
using System.ComponentModel.DataAnnotations;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Mictlanix.BE.Model {
	public enum SystemObjects : int {
        [Display(Name = "DisplayName_Products", ResourceType = typeof(Resources))]
        Products,
		[Display(Name = "Labels", ResourceType = typeof(Resources))]
        Labels,
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
        [Display(Name = "SalesOrders", ResourceType = typeof(Resources))]
        SalesOrders,
        [Display(Name = "DisplayName_CustomerPayments", ResourceType = typeof(Resources))]
        CustomerPayments,
        [Display(Name = "DisplayName_PointsOfSale", ResourceType = typeof(Resources))]
        PointsOfSale,
        [Display(Name = "DisplayName_CashDrawers", ResourceType = typeof(Resources))]
        CashDrawers,
        [Display(Name = "Addresses", ResourceType = typeof(Resources))]
        Addresses,
        [Display(Name = "Contacts", ResourceType = typeof(Resources))]
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
        InventoryTransfers,
        [Display(Name = "DisplayName_AccountsReceivable", ResourceType = typeof(Resources))]
        AccountsReceivable,
        [Display(Name = "DisplayName_AccountsPayable", ResourceType = typeof(Resources))]
        AccountsPayable,
        [Display(Name = "DisplayName_PurchasesOrders", ResourceType = typeof(Resources))]
        PurchasesOrders,
        [Display(Name = "DisplayName_SupplierPayment", ResourceType = typeof(Resources))]
        SupplierPayment,
		[Display(Name = "CustomerRefunds", ResourceType = typeof(Resources))]
		CustomerRefunds,
		[Display(Name = "FiscalDocuments", ResourceType = typeof(Resources))]
        FiscalDocuments,
        [Display(Name = "DisplayName_Taxpayers", ResourceType = typeof(Resources))]
        Taxpayers,
        [Display(Name = "DisplayName_SupplierReturns", ResourceType = typeof(Resources))]
        SupplierReturns,
		[Display(Name = "SalesOrdersHistoric", ResourceType = typeof(Resources))]
        SalesOrdersHistoric,
		[Display(Name = "CustomerRefundsHistoric", ResourceType = typeof(Resources))]
		CustomerRefundsHistoric,
        [Display(Name = "DisplayName_SupplierReturnHistoric", ResourceType = typeof(Resources))]
        SupplierReturnHistoric,
        [Display(Name = "DisplayName_Stores", ResourceType = typeof(Resources))]
        Stores,
		[Display(Name = "SalesQuotes", ResourceType = typeof(Resources))]
		SalesQuotes,
        [Display(Name = "FiscalReports", ResourceType = typeof(Resources))]
        FiscalReports,
        [Display(Name = "Kardex", ResourceType = typeof(Resources))]
        Kardex,
		[Display(Name = "ReceivedPayments", ResourceType = typeof(Resources))]
		ReceivedPayments,
		[Display(Name = "SalesByCustomer", ResourceType = typeof(Resources))]
		SalesByCustomer,
		[Display(Name = "SalesBySalesPerson", ResourceType = typeof(Resources))]
		SalesBySalesPerson,
		[Display(Name = "SalesByProduct", ResourceType = typeof(Resources))]
		SalesByProduct,
		[Display(Name = "GrossProfitsByCustomer", ResourceType = typeof(Resources))]
		GrossProfitsByCustomer,
		[Display(Name = "GrossProfitsBySalesPerson", ResourceType = typeof(Resources))]
		GrossProfitsBySalesPerson,
		[Display(Name = "GrossProfitsByProduct", ResourceType = typeof(Resources))]
		GrossProfitsByProduct,
		[Display(Name = "BestSellingProductsByCustomer", ResourceType = typeof(Resources))]
		BestSellingProductsByCustomer,
		[Display(Name = "BestSellingProductsBySalesPerson", ResourceType = typeof(Resources))]
		BestSellingProductsBySalesPerson,
		[Display(Name = "LotSerialNumbers", ResourceType = typeof(Resources))]
		LotSerialNumbers,
		[Display(Name = "ExchangeRates", ResourceType = typeof(Resources))]
		ExchangeRates,
		[Display(Name = "PointOfSale", ResourceType = typeof(Resources))]
		POS,
		[Display(Name = "LotsAndSerialNumbers", ResourceType = typeof(Resources))]
		SerialNumbersReport,
		[Display(Name = "CustomerDebt", ResourceType = typeof(Resources))]
		CustomerDebtReport,
		[Display(Name = "SalesOrderSummary", ResourceType = typeof(Resources))]
		SalesOrderSummaryReport,
		[Display(Name = "FiscalDocumentsReport", ResourceType = typeof(Resources))]
		FiscalDocumentsReport,
		[Display(Name = "SalesPersonOrders", ResourceType = typeof(Resources))]
		SalesPersonOrdersReport,
		[Display(Name = "CustomerSalesOrders", ResourceType = typeof(Resources))]
		CustomerSalesOrdersReport,
		[Display(Name = "ProductSalesByCustomer", ResourceType = typeof(Resources))]
		ProductSalesByCustomerReport,
		[Display(Name = "ProductSalesByModel", ResourceType = typeof(Resources))]
		ProductSalesByModelReport,
		[Display(Name = "ProductSalesByBrand", ResourceType = typeof(Resources))]
		ProductSalesByBrandReport,
		[Display(Name = "TaxpayerRecipients", ResourceType = typeof(Resources))]
		TaxpayerRecipients,
		[Display(Name = "ProductSalesBySalesPerson", ResourceType = typeof(Resources))]
		ProductSalesBySalesPerson,
		[Display(Name = "StandaloneFiscalDocuments", ResourceType = typeof(Resources))]
		StandaloneFiscalDocuments,
		[Display(Name = "ProductionOrders", ResourceType = typeof(Resources))]
		ProductionOrders,
		[Display(Name = "TechnicalServiceReports", ResourceType = typeof(Resources))]
		TechnicalServiceReports,
		[Display(Name = "TranslationRequests", ResourceType = typeof(Resources))]
		TranslationRequests,
		[Display(Name = "Notarizations", ResourceType = typeof(Resources))]
		Notarizations,
		[Display(Name = "ProductSalesBySalesPersonAndLabel", ResourceType = typeof(Resources))]
		ProductSalesBySalesPersonAndLabel,
		[Display(Name = "ProductSalesBySalesPersonAndBrand", ResourceType = typeof(Resources))]
		ProductSalesBySalesPersonAndBrand,
		[Display(Name = "ProductSalesBySalesPersonAndModel", ResourceType = typeof(Resources))]
		ProductSalesBySalesPersonAndModel,
		[Display(Name = "TechnicalServiceRequests", ResourceType = typeof(Resources))]
		TechnicalServiceRequests,
		[Display(Name = "TechnicalServiceReceipts", ResourceType = typeof(Resources))]
		TechnicalServiceReceipts,
        [Display(Name = "CustomersReport", ResourceType = typeof(Resources))]
        CustomersReport,
        [Display(Name = "WarehouseStocksReport", ResourceType = typeof (Resources))]
        WarehouseStocksReport,
        [Display(Name = "WarehouseSerialNumbersReport", ResourceType = typeof(Resources))]
        WarehouseSerialNumbersReport,
        [Display(Name = "SerialNumberKardex", ResourceType = typeof(Resources))]
        SerialNumberKardex
    }
}