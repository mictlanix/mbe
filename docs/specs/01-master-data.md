# Master Data Specs

These screens manage the core catalog / configuration data used by all transactional modules.

---

## 1. Products

**Route**: `GET /products`  
**Controller**: `ProductsController`  
**SystemObject**: `Products` (0)

### Purpose
Central product/SKU catalog. Every inventory movement, sale, and purchase references a product.

### List View
- Search by: **name, code, model, SKU, brand** (not barcode; not supplier)
- Filters: active/deactivated, stockable, salable, purchasable, label
- Columns: code, name, brand, model, unit of measurement, tax rate, deactivated

### Field Validation Constraints (from `Product.cs`)

| Field | Constraint |
|-------|-----------|
| `code` | No whitespace, 1–25 characters, unique |
| `name` | 4–250 characters |
| `bar_code` | Exactly 13 digits (EAN-13) or empty |

### Detail / Form Fields

| Field | Table Column | Notes |
|-------|-------------|-------|
| Code | `product.code` | No whitespace, 1–25 chars, unique, required |
| Name | `product.name` | 4–250 chars, required |
| Photo | `product.photo` | Image path; default = `WebConfig.DefaultPhotoFile` |
| SKU | `product.sku` | Manufacturer SKU |
| Brand | `product.brand` | Free text |
| Model | `product.model` | Free text |
| Barcode | `product.bar_code` | EAN-13 (exactly 13 digits) |
| Bin Location | `product.location` | Shelf/bin code |
| Unit of Measurement | `product.unit_of_measurement` | FK → `sat_unit_of_measurement` |
| SAT Product Key | `product.key` | FK → `sat_product_service` |
| Tax Rate | `product.tax_rate` | Decimal 0–1; default = `WebConfig.DefaultVAT` |
| Tax Included | `product.tax_included` | Boolean; default = `WebConfig.IsTaxIncluded` |
| Price Type | `product.price_type` | Enum: `Fixed` or `Variable` only; default = `WebConfig.DefaultPriceType` |
| Currency | `product.currency` | Default currency |
| Min Order Qty | `product.min_order_qty` | Integer; default = **1** on create |
| Default Supplier | `product.supplier` | FK → `supplier` |
| Stockable | `product.stockable` | Boolean |
| Perishable | `product.perishable` | Lot/expiration tracking |
| Seriable | `product.seriable` | Serial number tracking |
| Purchasable | `product.purchasable` | Boolean |
| Salable | `product.salable` | Boolean |
| Invoiceable | `product.invoiceable` | Boolean |
| Stock Required | `product.stock_required` | Boolean; default = **true** on create — blocks sale when out of stock |
| Notes | `product.comment` | Free text |

### Sub-Panels
- **Prices** (`product_price`): one row per `price_list`. Columns: list name, price, low profit threshold, high profit threshold.
- **Labels** (`product_label`): many-to-many tag assignment.

### Create Behavior
When a product is created, the controller automatically:
1. Sets `MinimumOrderQuantity = 1`
2. Sets `TaxRate = WebConfig.DefaultVAT`
3. Sets `IsTaxIncluded = WebConfig.IsTaxIncluded`
4. Sets `PriceType = WebConfig.DefaultPriceType`
5. Sets `Photo = WebConfig.DefaultPhotoFile`
6. Sets `StockRequired = true`
7. Creates one `product_price` row for **every existing `price_list`** (price = 0 until edited)
8. Logs an `Incidence` record (`SourceType.Product`)

### Delete Behavior
Deletion is a **soft delete** combined with a price cleanup:
1. Sets `product.disabled = true` (soft delete — product hidden from new transactions)
2. **Hard-deletes all `product_price` rows** for this product
3. Logs an `Incidence` record (`SourceType.Product`)

Product is excluded from `IQProducts` queryable once `disabled = true`.

### Merge Feature
**Route**: `POST /products/merge`  
**Requires**: `SystemObjects.ProductsMerge` (73) with `AllowCreate` privilege

Merges a duplicate product into a canonical product:
1. Remaps all FK references from `duplicate` → `product` across 13 tables via raw SQL
2. Hard-deletes the duplicate `product` record (no soft delete)
3. Used to clean up data-entry errors where the same item was entered twice

