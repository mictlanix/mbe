# Reports Specs

All reports are read-only analytical views. They share common filter patterns and export to CSV/PDF. Each maps to a `SystemObject` that controls read access.

---

## Common UI Patterns

- **Date Range**: start date / end date selector (defaults to current month); passed as `DateRange dates`
- **Store Filter**: single or all stores (admin sees all; user sees their store)
- **Export**: CSV download and printable PDF for all reports
- **Pagination**: server-side paging for large result sets

---

## 1. Received Payments

**SystemObject**: `ReceivedPayments` (33)  
**Action**: `Reports/ReceivedPayments`

### Purpose
List all customer payments received within a date range.

### Filters
- Store, date range, payment method, customer, salesperson, cash session
- Advanced filter: gated by `ReceivedPaymentsAdvancedSearchFilter` (97) privilege — enables additional filter fields

### Columns
- Date, folio, customer, salesperson, method, reference, amount, currency, cash session, verified by

### Key Tables
- `customer_payment`, `customer`, `employee`, `cash_session`

---

## 2. Sales by Product

**SystemObject**: `SalesByProduct` (36)  
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

**SystemObject**: `GrossProfitsByCustomer` (37)  
**Action**: `Reports/GrossProfitsByCustomer`

### Purpose
Revenue, cost, and gross profit margin per customer.

### Filters
- Store, date range

### Columns
- Customer, total sales, total cost, gross profit, margin %

---

## 4. Gross Profits by Salesperson

**SystemObject**: `GrossProfitsBySalesPerson` (38)  
**Action**: `Reports/GrossProfitsBySalesPerson`

### Purpose
Revenue and gross profit per salesperson.

### Filters
- Store, date range

### Columns
- Salesperson, order count, total sales, total cost, gross profit, margin %

---

## 5. Gross Profits by Product

**SystemObject**: `GrossProfitsByProduct` (39)  
**Action**: `Reports/GrossProfitsByProduct`

### Purpose
Margin analysis at the product level.

### Filters
- Store, date range

### Columns
- Product code, name, quantity sold, revenue, cost, gross profit, margin %

---

## 6. Best Selling Products by Customer

**SystemObject**: `BestSellingProductsByCustomer` (40)  
**Action**: `Reports/BestSellingProductsByCustomer`

### Purpose
For a selected customer, which products were purchased most.

### Filters
- Customer (required), date range

### Columns
- Product code, name, quantity, revenue, last purchase date

---

## 7. Best Selling Products by Salesperson

**SystemObject**: `BestSellingProductsBySalesPerson` (41)  
**Action**: `Reports/BestSellingProductsBySalesPerson`

### Purpose
For a selected salesperson, top-selling products.

### Filters
- Salesperson/employee (required), date range

---

## 8. Customers Report

**SystemObject**: `CustomersReport` (66)  
**Action**: `Reports/CustomersReport`

### Purpose
Customer directory / profile export with search.

### Columns
- Code, name, zone, credit limit, credit days, price list, salesperson, disabled

---

## 9. Sales by Customer

**SystemObject**: `SalesByCustomer` (34)  
**Action**: `Reports/SalesByCustomer`

### Purpose
Total sales per customer within a period.

### Filters
- Store, date range

### Columns
- Customer, order count, total quantity, total revenue, total paid, balance

---

## 10. Sales by Salesperson

**SystemObject**: `SalesBySalesPerson` (35)  
**Action**: `Reports/SalesBySalesPerson`

### Purpose
Total sales per salesperson.

### Filters
- Store, date range

### Columns
- Salesperson, order count, total revenue, total cost, gross profit, margin %

---

## 11. Customer Sales Orders Report

**SystemObject**: `CustomerSalesOrdersReport` (50)  
**Action**: `Reports/CustomerSalesOrders`

### Purpose
Detailed order listing for a specific customer.

### Filters
- Customer (required), date range

### Columns
- Folio, date, due date, total, paid, balance, status

---

## 12. Salesperson Orders Report

**SystemObject**: `SalesPersonOrdersReport` (49)  
**Action**: `Reports/SalesPersonOrders`

### Purpose
Order listing for a specific salesperson.

### Filters
- Salesperson/employee (required), date range

### Columns
- Order folio, date, customer, total, balance, status

---

## 13. Salesperson Orders and Refunds Report

**SystemObject**: `SalesPersonOrdersAndRefundsReport` (72)  
**Action**: `Reports/SalesPersonOrdersAndRefunds`

### Purpose
Net sales per salesperson after deducting refunds.

### Filters
- Salesperson/employee (required), date range

### Columns
- Salesperson, gross sales, total refunds, net sales, gross profit

---

## 14. Product Sales by Customer Report

**SystemObject**: `ProductSalesByCustomerReport` (51)  
**Action**: `Reports/ProductSalesByCustomer`

