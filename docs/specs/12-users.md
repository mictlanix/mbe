# Users Specs

User management is accessible only to administrators (`user.administrator = true`). It controls login accounts, role-based access, and default context settings.

---

## 1. User Management

**Route**: `GET /users`  
**Controller**: `UsersController`  
**SystemObject**: `Users` (92)  
**Access**: Administrator only — only appears in menu when `CurrentUser.IsAdministrator = true`

### Purpose
Manage system login accounts. Each user is linked to an employee and granted per-module access privileges. There is no separate Create action in the controller; new users are created via the `Account/Register` flow (first-time setup) or directly via `Edit`.

### List View
- Search by: username, email, employee first name, employee last name
- Columns: username, email, linked employee, administrator flag, disabled flag

### Form Fields (`user`)

| Field | Column | Type | Notes |
|-------|--------|------|-------|
| Username | `user_id` | `varchar(20)` | Alphanumeric only (`^[0-9a-zA-Z]+$`), 4–20 chars, PK — immutable after creation |
| Password | `password` | `varchar(40)` | Stored as SHA1 hex digest; enter plain text in form; NOT updated in the Edit action |
| Email | `email` | `varchar(100)` | Contact email, lowercased on save |
| Linked Employee | `employee` | FK → `employee` | Associates account with a person |
| Administrator | `administrator` | `tinyint(1)` | `true` = full access, bypasses all privilege checks |
| Disabled | `disabled` | `tinyint(1)` | Blocks login without deleting account |

### Actions
- **Edit**: saves privilege rows and increments `session_version` (see Session Invalidation). Password is NOT changed here.
- **Delete**: hard-deletes `user_settings` → all `access_privilege` rows → `user` record (cascaded in code, not by FK constraint)
- **Change Password**: separate `Account/ChangePassword` flow using old password verification
- **Password Recovery**: admin-triggered reset generates a random password, stores as SHA1, emails recovery link containing the raw SHA1 hash as token

### Session Invalidation
Every `Edit` save increments `user.session_version`. Any JWT or session token carrying an older `session_version` value is considered invalid, forcing immediate re-authentication.

### Incidence Logging
All edit and delete operations write an `incidence` record with `source_type = SourceType.UserSettings`.

---

## 2. Access Privileges

Managed as a sub-panel within the user edit form.

### Purpose
Fine-grained CRUD control per module (SystemObject). On every Edit save, the controller iterates all `SystemObjects` enum values and upserts one `access_privilege` row per object: creates the row if absent, updates it if existing.

### Table: `access_privilege`

| Field | Column | Notes |
|-------|--------|-------|
| User | `user` | FK → `user.user_id` |
| System Object | `object` | Integer from `SystemObjects` enum |
| Privileges | `privileges` | Bitmask integer |

### Privilege Bitmask (`AccessRight` enum — `[Flags]`)

| Value | Name | Meaning |
|-------|------|---------|
| 0 | `None` | No access |
| 1 | `Create` | Can create new records |
| 2 | `Read` | Can view the module list/detail |
| 4 | `Update` | Can edit existing records |
| 8 | `Delete` | Can delete / cancel records |

Values combine by bitwise OR: `privileges = 7` (binary `0111`) = Create + Read + Update (no Delete). `privileges = 15` = full access.

> **Important**: `AllowRead = (privileges & 2) != 0`, NOT bit 0. Historically documented specs had this inverted.

Computed helpers on `AccessPrivilege` model: `AllowCreate`, `AllowRead`, `AllowUpdate`, `AllowDelete`.

### UI Pattern
Display a grid: rows = SystemObjects, columns = Create / Read / Update / Delete checkboxes. Administrators see the grid greyed out (their access is unconditional).

---

## 3. SystemObjects Reference

Complete enum (integer value → name). Controls every module and sub-feature. Values without a visible menu entry still gate specific actions or views.

