# Reports Specs

All reports are read-only analytical views. They share common filter patterns and export to CSV/PDF. Each maps to a `SystemObject` that controls access.

---

## Common UI Patterns

- **Date Range**: start date / end date selector (defaults to current month)
- **Store Filter**: single or all stores (admin sees all; user sees their store)
- **Export**: CSV download and printable PDF for all reports
- **Pagination**: server-side paging for large result sets

---

## 1. Received Payments

**SystemObject**: `ReceivedPayments`  
**Action**: `Reports/ReceivedPayments`

### Purpose
List all customer payments received within a date range.

### Filters
- Store, date range, payment method, customer, salesperson, cash session

### Columns
- Date, folio, customer, salesperson, method, reference, amount, currency, cash session, verified by

### Key Tables
- `customer_payment`, `customer`, `employee`, `cash_session`

---

## 2. Sales by Product

**SystemObject**: `SalesByProduct`  
**Action**: `Reports/SalesByProduct`

### Purpose
Total sales quantity and revenue grouped by product.

### Filters
- Store, date range, warehouse, label, brand, model

### Columns
- Product code, name, brand, model, unit of measure, quantity sold, total revenue, total cost, gross profit

### Key Tables
- `sales_order_detail`, `sales_order`, `product`

---

## 3. Gross Profits by Customer

**SystemObject**: `GrossProfitsByCustomer`  
**Action**: `Reports/GrossProfitsByCustomer`

### Purpose
Revenue, cost, and gross profit margin per customer.

### Filters
- Store, date range, salesperson, price list

### Columns
- Customer, total sales, total cost, gross profit, margin %

---

## 4. Gross Profits by Salesperson

**SystemObject**: `GrossProfitsBySalesPerson`  
**Action**: `Reports/GrossProfitsBySalesPerson`

### Purpose
Revenue and gross profit per salesperson.

### Columns
- Salesperson, order count, total sales, total cost, gross profit, margin %

---

## 5. Gross Profits by Product

**SystemObject**: `GrossProfitsByProduct`  
**Action**: `Reports/GrossProfitsByProduct`

### Purpose
Margin analysis at the product level.

### Columns
- Product code, name, quantity sold, revenue, cost, gross profit, margin %

---

## 6. Best Selling Products by Customer

**SystemObject**: `BestSellingProductsByCustomer`  
**Action**: `Reports/BestSellingProductsByCustomer`

### Purpose
For a selected customer, which products were purchased most (by quantity or revenue).

### Filters
- Customer (required), date range, store

### Columns
- Product code, name, quantity, revenue, last purchase date

---

## 7. Best Selling Products by Salesperson

**SystemObject**: `BestSellingProductsBySalesPerson`  
**Action**: `Reports/BestSellingProductsBySalesPerson`

### Purpose
For a selected salesperson, top-selling products.

### Filters
- Salesperson (required), date range, store

---

## 8. Customers Report

**SystemObject**: `CustomersReport`  
**Action**: `Reports/CustomersReport`

### Purpose
Customer directory / profile export.

### Columns
- Code, name, zone, credit limit, credit days, price list, salesperson, disabled

---

## 9. Sales by Customer

**SystemObject**: `SalesByCustomer`  
**Action**: `Reports/SalesByCustomer`

### Purpose
Total sales per customer within a period.

### Columns
- Customer, order count, total quantity, total revenue, total paid, balance

---

## 10. Sales by Salesperson

**SystemObject**: `SalesBySalesPerson`  
**Action**: `Reports/SalesBySalesPerson`

### Purpose
Total sales per salesperson.

### Columns
- Salesperson, order count, total revenue, total cost, gross profit, margin %

---

## 11. Customer Sales Orders Report

**SystemObject**: `CustomerSalesOrdersReport`  
**Action**: `Reports/CustomerSalesOrders`

### Purpose
Detailed order listing per customer.

### Filters
- Customer (required), date range, store, status

### Columns
- Folio, date, due date, total, paid, balance, status

---

## 12. Salesperson Orders Report

**SystemObject**: `SalesPersonOrdersReport`  
**Action**: `Reports/SalesPersonOrders`

### Purpose
Order listing per salesperson.

### Filters
- Salesperson, date range, store

---

## 13. Salesperson Orders and Refunds Report

**SystemObject**: `SalesPersonOrdersAndRefundsReport`  
**Action**: `Reports/SalesPersonOrdersAndRefunds`

### Purpose
Net sales per salesperson after deducting refunds.

### Columns
- Salesperson, gross sales, total refunds, net sales, gross profit

---

## 14. Product Sales by Customer Report

**SystemObject**: `ProductSalesByCustomerReport`  
**Action**: `Reports/ProductSalesByCustomer`

### Purpose
Cross-tab: which customers bought a specific product.

### Filters
- Product (required), date range, store

### Columns
- Customer, quantity, revenue, last purchase date

---

## 15. Product Sales by Brand Report

**SystemObject**: `ProductSalesByBrandReport`  
**Action**: `Reports/ProductSalesByBrand`

### Purpose
Revenue and quantity grouped by product brand.

### Columns
- Brand, product count, total quantity, total revenue

---

## 16. Product Sales by Model Report

**SystemObject**: `ProductSalesByModelReport`  
**Action**: `Reports/ProductSalesByModel`

### Purpose
Revenue and quantity grouped by product model.

---

## 17. Product Sales by Salesperson

**SystemObject**: `ProductSalesBySalesPerson`  
**Action**: `Reports/ProductSalesBySalesPerson`

### Purpose
Which products each salesperson sold most.

---

## 18. Product Sales by Salesperson and Label

**SystemObject**: `ProductSalesBySalesPersonAndLabel`  
**Action**: `Reports/ProductSalesBySalesPersonAndLabel`