### Purpose
Cross-tab: which customers bought a specific product.

### Filters
- Customer (required), date range

### Columns
- Product code, name, quantity, revenue, last purchase date

---

## 15. Product Sales by Brand Report

**SystemObject**: `ProductSalesByBrandReport` (53)  
**Action**: `Reports/ProductSalesByBrand`

### Purpose
Revenue and quantity grouped by product brand.

### Filters
- Brand (required), date range

### Columns
- Brand, product count, total quantity, total revenue

---

## 16. Product Sales by Model Report

**SystemObject**: `ProductSalesByModelReport` (52)  
**Action**: `Reports/ProductSalesByModel`

### Purpose
Revenue and quantity grouped by product model.

### Filters
- Model (required), date range

---

## 17. Product Sales by Salesperson

**SystemObject**: `ProductSalesBySalesPerson` (55)  
**Action**: `Reports/ProductSalesBySalesPerson`

### Purpose
Which products each salesperson sold most.

### Filters
- Salesperson/employee (required), date range

---

## 18. Product Sales by Salesperson and Label

**SystemObject**: `ProductSalesBySalesPersonAndLabel` (61)  
**Action**: `Reports/ProductSalesBySalesPersonAndLabel`

### Purpose
Product sales per salesperson, segmented by product label (category).

### Filters
- Salesperson/employee (required), label (optional), date range

---

## 19. Product Sales by Salesperson and Brand

**SystemObject**: `ProductSalesBySalesPersonAndBrand` (62)  
**Action**: `Reports/ProductSalesBySalesPersonAndBrand`

### Purpose
Product sales per salesperson, segmented by brand.

### Filters
- Salesperson/employee (required), brand, date range

---

## 20. Product Sales by Salesperson and Model

**SystemObject**: `ProductSalesBySalesPersonAndModel` (63)  
**Action**: `Reports/ProductSalesBySalesPersonAndModel`

### Purpose
Product sales per salesperson, segmented by model.

### Filters
- Salesperson/employee (required), model, date range

---

## 21. Sales Order Summary Report

**SystemObject**: `SalesOrderSummaryReport` (47)  
**Action**: `Reports/SalesOrderSummary`

### Purpose
High-level summary: total orders, revenue, collected, and balance within a period — grouped by day or month.

### Filters
- Store, date range

### Columns
- Period, order count, total revenue, total paid, balance, refunds, net

---

## 22. Sales Details Summary

**Action**: `Reports/SalesDetailsSummary`  
*(No dedicated SystemObject — gated by standard reporting access)*

### Purpose
Line-level sales detail breakdown within a period. Shows individual order lines rather than order totals.

### Filters
- Store, date range

### Columns
- Order folio, date, customer, salesperson, product code, name, quantity, unit price, discount, tax rate, line total

---

## 23. Fiscal Documents Report

**SystemObject**: `FiscalDocumentsReport` (48)  
**Action**: `Reports/FiscalDocuments`

### Purpose
List of all CFDI documents issued, with stamping status and totals.

### Filters
- Issuer RFC (taxpayer), date range

### Columns
- UUID, series-folio, date, recipient RFC, recipient name, subtotal, VAT, total, status (stamped/cancelled)

---

## 24. Warehouse Stock Report

**SystemObject**: `WarehouseStockReport` (67)  
**Action**: `Reports/WarehouseStockReport`

### Purpose
Current stock levels per product per warehouse.

### Filters
- Warehouse, label, brand, product model; `showZeroInventory` boolean toggle

### Columns
- Warehouse, product code, name, unit, quantity on hand, unit cost, total value

### Key Calculation
Stock = net `SUM(lot_serial_tracking.quantity)` grouped by warehouse + product.

---

## 25. Warehouse Stock by Lot Report

**SystemObject**: `WarehouseStockByLotReport` (68)  
**Action**: `Reports/WarehouseStockByLotReport`

### Purpose
Stock broken down by lot number and expiration date. Critical for perishable products.

### Filters
- Warehouse, brand

### Columns
- Warehouse, product, lot number, expiration date, quantity

---

## 26. Warehouse Stock by Serial Number Report

**SystemObject**: `WarehouseStockBySerialNumberReport` (69)  
**Action**: `Reports/WarehouseStockBySerialNumberReport`

### Purpose
Stock broken down by individual serial number. For seriable products.

### Filters
- Warehouse

### Columns
- Warehouse, product, serial number, location

---

## 27. Warehouse Restock

**Action**: `Reports/WarehouseReStock`  
*(No dedicated SystemObject — gated by standard warehouse access)*

### Purpose
Lists products in a warehouse that are below minimum stock levels, helping purchasing agents identify what needs to be reordered.

### Filters
- Warehouse (required)