Tables remapped during merge include: `sales_order_detail`, `purchase_order_detail`, `inventory_receipt_detail`, `inventory_issue_detail`, `inventory_transfer_detail`, `product_price`, `lot_serial_tracking`, `product_label`, and others.

### Incidence Logging
Create, Edit, and Delete all write an `incidence` record with `source_type = SourceType.Product`.

### Business Rules
- `code` must be unique across all products (including disabled ones).
- If `stockable = false`, stock reports exclude this product.
- If `perishable = true`, lot/expiry must be entered on inventory receipts; FIFO by expiration date.
- If `seriable = true`, serial number must be entered on receipts and tracked through sales.
- `disabled = true` hides product from new orders/purchases but retains all historical data.
- A new `PriceList` added after the product was created does NOT automatically add a `product_price` row — must be added manually.

---

## 2. Price Lists

**Route**: `GET /price-lists`  
**Controller**: `PriceListsController`  
**SystemObject**: `PriceLists` (5)

### Purpose
Named pricing tiers assigned to customers. Each product has a price per list. Creating a product auto-creates a `product_price` row for every existing price list.

### List View
- Columns: name, high profit margin %, low profit margin %

### Form Fields

| Field | Column | Notes |
|-------|--------|-------|
| Name | `price_list.name` | Required, unique |
| High Profit Margin | `price_list.high_profit_margin` | Max markup as decimal (e.g. `0.40` = 40%) |
| Low Profit Margin | `price_list.low_profit_margin` | Min markup |

### Business Rules
- A price list cannot be deleted if assigned to any customer.
- On POS/Sales Order, selling below `low_profit_margin` triggers a warning.
- When a new price list is created, existing products do NOT automatically get a `product_price` row — this must be handled explicitly during migration.

---

## 3. Customers

**Route**: `GET /customers`  
**Controller**: `CustomersController`  
**SystemObject**: `Customers` (2)

### Purpose
Buyer entities. All sales orders, payments, and refunds belong to a customer.

### List View
- Search by: **code, name, zone** (not salesperson in search — salesperson is a filter, not a text search field)
- Filters: active/disabled
- Columns: code, name, zone, credit limit, credit days, price list, salesperson

### Form Fields

| Field | Column | Notes |
|-------|--------|-------|
| Code | `customer.code` | Unique |
| Name | `customer.name` | Required |
| Zone | `customer.zone` | Sales territory |
| Credit Limit | `customer.credit_limit` | Monetary |
| Credit Days | `customer.credit_days` | Net payment days |
| Price List | `customer.price_list` | FK → `price_list` |
| Salesperson | `customer.salesperson` | FK → `employee` (`sales_person=1`) — **optional** |
| Shipping Enabled | `customer.shipping` | Boolean |
| Shipping Requires Document | `customer.shipping_required_document` | Boolean |
| Notes | `customer.comment` | |

### Sub-Panels
- **Addresses** (`customer_address`): list of shipping/billing addresses.
- **Contacts** (`customer_contact`): list of contact persons.
- **Taxpayers** (`customer_taxpayer`): RFC(s) associated for invoicing.
- **Discounts** (`customer_discount`): product-specific discount overrides.

### Business Rules
- **Cannot delete `WebConfig.DefaultCustomer`**: the system-default customer (used for anonymous POS sales) is protected from deletion. Attempting to delete it returns an error.
- A customer at `credit_limit` cannot place new credit orders until balance is reduced.
- `disabled` customers cannot appear on new orders.
- `salesperson` is optional; some customers are walk-ins with no assigned rep.

---

## 4. Labels

**Route**: `GET /labels`  
**SystemObject**: `Labels` (1)

### Purpose
Classification tags applied to products for reporting and filtering.

### Form Fields

| Field | Column | Notes |
|-------|--------|-------|
| Name | `label.name` | Required, unique |
| Notes | `label.comment` | |

---

## 5. Taxpayer Recipients

**Route**: `GET /taxpayer-recipients`  
**SystemObject**: `TaxpayerRecipients` (54)

### Purpose
RFC records for customer entities used as CFDI invoice recipients. A customer may have multiple RFCs (individual vs. company).

### List View
- Search by: RFC, name

### Form Fields