| Value | Name | Module / Feature |
|-------|------|-----------------|
| 0 | `Products` | Master Data → Products |
| 1 | `Labels` | Master Data → Labels |
| 2 | `Customers` | Master Data → Customers |
| 3 | `Suppliers` | Master Data → Suppliers |
| 4 | `Warehouses` | Master Data → Warehouses |
| 5 | `PriceLists` | Master Data → Price Lists |
| 6 | `Employees` | Master Data → Employees |
| 7 | `SalesOrders` | Sales → Sales Orders |
| 8 | `CustomerPayments` | Sales → Customer Payments |
| 9 | `PointsOfSale` | Master Data → Points of Sale |
| 10 | `CashDrawers` | Master Data → Cash Drawers |
| 11 | `Addresses` | Sub-feature: address management (inline) |
| 12 | `Contacts` | Sub-feature: contact management (inline) |
| 13 | `BankAccounts` | Sub-feature: bank accounts |
| 14 | `SupplierAgreements` | Purchases → Supplier Agreements |
| 15 | `InventoryReceipts` | Inventory → Receipts |
| 16 | `InventoryIssues` | Inventory → Issues |
| 17 | `InventoryTransfers` | Inventory → Transfers |
| 18 | `AccountsReceivable` | Administration → AR |
| 19 | `AccountsPayable` | Administration → AP |
| 20 | `PurchasesOrders` | Purchases → Purchase Orders |
| 21 | `SupplierPayment` | Purchases → Supplier Payments |
| 22 | `CustomerRefunds` | Sales → Customer Refunds |
| 23 | `FiscalDocuments` | Fiscal → Fiscal Documents |
| 24 | `Taxpayers` | Fiscal → Taxpayers |
| 25 | `SupplierReturns` | Purchases → Supplier Returns |
| 26 | `SalesOrdersHistoric` | Reports → Sales Orders Historic |
| 27 | `CustomerRefundsHistoric` | Reports → Customer Refunds Historic |
| 28 | `SupplierReturnHistoric` | Reports → Supplier Return Historic |
| 29 | `Stores` | Master Data → Stores |
| 30 | `SalesQuotes` | Sales → Sales Quotes |
| 32 | `Kardex` | Reports → Kardex |
| 33 | `ReceivedPayments` | Reports → Received Payments |
| 34 | `SalesByCustomer` | Reports → Sales by Customer |
| 35 | `SalesBySalesPerson` | Reports → Sales by Salesperson |
| 36 | `SalesByProduct` | Reports → Sales by Product |
| 37 | `GrossProfitsByCustomer` | Reports → Gross Profits by Customer |
| 38 | `GrossProfitsBySalesPerson` | Reports → Gross Profits by Salesperson |
| 39 | `GrossProfitsByProduct` | Reports → Gross Profits by Product |
| 40 | `BestSellingProductsByCustomer` | Reports → Best Selling by Customer |
| 41 | `BestSellingProductsBySalesPerson` | Reports → Best Selling by Salesperson |
| 42 | `LotSerialNumbers` | Inventory → Lot/Serial Numbers |
| 43 | `ExchangeRates` | Master Data → Exchange Rates |
| 44 | `POS` | Sales → POS Terminal |
| 45 | `SerialNumberKardex` | Reports → Serial Number Kardex |
| 46 | `CustomerDebtReport` | Reports → Customer Debt |
| 47 | `SalesOrderSummaryReport` | Reports → Sales Order Summary |
| 48 | `FiscalDocumentsReport` | Reports → Fiscal Documents |
| 49 | `SalesPersonOrdersReport` | Reports → Salesperson Orders |
| 50 | `CustomerSalesOrdersReport` | Reports → Customer Sales Orders |
| 51 | `ProductSalesByCustomerReport` | Reports → Product Sales by Customer |
| 52 | `ProductSalesByModelReport` | Reports → Product Sales by Model |
| 53 | `ProductSalesByBrandReport` | Reports → Product Sales by Brand |
| 54 | `TaxpayerRecipients` | Fiscal → Taxpayer Recipients |
| 55 | `ProductSalesBySalesPerson` | Reports → Product Sales by Salesperson |
| 56 | `StandaloneFiscalDocuments` | Fiscal → Standalone Fiscal Docs |
| 57 | `ProductionOrders` | Production → Production Orders |
| 58 | `TechnicalServiceReports` | Technical Service → Service Reports |
| 59 | `TranslationRequests` | Front Desk → Translation Requests |
| 60 | `Notarizations` | Front Desk → Notarizations |
| 61 | `ProductSalesBySalesPersonAndLabel` | Reports → Product Sales by Salesperson+Label |
| 62 | `ProductSalesBySalesPersonAndBrand` | Reports → Product Sales by Salesperson+Brand |
| 63 | `ProductSalesBySalesPersonAndModel` | Reports → Product Sales by Salesperson+Model |
| 64 | `TechnicalServiceRequests` | Technical Service → Service Requests |
| 65 | `TechnicalServiceReceipts` | Technical Service → Service Receipts |
| 66 | `CustomersReport` | Reports → Customers Report |
| 67 | `WarehouseStockReport` | Reports → Warehouse Stock |
| 68 | `WarehouseStockByLotReport` | Reports → Warehouse Stock by Lot |
| 69 | `WarehouseStockBySerialNumberReport` | Reports → Warehouse Stock by Serial |
| 71 | `DeliveryOrders` | Logistics → Delivery Orders |
| 72 | `SalesPersonOrdersAndRefundsReport` | Reports → Salesperson Orders & Refunds |
| 73 | `ProductsMerge` | Master Data → Products Merge (hidden action) |
| 74 | `PhysicalCountAdjustment` | Inventory → Physical Count Adjustment |
| 75 | `ProductsBySupplierReport` | Reports → Products by Supplier |
| 79 | `ProductsOrdersAndRefundsBySalesPerson` | Reports → Product Orders & Refunds by Salesperson |
| 80 | `PendantDeliveries` | Logistics → Pending Deliveries |
| 81 | `Expenses` | Administration → Expenses |
| 82 | `ExpenseTicket` | Administration → Expense Ticket |
| 83 | `CreditPayments` | Sales → Credit Payments |
| 84 | `PaymentMethodOptions` | Master Data → Payment Method Options |
| 85 | `PaymentReceipt` | Sales → Payment Receipt (print action) |
| 86 | `PurchaseRequest` | Purchases → Purchase Requests |
| 87 | `DeliveryItineraries` | Logistics → Delivery Itineraries |
| 88 | `Vehicle` | Master Data → Vehicles |
| 89 | `VehicleOperators` | Master Data → Vehicle Operators |
| 90 | `VehicleServiceOrders` | Logistics → Vehicle Service Orders |
| 91 | `ForDeliver` | Logistics → For Delivery view |
| 92 | `Users` | Users (admin only menu item) |
| 93 | `InventoryAdjustments` | Inventory → Adjustments |
| 94 | `DeliveryOrderApproval` | Logistics → Delivery Order Approval |
| 95 | `PurchaseOrderApproval` | Purchases → Purchase Order Approval |
| 96 | `PurchaseRequestApproval` | Purchases → Purchase Request Approval |
| 97 | `ReceivedPaymentsAdvancedSearchFilter` | Sales → Received Payments advanced filter |
| 98 | `CreditCustomerConfiguration` | Administration → Credit Customer Config |
| 99 | `StoreMovementsSummary` | Reports → Store Movements Summary |
| 100 | `PaymentsEditor` | Administration → Payments Editor |
| 101 | `SearchCreditsFromAllStores` | Sales → Search Credits Across Stores |
| 102 | `ExcludePriceRangeValidation` | Sales → Skip Price Range Check (permission) |
| 103 | `IssuedLocationId` | Fiscal → Override Issued Location |
| 106 | `Pricing` | Master Data → Pricing (special access) |
| 107 | `ProductionSites` | Master Data → Production Sites |
| 108 | `PaymentsVerification` | Administration → Payments Verification |
| 109 | `ReceivedPaymentsSummary` | Reports → Received Payments Summary |
| 110 | `CustomerRefundConfirm` | Sales → Confirm Refund (action gate) |
| 111 | `CashSessionClose` | Sales → Close Cash Session |
| 112 | `CommissionsBySalesPerson` | Reports → Commissions by Salesperson |
| 113 | `DownloadCSVFiles` | Reports → Download CSV Files |