### Purpose
Product sales per salesperson, segmented by product label (category).

---

## 19. Product Sales by Salesperson and Brand

**SystemObject**: `ProductSalesBySalesPersonAndBrand`  
**Action**: `Reports/ProductSalesBySalesPersonAndBrand`

### Purpose
Product sales per salesperson, segmented by brand.

---

## 20. Product Sales by Salesperson and Model

**SystemObject**: `ProductSalesBySalesPersonAndModel`  
**Action**: `Reports/ProductSalesBySalesPersonAndModel`

### Purpose
Product sales per salesperson, segmented by model.

---

## 21. Sales Order Summary Report

**SystemObject**: `SalesOrderSummaryReport`  
**Action**: `Reports/SalesOrderSummary`

### Purpose
High-level summary: total orders, revenue, collected, and balance within a period — grouped by day or month.

### Columns
- Period, order count, total revenue, total paid, balance, refunds, net

---

## 22. Fiscal Documents Report

**SystemObject**: `FiscalDocumentsReport`  
**Action**: `Reports/FiscalDocuments`

### Purpose
List of all CFDI documents issued, with stamping status and totals.

### Filters
- Issuer RFC, date range, type, status (stamped/cancelled)

### Columns
- UUID, series-folio, date, recipient RFC, recipient name, subtotal, VAT, total, status

---

## 23. Warehouse Stock Report

**SystemObject**: `WarehouseStockReport`  
**Action**: `Reports/WarehouseStockReport`

### Purpose
Current stock levels per product per warehouse.

### Filters
- Warehouse, product, label, brand — as of a specific date

### Columns
- Warehouse, product code, name, unit, quantity on hand, unit cost, total value

### Key Calculation
Stock = net sum of all `lot_serial_tracking.quantity` entries for warehouse + product up to the specified date.

---

## 24. Warehouse Stock by Lot Report

**SystemObject**: `WarehouseStockByLotReport`  
**Action**: `Reports/WarehouseStockByLotReport`

### Purpose
Stock broken down by lot number and expiration date. Critical for perishable products.

### Columns
- Warehouse, product, lot number, expiration date, quantity

---

## 25. Warehouse Stock by Serial Number Report

**SystemObject**: `WarehouseStockBySerialNumberReport`  
**Action**: `Reports/WarehouseStockBySerialNumberReport`

### Purpose
Stock broken down by individual serial number. For seriable products.

### Columns
- Warehouse, product, serial number, location

---

## 26. Kardex

**SystemObject**: `Kardex`  
**Action**: `Reports/Kardex`

### Purpose
Full movement history (ledger) for a product in a warehouse — all ins and outs with running balance.

### Filters
- Product (required), warehouse, date range

### Columns
- Date, document type, document folio, quantity in, quantity out, balance, unit cost, total cost

---

## 27. Serial Number Kardex

**SystemObject**: `SerialNumberKardex`  
**Action**: `Reports/SerialNumberKardex`

### Purpose
Full movement history for a specific serial number.

### Filters
- Serial number (required)

### Columns
- Date, document type, document folio, warehouse, quantity (+ or −)

---

## 28. Products by Supplier Report

**SystemObject**: `ProductsBySupplierReport`  
**Action**: `Reports/ProductsBySupplier`

### Purpose
Which products belong to each supplier (default supplier assignment).

### Filters
- Supplier

### Columns
- Supplier, product code, name, brand, model, min order qty, unit of measure

---

## 29. Products Orders and Refunds by Salesperson

**SystemObject**: `ProductsOrdersAndRefundsBySalesPerson`  
**Action**: `Reports/ProductsOrderAndRefundsBySalesPerson`

### Purpose
Per product per salesperson: units sold, units refunded, net units.

### Columns
- Salesperson, product, qty sold, qty refunded, net qty, gross sales, refunds, net sales

---

## 30. Store Movements Summary

**SystemObject**: `StoreMovementsSummary`  
**Action**: `Reports/StoreMovementsSummary`

### Purpose
All inventory movements (receipts, issues, transfers, sales) summarized by store and period.

### Columns
- Store, movement type, document count, total quantity, total value

---

## 31. Customer Debt Report

**SystemObject**: `CustomerDebtReport`  
**Action**: `Reports/CustomersDebtSummary`

### Purpose
Outstanding balances per customer with aging. Counterpart of the AR module view, formatted for printing.

### Columns
- Customer, total balance, current, 1–30 overdue, 31–60, 61–90, 90+

---

## 32. Received Payments Summary

**SystemObject**: `ReceivedPaymentsSummary`  
**Action**: `Reports/ReceivedPaymentsSummary`

### Purpose
Totals of received payments grouped by method and store for a period.

### Columns
- Store, payment method, count, total amount

---

## 33. Commissions by Salesperson

**SystemObject**: `CommissionsBySalesPerson`  
**Action**: `Reports/CommissionsBySalesPerson`

### Purpose
Calculated commissions per salesperson from `commissions_history`, showing earned vs. confirmed vs. paid amounts.

### Filters
- Salesperson, date range, confirmed (yes/no/all)

### Columns
- Salesperson, order count, total sales, commission rate, commission earned, confirmed, paid

### Key Table
- `commissions_history`

---

## 34. Download CSV Files

**SystemObject**: `DownloadCSVFiles`  
**Action**: `Reports/DownloadCSV`

### Purpose
Bulk data export in CSV format for external processing (accounting, BI tools).

### Available Exports
- Sales orders
- Sales order details
- Customer payments
- Products catalog
- Stock levels
- Fiscal documents

### Features
- Date range selection per export type
- Store filter
- Immediate download trigger (no preview)