| Field | Column | Notes |
|-------|--------|-------|
| RFC | `taxpayer_recipient.taxpayer_recipient_id` | 12–13 chars, PK, unique |
| Name | `taxpayer_recipient.name` | Legal name |
| Email | `taxpayer_recipient.email` | For CFDI delivery |
| Postal Code | `taxpayer_recipient.postal_code` | FK → `sat_postal_code` |
| Tax Regime | `taxpayer_recipient.regime` | FK → `sat_tax_regime` |

---

## 6. Suppliers

**Route**: `GET /suppliers`  
**SystemObject**: `Suppliers` (3)

### Purpose
Vendor entities for purchasing goods.

### List View
- Search by: code, name, zone

### Form Fields

| Field | Column | Notes |
|-------|--------|-------|
| Code | `supplier.code` | Unique |
| Name | `supplier.name` | Required |
| Zone | `supplier.zone` | Geographic territory |
| Credit Limit | `supplier.credit_limit` | |
| Credit Days | `supplier.credit_days` | Net payment days |
| Notes | `supplier.comment` | |

### Sub-Panels
- **Addresses** (`supplier_address`)
- **Contacts** (`supplier_contact`)
- **Bank Accounts** (`supplier_bank_account`)
- **Agreements** (`supplier_agreement`): date-ranged commercial agreements

---

## 7. Employees

**Route**: `GET /employees`  
**SystemObject**: `Employees` (6)

### Purpose
Internal staff records. Employees link to user accounts and appear as creators, updaters, salespersons, and cashiers.

### List View
- Search by: name, nickname
- Filters: active/disabled, salesperson

### Form Fields

| Field | Column | Notes |
|-------|--------|-------|
| First Name | `employee.first_name` | |
| Last Name | `employee.last_name` | |
| Nickname | `employee.nickname` | Short display name |
| Gender | `employee.gender` | Enum |
| Birthday | `employee.birthday` | |
| RFC | `employee.taxpayer_id` | Mexican tax ID |
| CURP / ID | `employee.personal_id` | National ID |
| Hire Date | `employee.start_job_date` | |
| Is Salesperson | `employee.sales_person` | Boolean — enables assignment as `customer.salesperson` |
| Enroll Number | `employee.enroll_number` | Biometric device ID |
| Notes | `employee.comment` | |
| Active | `employee.active` | |

---

## 8. Warehouses

**Route**: `GET /warehouses`  
**SystemObject**: `Warehouses` (4)

### Purpose
Physical stock locations. Inventory receipts, issues, and transfers all specify a warehouse.

### Form Fields

| Field | Column | Notes |
|-------|--------|-------|
| Store | `warehouse.store` | FK → `store` |
| Code | `warehouse.code` | Unique |
| Name | `warehouse.name` | |
| Notes | `warehouse.comment` | |
| Disabled | `warehouse.disabled` | |

---

## 9. Points of Sale

**Route**: `GET /points-of-sale`  
**SystemObject**: `PointsOfSale` (9)

### Purpose
POS terminal configurations. Each POS is tied to a store and a warehouse (stock source for POS sales).

### Form Fields

| Field | Column | Notes |
|-------|--------|-------|
| Store | `point_sale.store` | FK → `store` |
| Code | `point_sale.code` | Unique |
| Name | `point_sale.name` | |
| Warehouse | `point_sale.warehouse` | FK → `warehouse` |
| Notes | `point_sale.comment` | |
| Disabled | `point_sale.disabled` | |

---

## 10. Cash Drawers

**Route**: `GET /cash-drawers`  
**SystemObject**: `CashDrawers` (10)

### Purpose
Physical cash drawer hardware tied to a store. Cashier sessions are opened per drawer.

### Form Fields

| Field | Column | Notes |
|-------|--------|-------|
| Store | `cash_drawer.store` | FK → `store` |
| Code | `cash_drawer.code` | Unique |
| Name | `cash_drawer.name` | |
| Notes | `cash_drawer.comment` | |
| Disabled | `cash_drawer.disabled` | |

---

## 11. Stores

**Route**: `GET /stores`  
**SystemObject**: `Stores` (29)

### Purpose
Top-level organizational unit (branch). All transactional data belongs to a store.

### Form Fields

