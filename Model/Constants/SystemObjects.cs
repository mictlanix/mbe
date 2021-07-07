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

using System.ComponentModel.DataAnnotations;

namespace Mictlanix.BE.Model {
	public enum SystemObjects : int {
		[Display (Name = "DisplayName_Products", ResourceType = typeof (Resources))]
		Products = 0,
		[Display (Name = "Labels", ResourceType = typeof (Resources))]
		Labels = 1,
		[Display (Name = "DisplayName_Customers", ResourceType = typeof (Resources))]
		Customers = 2,
		[Display (Name = "DisplayName_Suppliers", ResourceType = typeof (Resources))]
		Suppliers = 3,
		[Display (Name = "DisplayName_Warehouses", ResourceType = typeof (Resources))]
		Warehouses = 4,
		[Display (Name = "DisplayName_PriceLists", ResourceType = typeof (Resources))]
		PriceLists = 5,
		[Display (Name = "DisplayName_Employees", ResourceType = typeof (Resources))]
		Employees = 6,
		[Display (Name = "SalesOrders", ResourceType = typeof (Resources))]
		SalesOrders = 7,
		[Display (Name = "DisplayName_CustomerPayments", ResourceType = typeof (Resources))]
		CustomerPayments = 8,
		[Display (Name = "DisplayName_PointsOfSale", ResourceType = typeof (Resources))]
		PointsOfSale = 9,
		[Display (Name = "DisplayName_CashDrawers", ResourceType = typeof (Resources))]
		CashDrawers = 10,
		[Display (Name = "Addresses", ResourceType = typeof (Resources))]
		Addresses = 11,
		[Display (Name = "Contacts", ResourceType = typeof (Resources))]
		Contacts = 12,
		[Display (Name = "DisplayName_BankAccounts", ResourceType = typeof (Resources))]
		BankAccounts = 13,
		[Display (Name = "DisplayName_SupplierAgreements", ResourceType = typeof (Resources))]
		SupplierAgreements = 14,
		[Display (Name = "DisplayName_InventoryReceipts", ResourceType = typeof (Resources))]
		InventoryReceipts = 15,
		[Display (Name = "DisplayName_InventoryIssues", ResourceType = typeof (Resources))]
		InventoryIssues = 16,
		[Display (Name = "DisplayName_InventoryTransfers", ResourceType = typeof (Resources))]
		InventoryTransfers = 17,
		[Display (Name = "DisplayName_AccountsReceivable", ResourceType = typeof (Resources))]
		AccountsReceivable = 18,
		[Display (Name = "DisplayName_AccountsPayable", ResourceType = typeof (Resources))]
		AccountsPayable = 19,
		[Display (Name = "DisplayName_PurchasesOrders", ResourceType = typeof (Resources))]
		PurchasesOrders = 20,
		[Display (Name = "DisplayName_SupplierPayment", ResourceType = typeof (Resources))]
		SupplierPayment = 21,
		[Display (Name = "CustomerRefunds", ResourceType = typeof (Resources))]
		CustomerRefunds = 22,
		[Display (Name = "FiscalDocuments", ResourceType = typeof (Resources))]
		FiscalDocuments = 23,
		[Display (Name = "DisplayName_Taxpayers", ResourceType = typeof (Resources))]
		Taxpayers = 24,
		[Display (Name = "DisplayName_SupplierReturns", ResourceType = typeof (Resources))]
		SupplierReturns = 25,
		[Display (Name = "SalesOrdersHistoric", ResourceType = typeof (Resources))]
		SalesOrdersHistoric = 26,
		[Display (Name = "CustomerRefundsHistoric", ResourceType = typeof (Resources))]
		CustomerRefundsHistoric = 27,
		[Display (Name = "DisplayName_SupplierReturnHistoric", ResourceType = typeof (Resources))]
		SupplierReturnHistoric = 28,
		[Display (Name = "DisplayName_Stores", ResourceType = typeof (Resources))]
		Stores = 29,
		[Display (Name = "SalesQuotes", ResourceType = typeof (Resources))]
		SalesQuotes = 30,
		//[Display (Name = "FiscalReports", ResourceType = typeof (Resources))]
		//FiscalReports = 31,
		[Display (Name = "Kardex", ResourceType = typeof (Resources))]
		Kardex = 32,
		[Display (Name = "ReceivedPayments", ResourceType = typeof (Resources))]
		ReceivedPayments = 33,
		[Display (Name = "SalesByCustomer", ResourceType = typeof (Resources))]
		SalesByCustomer = 34,
		[Display (Name = "SalesBySalesPerson", ResourceType = typeof (Resources))]
		SalesBySalesPerson = 35,
		[Display (Name = "SalesByProduct", ResourceType = typeof (Resources))]
		SalesByProduct = 36,
		[Display (Name = "GrossProfitsByCustomer", ResourceType = typeof (Resources))]
		GrossProfitsByCustomer = 37,
		[Display (Name = "GrossProfitsBySalesPerson", ResourceType = typeof (Resources))]
		GrossProfitsBySalesPerson = 38,
		[Display (Name = "GrossProfitsByProduct", ResourceType = typeof (Resources))]
		GrossProfitsByProduct = 39,
		[Display (Name = "BestSellingProductsByCustomer", ResourceType = typeof (Resources))]
		BestSellingProductsByCustomer = 40,
		[Display (Name = "BestSellingProductsBySalesPerson", ResourceType = typeof (Resources))]
		BestSellingProductsBySalesPerson = 41,
		[Display (Name = "LotSerialNumbers", ResourceType = typeof (Resources))]
		LotSerialNumbers = 42,
		[Display (Name = "ExchangeRates", ResourceType = typeof (Resources))]
		ExchangeRates = 43,
		[Display (Name = "PointOfSale", ResourceType = typeof (Resources))]
		POS = 44,
		[Display (Name = "SerialNumberKardex", ResourceType = typeof (Resources))]
		SerialNumberKardex = 45,
		[Display (Name = "CustomerDebt", ResourceType = typeof (Resources))]
		CustomerDebtReport = 46,
		[Display (Name = "SalesOrderSummary", ResourceType = typeof (Resources))]
		SalesOrderSummaryReport = 47,
		[Display (Name = "FiscalDocumentsReport", ResourceType = typeof (Resources))]
		FiscalDocumentsReport = 48,
		[Display (Name = "SalesPersonOrders", ResourceType = typeof (Resources))]
		SalesPersonOrdersReport = 49,
		[Display (Name = "CustomerSalesOrders", ResourceType = typeof (Resources))]
		CustomerSalesOrdersReport = 50,
		[Display (Name = "ProductSalesByCustomer", ResourceType = typeof (Resources))]
		ProductSalesByCustomerReport = 51,
		[Display (Name = "ProductSalesByModel", ResourceType = typeof (Resources))]
		ProductSalesByModelReport = 52,
		[Display (Name = "ProductSalesByBrand", ResourceType = typeof (Resources))]
		ProductSalesByBrandReport = 53,
		[Display (Name = "TaxpayerRecipients", ResourceType = typeof (Resources))]
		TaxpayerRecipients = 54,
		[Display (Name = "ProductSalesBySalesPerson", ResourceType = typeof (Resources))]
		ProductSalesBySalesPerson = 55,
		[Display (Name = "StandaloneFiscalDocuments", ResourceType = typeof (Resources))]
		StandaloneFiscalDocuments = 56,
		[Display (Name = "ProductionOrders", ResourceType = typeof (Resources))]
		ProductionOrders = 57,
		[Display (Name = "TechnicalServiceReports", ResourceType = typeof (Resources))]
		TechnicalServiceReports = 58,
		[Display (Name = "TranslationRequests", ResourceType = typeof (Resources))]
		TranslationRequests = 59,
		[Display (Name = "Notarizations", ResourceType = typeof (Resources))]
		Notarizations = 60,
		[Display (Name = "ProductSalesBySalesPersonAndLabel", ResourceType = typeof (Resources))]
		ProductSalesBySalesPersonAndLabel = 61,
		[Display (Name = "ProductSalesBySalesPersonAndBrand", ResourceType = typeof (Resources))]
		ProductSalesBySalesPersonAndBrand = 62,
		[Display (Name = "ProductSalesBySalesPersonAndModel", ResourceType = typeof (Resources))]
		ProductSalesBySalesPersonAndModel = 63,
		[Display (Name = "TechnicalServiceRequests", ResourceType = typeof (Resources))]
		TechnicalServiceRequests = 64,
		[Display (Name = "TechnicalServiceReceipts", ResourceType = typeof (Resources))]
		TechnicalServiceReceipts = 65,
		[Display (Name = "CustomersReport", ResourceType = typeof (Resources))]
		CustomersReport = 66,
		[Display (Name = "WarehouseStockReport", ResourceType = typeof (Resources))]
		WarehouseStockReport = 67,
		[Display (Name = "WarehouseStockByLotReport", ResourceType = typeof (Resources))]
		WarehouseStockByLotReport = 68,
		[Display (Name = "WarehouseStockBySerialNumberReport", ResourceType = typeof (Resources))]
		WarehouseStockBySerialNumberReport = 69,
		//[Display (Name = "SalesOrderShipments", ResourceType = typeof (Resources))]
		//SalesOrderShipments = 70,
		[Display (Name = "DeliveryOrders", ResourceType = typeof (Resources))]
		DeliveryOrders = 71,
		[Display (Name = "SalesPersonOrdersAndRefunds", ResourceType = typeof (Resources))]
		SalesPersonOrdersAndRefundsReport = 72,
		[Display (Name = "ProductsMerge", ResourceType = typeof (Resources))]
		ProductsMerge = 73,
		[Display (Name = "PhysicalCountAdjustment", ResourceType = typeof (Resources))]
		PhysicalCountAdjustment = 74,
		[Display (Name = "ProductsBySupplier", ResourceType = typeof (Resources))]
		ProductsBySupplierReport = 75,
		//[Display(Name = "EmployeeAttendance", ResourceType = typeof(Resources))]
		//EmployeeAttendance = 76,
		//[Display(Name = "EmployeeSchedules", ResourceType = typeof(Resources))]
		//EmployeeSchedules = 77,
		//[Display(Name = "SynchronizeClocks", ResourceType = typeof(Resources))]
		//SynchronizeClocks = 78,
		[Display (Name = "ProductsOrdersAndRefundsBySalesPerson", ResourceType = typeof (Resources))]
		ProductsOrdersAndRefundsBySalesPerson = 79,
		[Display (Name = "PendantDeliveries", ResourceType = typeof (Resources))]
		PendantDeliveries = 80,
		[Display (Name = "Expenses", ResourceType = typeof (Resources))]
		Expenses = 81,
		[Display (Name = "ExpenseVoucher", ResourceType = typeof (Resources))]
		ExpenseVoucher = 82,
		[Display (Name = "CreditPayments", ResourceType = typeof (Resources))]
		CreditPayments = 83,
		[Display (Name = "PaymentMethodOptions", ResourceType = typeof (Resources))]
		PaymentMethodOptions = 84,
		[Display (Name = "PaymentReceipt", ResourceType = typeof (Resources))]
		PaymentReceipt = 85,
		[Display (Name = "Purchases", ResourceType = typeof (Resources))]
		PurchasesFast = 86,
		[Display (Name = "PurchaseClearance", ResourceType = typeof (Resources))]
		PurchaseClearance = 87
		}
}