> Values 31, 70, 76–78 are absent from the enum (gaps in the sequence). Do not assign values to these gaps in the Python migration.

---

## 4. User Settings

Each user has a default operating context stored in `user_settings` (one-to-one with `user`).

| Field | Column | Notes |
|-------|--------|-------|
| Store | `store` | FK → `store` — default store for transactions |
| Point of Sale | `point_sale` | FK → `point_of_sale` — default POS terminal |
| Cash Drawer | `cash_drawer` | FK → `cash_drawer` — default cash drawer |

### `UserSettingsMode` (`WebConfig.UserSettingsMode`)

| Mode | Behavior |
|------|----------|
| `Managed` | Store/POS/drawer are set by the admin in the Edit screen and loaded into session at login. User cannot change them. |
| `SelfService` | User selects their own store/POS/drawer from a self-service screen after login. |

In `Managed` mode, `user_settings` is populated at login time from the stored `user_settings` record. In `SelfService` mode, the values are set interactively per session.

---

## 5. Authentication Flow (Target — Python API)

### Login
1. Client sends `username` + `password` (plain text over HTTPS).
2. API computes `SHA1(password)` and compares to `user.password`.
3. If match and `disabled = 0`: issue JWT containing `user_id`, `session_version`, `administrator`, `store_id`.
4. On each authenticated request, verify `session_version` in JWT matches `user.session_version` in DB. Mismatch = force logout.

### Session Invalidation
Incrementing `user.session_version` immediately invalidates all existing JWTs for that user. This happens on every `Edit` save in `UsersController`.

### Authorization
- Middleware checks `access_privilege` for the requested route's `SystemObjects` value.
- Administrators bypass all privilege checks.
- Missing `access_privilege` row = no access (deny by default).
- Check pattern: `user.IsAdministrator || GetPrivilege(systemObject).AllowRead`.

### Password Storage Migration
Current storage is SHA1 (no salt, weak). During rewrite:
- On successful SHA1 login, rehash with bcrypt/argon2 and store the new hash.
- Mark migrated accounts with a `password_scheme` column (`sha1` vs `bcrypt`).
- After migration window, reject any remaining SHA1-only accounts.

### Password Change (Account level)
- `Account/ChangePassword`: requires `oldPassword` verification before setting new password.
- `Account/RecoverPassword`: admin triggers a random password reset; recovery link contains the raw SHA1 as token — **do not replicate this pattern**; use a signed time-limited token instead.