| Field | Column | Notes |
|-------|--------|-------|
| Code | `store.code` | |
| Name | `store.name` | |
| SAT Location | `store.location` | FK → `sat_postal_code` |
| Address | `store.address` | FK → `address` |
| Taxpayer (RFC) | `store.taxpayer` | FK → `taxpayer_issuer` |
| Logo | `store.logo` | Image path |
| Receipt Message | `store.receipt_message` | Printed footer |
| Default Batch | `store.default_batch` | CFDI folio series |
| Disabled | `store.disabled` | |

---

## 12. Exchange Rates

**Route**: `GET /exchange-rates`  
**SystemObject**: `ExchangeRates` (43)

### Purpose
Daily currency exchange rates used to convert foreign-currency orders.

### List View
- Filter by: date range, base/target currency
- Columns: date, base currency, target currency, rate

### Form Fields

| Field | Column | Notes |
|-------|--------|-------|
| Date | `exchange_rate.date` | Unique per base+target pair per day |
| Rate | `exchange_rate.rate` | Decimal |
| Base Currency | `exchange_rate.base` | |
| Target Currency | `exchange_rate.target` | |

---

## 13. Expenses

**Route**: `GET /expenses`  
**SystemObject**: `Expenses` (81)

### Purpose
Expense category catalog used in expense vouchers (petty cash).

### Form Fields

| Field | Column | Notes |
|-------|--------|-------|
| Name | `expenses.expense` | Category label |
| Notes | `expenses.comment` | |

---

## 14. Payment Method Options

**Route**: `GET /payment-method-options`  
**SystemObject**: `PaymentMethodOptions` (84)

### Purpose
Configures accepted payment methods per store, including installment options and merchant commission rates.

### Form Fields

| Field | Column | Notes |
|-------|--------|-------|
| Store | `payment_method_option.store` | FK → `store` |
| Warehouse | `payment_method_option.warehouse` | Optional scope |
| Name | `payment_method_option.name` | Display label |
| Payment Method | `payment_method_option.payment_method` | Enum: Cash, Card, Transfer, etc. |
| Installments | `payment_method_option.number_of_payments` | 1 = single payment |
| Show on Ticket | `payment_method_option.display_on_ticket` | Boolean |
| Commission | `payment_method_option.commission` | Merchant surcharge rate |
| Enabled | `payment_method_option.enabled` | |

---

## 15. Vehicles

**Route**: `GET /vehicles`  
**SystemObject**: `Vehicle` (88)

### Purpose
Fleet vehicle registry used for delivery itineraries and maintenance orders.

### Form Fields

| Field | Column | Notes |
|-------|--------|-------|
| License Plate | `vehicle.license_plate` | Unique |
| Name | `vehicle.name` | Description |
| Nickname | `vehicle.nickname` | Short alias |
| Tons Capacity | `vehicle.tons_capacity` | Load capacity |
| Active | `vehicle.active` | |

---

## 16. Vehicle Operators

**Route**: `GET /vehicle-operators`  
**SystemObject**: `VehicleOperators` (89)

### Purpose
Driver license records for employees who operate fleet vehicles.

### Form Fields

| Field | Column | Notes |
|-------|--------|-------|
| Driver | `vehicle_operator.driver` | FK → `employee` |
| License Type | `vehicle_operator.license_type` | Category code |
| License Number | `vehicle_operator.driver_license_number` | |
| Issue Date | `vehicle_operator.issue_date` | |
| Expiration Date | `vehicle_operator.expiration_date` | |
| Issuing Location | `vehicle_operator.issuing_location` | State |
| Active | `vehicle_operator.active` | |

### Business Rules
- Alert when license is within 30 days of expiration.
- Expired operators cannot be assigned to delivery itineraries (advisory check in controller).

---

## 17. Production Sites

**Route**: `GET /production-sites`  
**SystemObject**: `ProductionSites` (107)

### Purpose
Manufacturing site / production line configuration.

### Form Fields

| Field | Column | Notes |
|-------|--------|-------|
| Store | `production_site.store` | FK → `store` |
| Code | `production_site.code` | Unique |
| Name | `production_site.name` | |
| Notes | `production_site.comment` | |
| Disabled | `production_site.disabled` | |