### Columns
- Product code, name, current stock, minimum order quantity, suggested reorder quantity

---

## 28. Kardex

**SystemObject**: `Kardex` (32)  
**Action**: `Reports/Kardex`

### Purpose
Full movement history (ledger) for a product in a warehouse — all ins and outs with running balance.

### Filters
- Product (required), warehouse, date range

### Columns
- Date, document type (`TransactionType`), document ID, quantity in, quantity out, balance, unit cost, total cost

---

## 29. Serial Number Kardex

**SystemObject**: `SerialNumberKardex` (45)  
**Action**: `Reports/SerialNumberKardex`

### Purpose
Full movement history for a specific serial number.

### Filters
- Product (required), warehouse, date range

### Columns
- Date, document type, document ID, warehouse, quantity (+ or −)

---

## 30. Products by Supplier Report

**SystemObject**: `ProductsBySupplierReport` (75)  
**Action**: `Reports/ProductsBySupplier`

### Purpose
Which products belong to each supplier (via `product.supplier` default supplier assignment).

### Filters
- Supplier (required)

### Columns
- Supplier, product code, name, brand, model, min order qty, unit of measure

---

## 31. Products Orders and Refunds by Salesperson

**SystemObject**: `ProductsOrdersAndRefundsBySalesPerson` (79)  
**Action**: `Reports/ProductsOrderAndRefundsBySalesPerson`

### Purpose
Per product per salesperson: units sold, units refunded, net units.

### Filters
- Salesperson/employee (required), date range

### Columns
- Salesperson, product, qty sold, qty refunded, net qty, gross sales, refunds, net sales

---

## 32. Store Movements Summary

**SystemObject**: `StoreMovementsSummary` (99)  
**Action**: `Reports/StoreMovementsSummary`

### Purpose
All inventory movements (receipts, issues, transfers, sales) summarized by store and period.

### Filters
- Store, date range

### Columns
- Store, movement type, document count, total quantity, total value

---

## 33. Customer Debt Report

**SystemObject**: `CustomerDebtReport` (46)  
**Action**: `Reports/CustomersDebtSummary`

### Purpose
Outstanding balances per customer with aging. Comprehensive view with filter options for credit status.

### Filters
- `CustomersStatusFilter`: all / with balance / overdue / expired credit

### Actions from this report
- **Print per Customer** (`PrintCustomerDebtReport`): individual debt statement for one customer
- **Customer Debt Detail** (`CustomerDebtReport`): drill-down to one customer's orders

### Columns
- Customer, total balance, current, 1–30 days overdue, 31–60, 61–90, 90+

---

## 34. Credit and Collection

**Action**: `Reports/CreditAndCollection`  
*(No dedicated SystemObject — typically restricted to supervisor role)*

### Purpose
Collection performance report: shows credit sales, payments collected, and collection rate within a period. Used by the collections team to track progress.

### Filters
- Date range

### Key Table
- Uses `CreditAndCollectionSummary` computed model from `sales_order`, `customer_payment`, `customer`

---

## 35. Customer 360

**Action**: `Reports/Customer360`  
*(No dedicated SystemObject)*

### Purpose
360-degree view of a single customer: combines purchase history, payment history, and product mix in one view.

### Filters
- Customer (required)

---

## 36. Received Payments Summary

**SystemObject**: `ReceivedPaymentsSummary` (109)  
**Action**: `Reports/ReceivedPaymentsSummary`

### Purpose
Totals of received payments grouped by method and store for a period.

### Filters
- Date range

### Columns
- Store, payment method, count, total amount

---

## 37. Commissions by Salesperson

**SystemObject**: `CommissionsBySalesPerson` (112)  
**Action**: `Reports/CommissionsBySalesPerson`

### Purpose
Calculated commissions per salesperson from `commissions_history`, showing earned vs. confirmed vs. paid amounts.

### Filters
- Salesperson/employee (optional — all if omitted), date range

### Actions
- **Print Commission Ticket** (`PrintCommissionTicket`): individual commission breakdown per agent + date range

### Columns
- Salesperson, order count, total sales, commission rate, commission earned, confirmed, paid

### Key Table
- `commissions_history`

---

## 38. Download CSV Files

**SystemObject**: `DownloadCSVFiles` (113)  
**Action**: `Reports/DownloadCSV`

### Purpose
Bulk data export in CSV format for external processing (accounting, BI tools).

### Available Exports

| Export | Action | Content |
|--------|--------|---------|
| Sales CSV | `SalesCsv` | Sales orders + order details for date range |
| Purchases CSV | `PurchasesCsv` | Purchase orders + details for date range |
| Payments CSV | `PaymentsCsv` | Customer payments for date range |

### Features
- Date range selection
- Immediate download trigger (no preview)
- Returns `FileStreamResult` with `text/csv` content type
