# Data Dictionary

Generated from `mbe_schema.sql` — MariaDB 10.11, database `mbe`.

---

## Table of Contents

1. [Security & Users](#1-security--users)
2. [Core Reference](#2-core-reference)
3. [Products & Pricing](#3-products--pricing)
4. [Customers](#4-customers)
5. [Suppliers](#5-suppliers)
6. [Sales](#6-sales)
7. [Inventory](#7-inventory)
8. [Purchases](#8-purchases)
9. [Logistics](#9-logistics)
10. [Fiscal Documents](#10-fiscal-documents)
11. [Technical Service](#11-technical-service)
12. [Front Desk](#12-front-desk)
13. [Commissions](#13-commissions)
14. [SAT Catalogs](#14-sat-catalogs)
15. [Computed / View Tables](#15-computed--view-tables)

---

## 1. Security & Users

### `user`
System login account.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `user_id` | varchar(20) | NO | Username / login identifier (PK) |
| `password` | varchar(40) | NO | SHA1 hashed password |
| `email` | varchar(250) | NO | Contact email |
| `employee` | int(11) | YES | Linked employee record |
| `administrator` | bit(1) | NO | Full admin flag — bypasses all privilege checks |
| `session_version` | int(11) | NO | Incremented to invalidate existing sessions |
| `disabled` | tinyint(1) | NO | Soft-delete flag |

### `access_privilege`
Per-user, per-object (SystemObject enum) permission bits (read/write/etc.).

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `access_privilege_id` | int(11) | NO | PK |
| `user` | varchar(20) | NO | FK → `user.user_id` |
| `object` | int(11) | NO | SystemObject enum value (menu entry identifier) |
| `privileges` | int(11) | NO | Bitmask: AllowRead, AllowCreate, AllowEdit, AllowDelete |

### `user_settings`
Per-user default store/POS/cash drawer context.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `user` | varchar(20) | NO | FK → `user.user_id` (PK) |
| `store` | int(11) | NO | FK → `store.store_id` — default store |
| `point_sale` | int(11) | YES | FK → `point_sale.point_sale_id` — default POS terminal |
| `cash_drawer` | int(11) | YES | FK → `cash_drawer.cash_drawer_id` — default drawer |

---

## 2. Core Reference

### `store`
Top-level organizational unit (branch / location).

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `store_id` | int(11) | NO | PK |
| `code` | varchar(25) | NO | Short code |
| `name` | varchar(250) | NO | Display name |
| `location` | varchar(5) | NO | FK → `sat_postal_code` — SAT location code |
| `address` | int(11) | NO | FK → `address.address_id` |
| `taxpayer` | varchar(13) | NO | FK → `taxpayer_issuer` — RFC of issuing entity |
| `logo` | varchar(255) | NO | Path/URL to logo image |
| `receipt_message` | varchar(250) | YES | Printed on receipts |
| `default_batch` | varchar(10) | YES | Default CFDI fiscal batch/folio series |
| `disabled` | tinyint(1) | YES | Soft-delete |

### `warehouse`
Physical storage location belonging to a store.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `warehouse_id` | int(11) | NO | PK |
| `store` | int(11) | NO | FK → `store.store_id` |
| `code` | varchar(25) | NO | Unique short code |
| `name` | varchar(250) | NO | Display name |
| `comment` | varchar(500) | YES | Notes |
| `disabled` | tinyint(4) | YES | Soft-delete |

### `point_sale`
Point-of-sale terminal configuration.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `point_sale_id` | int(11) | NO | PK |
| `store` | int(11) | NO | FK → `store.store_id` |
| `code` | varchar(25) | NO | Unique code |
| `name` | varchar(250) | NO | Display name |
| `warehouse` | int(11) | NO | FK → `warehouse` — stock source for POS sales |
| `comment` | varchar(500) | YES | Notes |
| `disabled` | tinyint(1) | YES | Soft-delete |

### `cash_drawer`
Physical cash drawer device.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `cash_drawer_id` | int(11) | NO | PK |
| `store` | int(11) | NO | FK → `store.store_id` |
| `code` | varchar(25) | NO | Unique code |
| `name` | varchar(250) | NO | Display name |
| `comment` | varchar(500) | YES | Notes |
| `disabled` | tinyint(1) | YES | Soft-delete |

### `cash_session`
A cashier's work shift on a given cash drawer.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `cash_session_id` | int(11) | NO | PK |
| `start` | datetime | NO | Session open time |
| `end` | datetime | YES | Session close time (null = still open) |
| `cashier` | int(11) | NO | FK → `employee` |
| `cash_drawer` | int(11) | NO | FK → `cash_drawer` |
| `cash_supervisor` | int(11) | YES | FK → `employee` — supervisor who closed session |

### `cash_count`
Denomination breakdown recorded at session close.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `cash_count_id` | int(11) | NO | PK |
| `session` | int(11) | NO | FK → `cash_session` |
| `denomination` | decimal(18,4) | NO | Bill/coin value |
| `quantity` | int(11) | NO | Count of that denomination |
| `type` | int(11) | NO | Enum: Bill, Coin, etc. |

### `address`
Reusable address record (used by customers, suppliers, stores, fiscal docs).

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `address_id` | int(11) | NO | PK |
| `nickname` | char(100) | YES | Friendly label (e.g. "Main Office") |
| `type` | int(11) | NO | Enum: Fiscal, Shipping, etc. |
| `street` | varchar(150) | NO | Street name |
| `exterior_number` | varchar(25) | NO | Exterior number |
| `interior_number` | varchar(25) | YES | Interior number/suite |
| `postal_code` | varchar(5) | NO | 5-digit Mexican postal code |
| `neighborhood` | varchar(100) | NO | Colonia |
| `locality` | varchar(100) | YES | Localidad |
| `borough` | varchar(50) | NO | Municipio/Delegación |
| `state` | varchar(50) | NO | State name |
| `city` | varchar(50) | YES | City (if different from borough) |
| `country` | varchar(50) | NO | Country name |
| `url_address` | varchar(200) | YES | Google Maps or similar URL |
| `comment` | varchar(500) | YES | Notes |
| `disabled` | tinyint(1) | YES | Soft-delete |

### `contact`
Person contact record (shared between customers and suppliers).

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `contact_id` | int(11) | NO | PK |
| `name` | varchar(250) | NO | Full name |
| `job_title` | varchar(100) | YES | Position/title |
| `phone` | varchar(25) | YES | Office phone |
| `phone_ext` | varchar(5) | YES | Extension |
| `mobile` | varchar(25) | NO | Mobile phone |
| `fax` | varchar(25) | YES | Fax number |
| `website` | varchar(80) | YES | Website URL |
| `email` | varchar(80) | YES | Email |
| `im` | varchar(80) | YES | Instant messaging handle |
| `sip` | varchar(80) | YES | SIP/VoIP address |
| `birthday` | date | YES | Date of birth |
| `comment` | varchar(500) | YES | Notes |

### `employee`
Internal staff member.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `employee_id` | int(11) | NO | PK |
| `first_name` | varchar(100) | NO | First name |
| `last_name` | varchar(100) | NO | Last name |
| `nickname` | varchar(50) | NO | Short display name |
| `gender` | tinyint(4) | NO | Enum: Male, Female |
| `birthday` | date | NO | Date of birth |
| `taxpayer_id` | varchar(13) | YES | RFC (Mexican tax ID) |
| `sales_person` | tinyint(1) | NO | Whether this employee is a salesperson |
| `active` | tinyint(1) | NO | Active employee flag |
| `personal_id` | varchar(18) | YES | CURP or national ID |
| `start_job_date` | date | NO | Hire date |
| `comment` | varchar(500) | YES | Notes |
| `enroll_number` | int(11) | YES | Biometric enrollment ID |
| `disabled` | tinyint(1) | YES | Soft-delete |

### `exchange_rate`
Daily currency exchange rates.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `exchange_rate_id` | int(11) | NO | PK |
| `date` | date | NO | Rate date |
| `rate` | decimal(8,4) | NO | Exchange rate value |
| `base` | int(11) | NO | Base currency (FK → sat_currency implied) |
| `target` | int(11) | NO | Target currency |

### `expenses`
Expense category catalog.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `expense_id` | int(11) | NO | PK |
| `expense` | varchar(100) | NO | Expense category name |
| `comment` | varchar(500) | YES | Notes |

### `payment_method_option`
Configured payment method options per store (cash, card, transfer, etc.).

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `payment_method_option_id` | int(11) | NO | PK |
| `warehouse` | int(11) | YES | Optional warehouse scope |
| `store` | int(11) | NO | FK → `store.store_id` |
| `name` | varchar(50) | NO | Display label |
| `number_of_payments` | tinyint(4) | NO | Installments (1 = single) |
| `display_on_ticket` | tinyint(1) | NO | Show on printed receipt |
| `payment_method` | int(11) | NO | Enum: Cash, Card, Transfer, Check, etc. |
| `commission` | decimal(10,3) | NO | Surcharge rate applied to merchant |
| `enabled` | tinyint(1) | NO | Active flag |

### `bank_account`
Bank account for supplier payments.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `bank_account_id` | int(11) | NO | PK |
| `bank_name` | varchar(250) | NO | Bank name |
| `account_number` | varchar(20) | NO | Account number |
| `reference` | varchar(20) | YES | CLABE or routing reference |
| `routing_number` | varchar(18) | YES | Routing/SWIFT |
| `comment` | varchar(500) | YES | Notes |

### `label`
Product classification tag (many-to-many with products).

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `label_id` | int(11) | NO | PK |
| `name` | varchar(250) | NO | Label name |
| `comment` | varchar(500) | YES | Notes |

### `postal_code`
Mexican postal code catalog (neighborhood lookup).

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `postal_code_id` | int(11) | NO | PK |
| `code` | int(5) | NO | 5-digit code |
| `neighborhood` | varchar(150) | NO | Colonia |
| `borough` | varchar(50) | NO | Municipio |
| `state` | varchar(50) | NO | State |
| `city` | varchar(50) | YES | City |
| `country` | varchar(50) | NO | Country |

### `production_site`
Manufacturing site / production line location.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `production_site_id` | int(11) | NO | PK |
| `store` | int(11) | NO | FK → `store.store_id` |
| `code` | varchar(25) | NO | Unique code |
| `name` | varchar(250) | NO | Display name |
| `comment` | varchar(500) | YES | Notes |
| `disabled` | tinyint(4) | YES | Soft-delete |

### `vehicle`
Fleet vehicle record.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `vehicle_id` | int(11) | NO | PK |
| `license_plate` | varchar(8) | NO | Plate number (unique) |
| `name` | varchar(50) | NO | Vehicle description |
| `nickname` | varchar(30) | NO | Short alias |
| `tons_capacity` | tinyint(4) | NO | Load capacity in metric tons |
| `active` | tinyint(1) | NO | Active flag |

### `vehicle_operator`
Driver license record for an employee.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `vehicle_operator_id` | int(11) | NO | PK |
| `driver` | int(11) | NO | FK → `employee` |
| `license_type` | varchar(3) | NO | License category |
| `driver_license_number` | varchar(15) | NO | License number |
| `issue_date` | date | NO | Issue date |
| `expiration_date` | date | NO | Expiry date |
| `issuing_location` | varchar(30) | NO | Issuing state/city |
| `creation_time` | datetime | NO | Record created |
| `modification_time` | datetime | NO | Last updated |
| `creator` | int(11) | NO | FK → `employee` |
| `updater` | int(11) | NO | FK → `employee` |
| `active` | tinyint(1) | NO | Active flag |

---

## 3. Products & Pricing

### `product`
Product/SKU catalog.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `product_id` | int(11) | NO | PK |
| `code` | varchar(25) | NO | Internal product code |
| `name` | varchar(250) | NO | Product name |
| `photo` | varchar(255) | YES | Photo path |
| `sku` | varchar(50) | YES | Manufacturer SKU |
| `brand` | varchar(100) | YES | Brand name |
| `model` | varchar(100) | YES | Model name |
| `bar_code` | char(13) | YES | EAN-13 barcode |
| `location` | varchar(50) | YES | Bin/shelf location code |
| `unit_of_measurement` | varchar(3) | NO | FK → `sat_unit_of_measurement` |
| `stockable` | tinyint(1) | NO | Whether inventory is tracked |
| `perishable` | tinyint(1) | NO | Has lot/expiration tracking |
| `seriable` | tinyint(1) | NO | Has serial number tracking |
| `purchasable` | tinyint(1) | NO | Can be purchased from supplier |
| `salable` | tinyint(1) | NO | Can be sold to customer |
| `invoiceable` | tinyint(1) | NO | Can appear on fiscal invoices |
| `tax_rate` | decimal(7,6) | NO | Default VAT rate (e.g. 0.160000 = 16%) |
| `tax_included` | tinyint(1) | NO | Whether price includes tax |
| `price_type` | tinyint(4) | NO | Enum: Fixed, Percentage-of-cost, etc. |
| `currency` | int(11) | NO | Default currency |
| `min_order_qty` | int(11) | NO | Minimum purchase quantity |
| `comment` | varchar(500) | YES | Notes |
| `supplier` | int(11) | YES | FK → `supplier` — default supplier |
| `key` | varchar(8) | YES | FK → `sat_product_service` — SAT product key |
| `deactivated` | tinyint(1) | NO | Soft-delete flag |
| `stock_verification` | tinyint(1) | NO | Require stock check before selling |

### `price_list`
Named price tier.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `price_list_id` | int(11) | NO | PK |
| `name` | varchar(250) | NO | List name (e.g. "Retail", "Wholesale") |
| `high_profit_margin` | decimal(5,4) | NO | Maximum allowed margin above cost |
| `low_profit_margin` | decimal(5,4) | NO | Minimum allowed margin above cost |

### `product_price`
Price per product per price list.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `product_price_id` | int(11) | NO | PK |
| `product` | int(11) | NO | FK → `product` |
| `list` | int(11) | NO | FK → `price_list` |
| `price` | decimal(18,4) | NO | Configured sale price |
| `low_profit` | decimal(20,6) | NO | Minimum price (gross margin floor) |
| `high_profit` | decimal(20,6) | NO | Maximum price (gross margin ceiling) |

### `product_label`
Many-to-many product ↔ label.

| Column | Type | Description |
|--------|------|-------------|
| `product` | int(11) | FK → `product` |
| `label` | int(11) | FK → `label` |

---

## 4. Customers

### `customer`
Customer / buyer entity.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `customer_id` | int(11) | NO | PK |
| `code` | varchar(25) | NO | Internal code |
| `name` | varchar(250) | NO | Legal or display name |
| `zone` | varchar(250) | YES | Sales zone / territory |
| `credit_limit` | decimal(18,4) | NO | Maximum outstanding credit balance |
| `credit_days` | int(11) | NO | Net days payment terms |
| `comment` | varchar(1024) | YES | Notes |
| `price_list` | int(11) | NO | FK → `price_list` — assigned price tier |
| `shipping` | tinyint(1) | NO | Whether deliveries are enabled |
| `shipping_required_document` | tinyint(1) | NO | Require delivery document |
| `salesperson` | int(11) | YES | FK → `employee` — assigned sales rep |
| `disabled` | tinyint(1) | YES | Soft-delete |
| `creator` | int(11) | YES | FK → `employee` |

### `customer_address`
Many-to-many customer ↔ address.

### `customer_contact`
Many-to-many customer ↔ contact.

### `customer_discount`
Per-product custom discount for a customer.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `customer_discount_id` | int(11) | NO | PK |
| `customer` | int(11) | NO | FK → `customer` |
| `product` | int(11) | NO | FK → `product` |
| `discount` | decimal(9,8) | NO | Discount rate (0–1) |

### `customer_taxpayer`
Many-to-many customer ↔ taxpayer_recipient (RFC for invoicing).

### `taxpayer_recipient`
RFC (tax ID) of a customer entity, used as invoice recipient.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `taxpayer_recipient_id` | varchar(13) | NO | RFC (PK) |
| `name` | varchar(250) | YES | Legal name |
| `email` | varchar(80) | NO | Email for CFDI delivery |
| `postal_code` | varchar(5) | YES | SAT postal code |
| `regime` | varchar(3) | YES | FK → `sat_tax_regime` |

---

## 5. Suppliers

### `supplier`
Vendor / supplier entity.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `supplier_id` | int(11) | NO | PK |
| `code` | varchar(25) | NO | Internal code |
| `name` | varchar(250) | NO | Supplier name |
| `zone` | varchar(250) | YES | Geographic zone |
| `credit_limit` | decimal(18,4) | NO | Credit limit |
| `credit_days` | int(11) | NO | Payment terms in days |
| `comment` | varchar(500) | YES | Notes |

### `supplier_address`
Many-to-many supplier ↔ address.

### `supplier_contact`
Many-to-many supplier ↔ contact.

### `supplier_bank_account`
Many-to-many supplier ↔ bank_account.

### `supplier_agreement`
Supplier commercial agreement date range.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `supplier_agreement_id` | int(11) | NO | PK |
| `supplier` | int(11) | NO | FK → `supplier` |
| `start` | date | NO | Agreement start date |
| `end` | date | NO | Agreement end date |
| `comment` | varchar(500) | YES | Notes |

### `supplier_payment`
Payment made to a supplier.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `supplier_payment_id` | int(11) | NO | PK |
| `supplier` | int(11) | NO | FK → `supplier` |
| `amount` | decimal(18,4) | NO | Amount paid |
| `method` | int(11) | NO | Payment method enum |
| `date` | datetime | NO | Payment date |
| `reference` | varchar(50) | YES | Transfer/check reference |
| `comment` | varchar(500) | YES | Notes |
| `creator` | int(11) | NO | FK → `employee` |

### `supplier_return`
Return of goods to supplier, linked to a purchase order.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `supplier_return_id` | int(11) | NO | PK |
| `purchase_order` | int(11) | NO | FK → `purchase_order` |
| `creator` | int(11) | NO | FK → `employee` |
| `updater` | int(11) | NO | FK → `employee` |
| `supplier` | int(11) | NO | FK → `supplier` |
| `creation_time` | datetime | NO | Created at |
| `modification_time` | datetime | NO | Last updated |
| `completed` | tinyint(1) | NO | Finalized flag |
| `cancelled` | tinyint(1) | NO | Cancelled flag |

### `supplier_return_detail`
Line items of a supplier return.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `supplier_return_detail_id` | int(11) | NO | PK |
| `supplier_return` | int(11) | NO | FK → `supplier_return` |
| `purchase_order_detail` | int(11) | NO | FK → `purchase_order_detail` |
| `product` | int(11) | NO | FK → `product` |
| `warehouse` | int(11) | YES | FK → `warehouse` |
| `quantity` | decimal(18,4) | NO | Returned quantity |
| `price` | decimal(18,4) | NO | Unit price at return |
| `product_code` | varchar(25) | NO | Snapshot of product code |
| `product_name` | varchar(250) | NO | Snapshot of product name |
| `tax_rate` | decimal(5,4) | NO | VAT rate |
| `discount` | decimal(9,8) | NO | Discount applied |
| `exchange_rate` | decimal(8,4) | NO | FX rate at time of return |
| `currency` | int(11) | NO | Currency |
| `tax_included` | tinyint(1) | NO | Price includes tax flag |

---

## 6. Sales

### `sales_quote`
Customer quotation / presale document.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `sales_quote_id` | int(11) | NO | PK |
| `store` | int(11) | NO | FK → `store` |
| `serial` | int(11) | YES | Sequential folio within store |
| `date` | datetime | NO | Quote date |
| `salesperson` | int(11) | NO | FK → `employee` |
| `customer` | int(11) | NO | FK → `customer` |
| `payment_terms` | tinyint(4) | NO | Enum: Cash, 30-day credit, etc. |
| `due_date` | datetime | NO | Quote expiry date |
| `completed` | tinyint(1) | NO | Converted to order flag |
| `cancelled` | tinyint(1) | NO | Cancelled flag |
| `creator` | int(11) | NO | FK → `employee` |
| `updater` | int(11) | NO | FK → `employee` |
| `creation_time` | datetime | NO | Created at |
| `modification_time` | datetime | NO | Last updated |
| `contact` | int(11) | YES | FK → `contact` — customer contact |
| `ship_to` | int(11) | YES | FK → `address` — delivery address |
| `comment` | varchar(1024) | YES | Notes |
| `currency` | int(11) | NO | Currency |
| `exchange_rate` | decimal(8,4) | NO | FX rate at quote date |

### `sales_quote_detail`
Line items of a quote.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `sales_quote_detail_id` | int(11) | NO | PK |
| `sales_quote` | int(11) | NO | FK → `sales_quote` |
| `product` | int(11) | NO | FK → `product` |
| `quantity` | decimal(18,4) | NO | Quoted quantity |
| `price` | decimal(18,7) | NO | Unit price |
| `price_adjustment` | decimal(18,7) | NO | Manual price override delta |
| `discount_rate` | decimal(9,8) | NO | Line discount (0–1) |
| `tax_rate` | decimal(5,4) | NO | VAT rate |
| `product_code` | varchar(25) | NO | Snapshot of code |
| `product_name` | varchar(250) | NO | Snapshot of name |
| `exchange_rate` | decimal(8,4) | NO | FX rate |
| `currency` | int(11) | NO | Currency |
| `tax_included` | tinyint(1) | NO | Price includes tax |
| `comment` | varchar(1024) | YES | Line note |

### `sales_order`
Confirmed sales order (invoice source).

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `sales_order_id` | int(11) | NO | PK |
| `store` | int(11) | NO | FK → `store` |
| `serial` | int(11) | YES | Folio within store (unique with store) |
| `point_sale` | int(11) | NO | FK → `point_sale` |
| `salesperson` | int(11) | NO | FK → `employee` |
| `customer` | int(11) | NO | FK → `customer` |
| `sales_quote` | int(11) | YES | FK → `sales_quote` — originating quote |
| `payment_terms` | tinyint(4) | NO | Payment terms enum |
| `date` | datetime | NO | Order date |
| `promise_date` | datetime | NO | Promised delivery date |
| `recipient` | varchar(13) | YES | RFC for invoicing |
| `recipient_name` | varchar(250) | YES | Recipient name (fiscal) |
| `recipient_address` | int(11) | YES | FK → `address` — fiscal address |
| `due_date` | datetime | NO | Payment due date |
| `completed` | tinyint(1) | NO | Fully processed flag |
| `cancelled` | tinyint(1) | NO | Cancelled flag |
| `paid` | tinyint(1) | NO | Fully paid flag |
| `creator` | int(11) | NO | FK → `employee` |
| `updater` | int(11) | NO | FK → `employee` |
| `creation_time` | datetime | NO | Created at |
| `modification_time` | datetime | NO | Last updated |
| `balance_zeroed_time` | datetime | YES | When balance was manually zeroed |
| `contact` | int(11) | YES | FK → `contact` |
| `ship_to` | int(11) | YES | FK → `address` — shipping address |
| `delivered` | tinyint(1) | NO | Delivery completed flag |
| `comment` | varchar(500) | YES | Notes |
| `currency` | int(11) | NO | Currency |
| `exchange_rate` | decimal(8,4) | NO | FX rate |
| `customer_name` | varchar(100) | YES | Override customer name on docs |
| `customer_shipto` | varchar(200) | YES | Override ship-to text |
| `priority` | tinyint(3) | NO | 1=Normal, 2=High, etc. |
| `partial_deliveries` | tinyint(2) | YES | Allow partial shipments |

### `sales_order_detail`
Line items of a sales order.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `sales_order_detail_id` | int(11) | NO | PK |
| `sales_order` | int(11) | NO | FK → `sales_order` |
| `product` | int(11) | NO | FK → `product` |
| `quantity` | decimal(18,4) | NO | Ordered quantity |
| `cost` | decimal(18,4) | NO | Unit cost at time of sale |
| `price` | decimal(18,7) | NO | Unit sale price |
| `discount_rate` | decimal(9,8) | NO | Discount (0–1) |
| `tax_rate` | decimal(5,4) | NO | VAT rate |
| `product_code` | varchar(25) | NO | Snapshot of code |
| `product_name` | varchar(250) | NO | Snapshot of name |
| `delivery` | tinyint(1) | NO | Requires delivery flag |
| `warehouse` | int(11) | YES | FK → `warehouse` — fulfillment source |
| `exchange_rate` | decimal(8,4) | NO | FX rate |
| `currency` | int(11) | NO | Currency |
| `tax_included` | tinyint(1) | NO | Price includes tax |
| `comment` | varchar(500) | YES | Line note |

### `customer_payment`
Payment received from a customer.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `customer_payment_id` | int(11) | NO | PK |
| `amount` | decimal(18,4) | NO | Payment amount |
| `method` | int(11) | NO | Payment method enum |
| `commission` | decimal(10,4) | YES | Merchant commission |
| `payment_charge` | int(11) | YES | FK → `payment_method_option` |
| `date` | datetime | NO | Payment date |
| `cash_session` | int(11) | YES | FK → `cash_session` — session when received |
| `reference` | varchar(50) | YES | Transfer/check reference number |
| `customer` | int(11) | NO | FK → `customer` |
| `store` | int(11) | NO | FK → `store` |
| `serial` | int(11) | NO | Sequential folio within store |
| `creator` | int(11) | NO | FK → `employee` |
| `updater` | int(11) | NO | FK → `employee` |
| `verifier` | int(11) | YES | FK → `employee` — verifying supervisor |
| `creation_time` | datetime | NO | Created at |
| `modification_time` | datetime | NO | Last updated |
| `currency` | int(11) | NO | Currency |
| `payment_type` | tinyint(2) | NO | Enum: Normal, Credit, OnDelivery |

### `sales_order_payment`
Application of a customer payment to a specific order (many-to-many).

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `sales_order_payment_id` | int(11) | NO | PK |
| `sales_order` | int(11) | NO | FK → `sales_order` |
| `customer_payment` | int(11) | NO | FK → `customer_payment` |
| `amount` | decimal(18,4) | NO | Amount applied to this order |
| `amount_change` | decimal(18,4) | NO | Change given back |
| `applier` | int(11) | YES | FK → `employee` — who applied it |
| `date` | datetime | YES | Application date |
| `confirmed` | tinyint(1) | YES | Supervisor confirmation |
| `cancelled` | tinyint(1) | NO | Cancelled/reversed |

### `customer_refund`
Customer return (devolution) header.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `customer_refund_id` | int(11) | NO | PK |
| `sales_order` | int(11) | NO | FK → `sales_order` — original order |
| `customer` | int(11) | YES | FK → `customer` |
| `creator` | int(11) | NO | FK → `employee` |
| `updater` | int(11) | NO | FK → `employee` |
| `sales_person` | int(11) | NO | FK → `employee` |
| `creation_time` | datetime | NO | Created at |
| `modification_time` | datetime | NO | Last updated |
| `completed` | tinyint(1) | NO | Finalized |
| `cancelled` | tinyint(1) | NO | Cancelled |
| `store` | int(11) | NO | FK → `store` |
| `serial` | int(11) | YES | Folio |
| `date` | datetime | YES | Refund date |
| `currency` | int(11) | NO | Currency |
| `exchange_rate` | decimal(8,4) | NO | FX rate |

### `customer_refund_detail`
Line items of a customer refund.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `customer_refund_detail_id` | int(11) | NO | PK |
| `customer_refund` | int(11) | NO | FK → `customer_refund` |
| `sales_order_detail` | int(11) | NO | FK → `sales_order_detail` — original line |
| `quantity` | decimal(18,4) | NO | Returned quantity |
| `product` | int(11) | NO | FK → `product` |
| `price` | decimal(18,4) | NO | Unit price at refund |
| `product_code` | varchar(25) | NO | Snapshot |
| `product_name` | varchar(250) | NO | Snapshot |
| `tax_rate` | decimal(5,4) | NO | VAT rate |
| `discount` | decimal(9,8) | NO | Discount |
| `exchange_rate` | decimal(8,4) | NO | FX rate |
| `currency` | int(11) | NO | Currency |
| `tax_included` | tinyint(1) | NO | Price includes tax |
| `warehouse` | int(11) | YES | FK → `warehouse` — return-to warehouse |

### `credit_note`
Credit note issued when a refund results in credit balance.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `credit_note_id` | int(11) | NO | PK |
| `sales_order` | int(11) | NO | FK → `sales_order` |
| `customer_refund` | int(11) | NO | FK → `customer_refund` |
| `customer_payment` | int(11) | NO | FK → `customer_payment` — credit applied as payment |
| `customer` | int(11) | NO | FK → `customer` |
| `refunded` | decimal(20,6) | NO | Credit amount |
| `cash_session` | int(11) | YES | FK → `cash_session` |
| `date` | datetime | YES | Date |

### `payment_on_delivery`
Payment collected at delivery (COD).

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `payment_on_delivery_id` | int(11) | NO | PK |
| `customer_payment` | int(11) | NO | FK → `customer_payment` |
| `cash_session` | int(11) | YES | FK → `cash_session` |
| `paid` | tinyint(1) | NO | Collected flag |
| `date` | datetime | NO | Collection date |

---

## 7. Inventory

### `inventory_receipt`
Goods received into a warehouse.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `inventory_receipt_id` | int(11) | NO | PK |
| `store` | int(11) | NO | FK → `store` |
| `serial` | int(11) | YES | Folio |
| `creation_time` | datetime | NO | Created at |
| `modification_time` | datetime | NO | Last updated |
| `creator` | int(11) | NO | FK → `employee` |
| `updater` | int(11) | NO | FK → `employee` |
| `warehouse` | int(11) | NO | FK → `warehouse` — destination |
| `purchase_order` | int(11) | YES | FK → `purchase_order` — if from PO |
| `completed` | tinyint(1) | NO | Posted to stock |
| `cancelled` | tinyint(1) | NO | Cancelled |
| `comment` | varchar(500) | YES | Notes |

### `inventory_receipt_detail`
Line items of an inventory receipt.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `receipt_detail_id` | int(11) | NO | PK |
| `receipt` | int(11) | NO | FK → `inventory_receipt` |
| `product` | int(11) | NO | FK → `product` |
| `purchase_order_detail` | int(11) | YES | FK → `purchase_order_detail` |
| `quantity` | decimal(18,4) | NO | Received quantity |
| `product_code` | varchar(25) | YES | Snapshot |
| `product_name` | varchar(250) | YES | Snapshot |
| `quantity_ordered` | decimal(18,4) | NO | Ordered quantity (for comparison) |

### `inventory_issue`
Goods issued out of a warehouse.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `inventory_issue_id` | int(11) | NO | PK |
| `store` | int(11) | NO | FK → `store` |
| `serial` | int(11) | YES | Folio |
| `creation_time` | datetime | NO | Created at |
| `modification_time` | datetime | NO | Last updated |
| `creator` | int(11) | NO | FK → `employee` |
| `updater` | int(11) | NO | FK → `employee` |
| `warehouse` | int(11) | NO | FK → `warehouse` — source |
| `completed` | tinyint(1) | NO | Posted |
| `cancelled` | tinyint(1) | NO | Cancelled |
| `comment` | varchar(500) | YES | Notes |
| `supplier_return` | int(11) | YES | FK → `supplier_return` — if for return |

### `inventory_issue_detail`
Line items of an inventory issue.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `issue_detail_id` | int(11) | NO | PK |
| `issue` | int(11) | NO | FK → `inventory_issue` |
| `product` | int(11) | NO | FK → `product` |
| `quantity` | decimal(18,4) | NO | Issued quantity |
| `product_code` | varchar(25) | YES | Snapshot |
| `product_name` | varchar(250) | YES | Snapshot |

### `inventory_transfer`
Stock movement between two warehouses.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `inventory_transfer_id` | int(11) | NO | PK |
| `store` | int(11) | NO | FK → `store` |
| `serial` | int(11) | YES | Folio |
| `creation_time` | datetime | NO | Created at |
| `modification_time` | datetime | NO | Last updated |
| `creator` | int(11) | NO | FK → `employee` |
| `updater` | int(11) | NO | FK → `employee` |
| `warehouse` | int(11) | NO | FK → `warehouse` — source |
| `warehouse_to` | int(11) | NO | FK → `warehouse` — destination |
| `completed` | tinyint(1) | NO | Posted |
| `cancelled` | tinyint(1) | NO | Cancelled |
| `comment` | varchar(500) | YES | Notes |

### `inventory_transfer_detail`
Line items of a transfer.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `transfer_detail_id` | int(11) | NO | PK |
| `transfer` | int(11) | NO | FK → `inventory_transfer` |
| `product` | int(11) | NO | FK → `product` |
| `quantity` | decimal(18,4) | NO | Transferred quantity |
| `product_code` | varchar(25) | YES | Snapshot |
| `product_name` | varchar(250) | YES | Snapshot |

### `lot_serial_tracking`
Lot/serial number movement ledger (all inventory transactions).

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `lot_serial_tracking_id` | int(11) | NO | PK |
| `source` | int(11) | NO | Enum: InventoryReceipt, SalesOrder, Transfer, etc. |
| `reference` | int(11) | NO | ID of the source document |
| `date` | datetime | NO | Transaction date |
| `warehouse` | int(11) | NO | FK → `warehouse` |
| `product` | int(11) | NO | FK → `product` |
| `quantity` | decimal(18,4) | NO | Positive=in, Negative=out |
| `lot_number` | varchar(50) | YES | Lot/batch number |
| `expiration_date` | date | YES | Expiry date (perishables) |
| `serial_number` | varchar(50) | YES | Serial number (seriable products) |

### `lot_serial_rqmt`
Lot/serial requirement pending fulfillment.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `lot_serial_rqmt_id` | int(11) | NO | PK |
| `source` | int(11) | NO | Source document type enum |
| `reference` | int(11) | NO | Source document ID |
| `warehouse` | int(11) | NO | FK → `warehouse` |
| `product` | int(11) | NO | FK → `product` |
| `quantity` | decimal(18,4) | NO | Required quantity |

---

## 8. Purchases

### `purchase_request`
Internal purchase request (before PO is created).

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `purchase_request_id` | int(11) | NO | PK |
| `creator` | int(11) | NO | FK → `employee` |
| `updater` | int(11) | NO | FK → `employee` |
| `warehouse` | int(11) | NO | FK → `warehouse` — destination |
| `comment` | varchar(500) | YES | Notes |
| `serial` | int(11) | YES | Folio |
| `date` | datetime | NO | Request date |
| `creation_time` | datetime | NO | Created at |
| `modification_time` | datetime | NO | Last updated |
| `completed` | tinyint(1) | YES | Fulfilled flag |
| `cancelled` | tinyint(1) | YES | Cancelled flag |
| `approved` | tinyint(1) | NO | Approved by supervisor |

### `purchase_request_detail`
Line items of a purchase request.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `purchase_request_detail_id` | int(11) | NO | PK |
| `purchase_request` | int(11) | NO | FK → `purchase_request` |
| `product` | int(11) | NO | FK → `product` |
| `product_name` | varchar(250) | YES | Snapshot |
| `quantity` | decimal(18,2) | NO | Requested quantity |
| `warehouse` | int(11) | YES | FK → `warehouse` — destination override |
| `customer` | int(11) | YES | FK → `customer` — if for specific customer |
| `to_purchase` | tinyint(1) | NO | Flagged for PO inclusion |
| `Accepted` | tinyint(1) | NO | Approved for purchasing |

### `purchase_order`
Purchase order sent to a supplier.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `purchase_order_id` | int(11) | NO | PK |
| `supplier` | int(11) | YES | FK → `supplier` |
| `creation_time` | datetime | NO | Created at |
| `modification_time` | datetime | NO | Last updated |
| `creator` | int(11) | NO | FK → `employee` |
| `updater` | int(11) | NO | FK → `employee` |
| `completed` | tinyint(1) | NO | Received in full |
| `cancelled` | tinyint(1) | NO | Cancelled |
| `approved` | tinyint(1) | NO | Approved flag |
| `estimated_receipt_date` | datetime | YES | Expected arrival |
| `invoice_number` | varchar(50) | YES | Supplier's invoice reference |
| `comment` | varchar(500) | YES | Notes |
| `approver` | int(11) | YES | FK → `employee` |

### `purchase_order_detail`
Line items of a purchase order.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `purchase_order_detail_id` | int(11) | NO | PK |
| `purchase_order` | int(11) | NO | FK → `purchase_order` |
| `product` | int(11) | NO | FK → `product` |
| `warehouse` | int(11) | YES | FK → `warehouse` — destination |
| `quantity` | decimal(18,4) | NO | Ordered quantity |
| `price` | decimal(18,7) | NO | Unit cost |
| `discount` | decimal(9,8) | NO | Discount |
| `tax_rate` | decimal(5,4) | NO | VAT rate |
| `product_code` | varchar(25) | NO | Snapshot |
| `product_name` | varchar(250) | NO | Snapshot |
| `exchange_rate` | decimal(8,4) | NO | FX rate |
| `currency` | int(11) | NO | Currency |
| `tax_included` | tinyint(1) | NO | Price includes tax |
| `purchase_request_detail` | int(11) | YES | FK — originating request line |

### `expense_voucher`
Petty cash / expense ticket issued from a cash session.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `expense_voucher_id` | int(11) | NO | PK |
| `creator` | int(11) | NO | FK → `employee` |
| `updater` | int(11) | NO | FK → `employee` |
| `store` | int(11) | NO | FK → `store` |
| `cash_session` | int(11) | NO | FK → `cash_session` |
| `comment` | varchar(500) | YES | Notes |
| `date` | datetime | NO | Expense date |
| `creation_time` | datetime | NO | Created at |
| `modification_time` | datetime | NO | Last updated |
| `completed` | tinyint(1) | YES | Posted |
| `cancelled` | tinyint(1) | YES | Cancelled |

### `expense_voucher_detail`
Line items of an expense voucher.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `expense_voucher_detail_id` | int(11) | NO | PK |
| `expense_voucher` | int(11) | NO | FK → `expense_voucher` |
| `expense` | int(11) | NO | FK → `expenses` |
| `amount` | decimal(18,2) | NO | Amount |
| `comment` | varchar(500) | YES | Notes |

---

## 9. Logistics

### `delivery_order`
Delivery order (picking/shipping order for a customer).

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `delivery_order_id` | int(11) | NO | PK |
| `creator` | int(11) | NO | FK → `employee` |
| `updater` | int(11) | NO | FK → `employee` |
| `creation_time` | datetime | NO | Created at |
| `modification_time` | datetime | NO | Last updated |
| `store` | int(11) | NO | FK → `store` |
| `serial` | int(11) | NO | Folio |
| `customer` | int(11) | NO | FK → `customer` |
| `ship_to` | int(11) | YES | FK → `address` |
| `contact` | int(11) | YES | FK → `contact` |
| `date` | datetime | YES | Scheduled delivery date |
| `priority` | tinyint(3) | NO | Priority level |
| `completed` | tinyint(1) | NO | Delivered flag |
| `cancelled` | tinyint(1) | NO | Cancelled |
| `comment` | varchar(500) | YES | Notes |
| `delivered` | tinyint(1) | YES | Physical delivery confirmed |
| `confirmed` | tinyint(1) | YES | Supervisor confirmed |
| `picked_up` | tinyint(1) | NO | Customer picked up at counter |

### `delivery_order_detail`
Line items of a delivery order.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `delivery_order_detail_id` | int(11) | NO | PK |
| `delivery_order` | int(11) | NO | FK → `delivery_order` |
| `sales_order_detail` | int(11) | YES | FK → `sales_order_detail` |
| `product` | int(11) | NO | FK → `product` |
| `quantity` | decimal(18,4) | NO | Quantity to deliver |
| `product_code` | varchar(425) | NO | Snapshot |
| `product_name` | varchar(250) | NO | Snapshot |

### `deliveries_itinerary`
Route/trip plan grouping multiple delivery orders.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `deliveries_itinerary_id` | int(11) | NO | PK |
| `vehicle` | int(11) | YES | FK → `vehicle` |
| `vehicle_operator` | int(11) | YES | FK → `vehicle_operator` |
| `date` | date | NO | Delivery date |
| `creator` | int(11) | NO | FK → `employee` |
| `updater` | int(11) | NO | FK → `employee` |
| `creation_time` | datetime | NO | Created at |
| `modification_time` | datetime | NO | Last updated |
| `cancelled` | tinyint(1) | NO | Cancelled |
| `completed` | tinyint(1) | NO | Route completed |
| `comment` | varchar(500) | YES | Notes |
| `warehouse` | int(11) | YES | FK → `warehouse` — dispatch origin |

### `deliveries_itinerary_detail`
Items loaded onto a delivery itinerary.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `deliveries_itinerary_detail_id` | int(11) | NO | PK |
| `deliveries_itinerary` | int(11) | YES | FK → `deliveries_itinerary` |
| `delivery_order_detail` | int(11) | NO | FK → `delivery_order_detail` |
| `quantity` | decimal(20,6) | NO | Quantity loaded |
| `comment` | varchar(500) | YES | Notes |

---

## 10. Fiscal Documents

### `taxpayer_issuer`
Company/entity that issues CFDI fiscal documents (RFC).

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `taxpayer_issuer_id` | varchar(13) | NO | RFC (PK) |
| `name` | varchar(250) | YES | Legal name |
| `regime` | varchar(3) | NO | FK → `sat_tax_regime` |
| `provider` | int(11) | NO | CFDI PAC provider enum |
| `comment` | varchar(500) | YES | Notes |
| `postal_code` | char(5) | YES | FK → `sat_postal_code` |

### `taxpayer_certificate`
CSD certificate files for CFDI digital signing.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `taxpayer_certificate_id` | char(20) | NO | Certificate number (PK) |
| `taxpayer` | varchar(13) | NO | FK → `taxpayer_issuer` |
| `certificate_data` | blob | NO | .cer file binary |
| `key_data` | blob | NO | .key file binary |
| `key_password` | tinyblob | NO | Encrypted private key password |
| `valid_from` | datetime | NO | Certificate validity start |
| `valid_to` | datetime | NO | Certificate validity end |
| `active` | tinyint(1) | NO | Currently active for signing |

### `taxpayer_batch`
Folio series (batch) configuration per taxpayer and document type.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `taxpayer_batch_id` | int(11) | NO | PK |
| `taxpayer` | varchar(13) | NO | FK → `taxpayer_issuer` |
| `batch` | varchar(10) | NO | Series letter(s) (e.g. "A", "FAC") |
| `type` | int(11) | NO | CFDI document type enum |
| `template` | text | NO | XML template or config |

### `fiscal_document`
CFDI electronic invoice header.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `fiscal_document_id` | int(11) | NO | PK |
| `creation_time` | datetime | NO | Created at |
| `modification_time` | datetime | NO | Last updated |
| `creator` | int(11) | NO | FK → `employee` |
| `updater` | int(11) | NO | FK → `employee` |
| `issuer` | varchar(13) | NO | FK → `taxpayer_issuer` — RFC |
| `issuer_name` | varchar(250) | YES | Legal name snapshot |
| `issuer_regime` | varchar(3) | YES | Tax regime code |
| `issuer_regime_name` | varchar(250) | YES | Tax regime description snapshot |
| `issuer_address` | int(11) | YES | FK → `address` |
| `customer` | int(11) | NO | FK → `customer` |
| `recipient` | varchar(13) | NO | Recipient RFC |
| `recipient_name` | varchar(250) | YES | Recipient legal name |
| `recipient_address` | int(11) | YES | FK → `address` |
| `type` | int(11) | NO | CFDI type enum (I=Income, E=Expense, etc.) |
| `store` | int(11) | NO | FK → `store` |
| `batch` | varchar(10) | YES | Folio series |
| `serial` | int(11) | YES | Sequential folio number |
| `issued` | datetime | YES | Date/time stamped by PAC |
| `issued_at` | int(11) | YES | FK → `address` — issued location |
| `issued_location` | varchar(250) | NO | Postal code of issuance |
| `completed` | bit(1) | NO | Stamped and valid |
| `cancelled` | bit(1) | NO | Cancelled with SAT |
| `cancellation_date` | datetime | YES | Date of cancellation |
| `reference` | varchar(25) | YES | Internal reference |
| `payment_method` | int(11) | NO | SAT payment method enum |
| `payment_reference` | varchar(50) | YES | Payment reference |
| `exchange_rate` | decimal(8,4) | NO | FX rate |
| `currency` | int(11) | NO | Currency |
| `payment_terms` | tinyint(4) | NO | PUE (single) or PPD (installment) |
| `usage` | varchar(3) | YES | FK → `sat_cfdi_usage` |
| `comment` | varchar(1000) | YES | Notes |
| `stamped` | datetime | YES | PAC stamping timestamp |
| `stamp_uuid` | varchar(36) | YES | SAT UUID (folio fiscal) |
| `authority_digital_seal` | varchar(500) | YES | SAT digital seal |
| `authority_certificate_number` | varchar(20) | YES | SAT certificate number |
| `version` | decimal(3,1) | NO | CFDI version (4.0) |
| `provider` | int(11) | NO | PAC provider used |
| `retention_rate` | decimal(5,4) | NO | ISR retention rate |
| `local_retention_name` | varchar(32) | YES | Local tax name |
| `local_retention_rate` | decimal(5,4) | NO | Local tax rate |
| `cancellation_reason` | varchar(250) | YES | FK → `sat_reason_cancellation` |
| `cancellation_substitution` | varchar(250) | YES | UUID of replacement document |
| `taxpayer_regime` | varchar(3) | YES | Recipient regime |
| `taxpayer_postal_code` | varchar(5) | YES | Recipient postal code |
| `rfc_pac` | varchar(13) | YES | PAC RFC that stamped the document |

### `fiscal_document_detail`
Line items of a fiscal document.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `fiscal_document_detail_id` | int(11) | NO | PK |
| `document` | int(11) | NO | FK → `fiscal_document` |
| `product` | int(11) | NO | FK → `product` |
| `order_detail` | int(11) | YES | FK → `sales_order_detail` |
| `product_service` | varchar(8) | YES | FK → `sat_product_service` — SAT code |
| `product_code` | varchar(35) | YES | Internal code |
| `product_name` | varchar(1000) | NO | Description |
| `unit_of_measurement` | varchar(3) | YES | FK → `sat_unit_of_measurement` |
| `unit_of_measurement_name` | varchar(128) | YES | UoM description |
| `quantity` | decimal(18,4) | NO | Quantity |
| `price` | decimal(18,7) | NO | Unit price (pre-tax) |
| `discount` | decimal(9,8) | NO | Discount |
| `tax_rate` | decimal(7,6) | NO | VAT rate |
| `exchange_rate` | decimal(8,4) | NO | FX rate |
| `currency` | int(11) | NO | Currency |
| `tax_included` | tinyint(1) | NO | Price includes tax |
| `comment` | varchar(1000) | YES | Line note |

### `fiscal_document_relation`
Payment complement relations (PPD payment tracking).

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `fiscal_document_relation_id` | int(11) | NO | PK |
| `document` | int(11) | NO | FK → `fiscal_document` — complement |
| `relation` | int(11) | NO | FK → `fiscal_document` — original invoice |
| `exchange_rate` | decimal(8,4) | NO | FX rate |
| `installment` | int(11) | NO | Payment installment number |
| `previous_balance` | decimal(18,2) | NO | Balance before this payment |
| `amount` | decimal(18,2) | NO | Amount applied |
| `type` | varchar(3) | YES | Relation type code |

### `fiscal_document_xml`
Stored XML blob for a stamped CFDI.

| Column | Type | Description |
|--------|------|-------------|
| `fiscal_document_xml_id` | int(11) | PK = FK → `fiscal_document` |
| `data` | mediumtext | Full CFDI XML content |

---

## 11. Technical Service

### `tech_service_receipt`
Equipment intake receipt for technical service.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `tech_service_receipt_id` | int(11) | NO | PK |
| `brand` | varchar(64) | NO | Brand of equipment |
| `equipment` | varchar(64) | NO | Equipment type description |
| `model` | varchar(64) | NO | Model |
| `serial_number` | varchar(64) | YES | Serial number |
| `date` | datetime | NO | Receipt date |
| `status` | varchar(64) | YES | Current status |
| `location` | varchar(128) | YES | Storage location |
| `checker` | varchar(128) | NO | Who checked it in |
| `comment` | varchar(1024) | YES | Notes |

### `tech_service_receipt_component`
Accessories/components received with equipment.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `tech_service_receipt_component_id` | int(11) | NO | PK |
| `receipt` | int(11) | NO | FK → `tech_service_receipt` |
| `name` | varchar(128) | NO | Component name |
| `quantity` | int(11) | NO | Count |
| `serial_number` | varchar(64) | YES | Serial |
| `comment` | varchar(256) | YES | Notes |

### `tech_service_report`
Technical diagnosis/repair report.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `tech_service_report_id` | int(11) | NO | PK |
| `date` | datetime | NO | Report date |
| `location` | varchar(128) | NO | Service location |
| `type` | varchar(128) | NO | Service type |
| `equipment` | varchar(64) | NO | Equipment |
| `brand` | varchar(64) | NO | Brand |
| `model` | varchar(64) | NO | Model |
| `serial_number` | varchar(64) | YES | Serial |
| `user` | varchar(128) | YES | Equipment owner/user |
| `technician` | varchar(128) | YES | Technician name |
| `cost` | decimal(18,4) | NO | Service cost |
| `user_report` | varchar(1024) | YES | Problem as reported by user |
| `description` | varchar(1024) | YES | Technical diagnosis |
| `comment` | varchar(1024) | YES | Additional notes |

### `tech_service_request`
Customer technical service request.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `tech_service_request_id` | int(11) | NO | PK |
| `type` | int(11) | NO | Service type enum |
| `brand` | varchar(64) | NO | Brand |
| `equipment` | varchar(64) | NO | Equipment description |
| `model` | varchar(64) | NO | Model |
| `serial_number` | varchar(64) | YES | Serial |
| `date` | datetime | NO | Request date |
| `end_date` | datetime | YES | Completion date |
| `customer` | int(11) | NO | FK → `customer` |
| `responsible` | varchar(128) | NO | Responsible technician |
| `location` | varchar(128) | NO | Service address |
| `payment_status` | varchar(64) | YES | Paid/Pending/etc. |
| `shipping_method` | varchar(64) | YES | How equipment was/will be shipped |
| `contact_name` | varchar(128) | YES | Contact person |
| `contact_phone_number` | varchar(64) | YES | Contact phone |
| `address` | varchar(256) | YES | Service address text |
| `remarks` | varchar(1024) | YES | Customer remarks |
| `comment` | varchar(1024) | YES | Internal notes |

### `tech_service_request_component`
Components listed in a service request.

Mirrors `tech_service_receipt_component` structure, linked to `tech_service_request`.

### `vehicle_service_order`
Maintenance/repair order for a fleet vehicle.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `service_order_id` | int(11) | NO | PK |
| `vehicle` | int(11) | NO | FK → `vehicle` |
| `problem_description` | varchar(500) | NO | Reported problem |
| `service_description` | varchar(500) | YES | Work performed |
| `creator` | int(11) | NO | FK → `employee` |
| `updater` | int(11) | NO | FK → `employee` |
| `notifier` | int(11) | NO | FK → `employee` — who was notified |
| `creation_time` | datetime | NO | Created at |
| `modification_time` | datetime | NO | Last updated |
| `completed` | tinyint(1) | NO | Work completed |
| `cancelled` | tinyint(1) | NO | Cancelled |
| `comment` | varchar(250) | YES | Notes |
| `date` | datetime | YES | Scheduled/completion date |

### `service_order_detail`
Spare parts used in a vehicle service order.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `service_order_detail_id` | int(11) | NO | PK |
| `vehicle_service_order` | int(11) | NO | FK → `vehicle_service_order` |
| `spare_part` | int(11) | NO | FK → `product` |
| `quantity` | decimal(20,6) | NO | Quantity used |
| `comment` | varchar(500) | YES | Notes |
| `date` | datetime | NO | Date used |

---

## 12. Front Desk

### `translation_request`
Document translation request managed by the front desk.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `translation_request_id` | int(11) | NO | PK |
| `requester` | int(11) | NO | FK → `employee` — who requested |
| `date` | datetime | NO | Request date |
| `agency` | varchar(256) | NO | Translation agency name |
| `document_name` | varchar(128) | NO | Document being translated |
| `amount` | decimal(18,4) | NO | Cost |
| `delivery_date` | datetime | NO | Expected delivery date |
| `comment` | varchar(1024) | YES | Notes |

### `notarization`
Notarization process request.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `notarization_id` | int(11) | NO | PK |
| `requester` | int(11) | NO | FK → `employee` |
| `notary_office` | varchar(256) | NO | Notary office name |
| `date` | datetime | NO | Request date |
| `document_description` | varchar(512) | NO | Document description |
| `amount` | decimal(18,4) | NO | Cost |
| `payment_date` | datetime | NO | Payment date |
| `delivery_date` | datetime | NO | Expected delivery |
| `comment` | varchar(1024) | YES | Notes |

---

## 13. Commissions

### `commission`
Commission rate catalog entry.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `commission_id` | int(11) | NO | PK |
| `name` | varchar(50) | NO | Commission name/label |
| `commission_rate` | decimal(20,6) | NO | Rate (0–1) |
| `comment` | varchar(50) | YES | Notes |

### `commission_agent`
Marks an employee as a commission-eligible sales agent.

| Column | Type | Description |
|--------|------|-------------|
| `employee` | int(11) | FK → `employee` (PK) |

### `commission_participation`
Type of participation in a commission split.

| Column | Type | Description |
|--------|------|-------------|
| `commission_participation_id` | int(11) | PK |
| `name` | varchar(50) | Participation type name |

### `commission_product`
Assigns a commission rate to a product.

| Column | Type | Description |
|--------|------|-------------|
| `commission_product_id` | int(11) | PK |
| `product` | int(11) | FK → `product` (unique) |
| `commission` | int(11) | FK → `commission` |

### `commission_salesperson`
Assigns a salesperson's share of a commission.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `commission_salesperson_id` | int(11) | NO | PK |
| `salesperson` | int(11) | NO | FK → `commission_agent` |
| `commission` | int(11) | NO | FK → `commission` |
| `commission_participation` | int(11) | NO | FK → `commission_participation` |
| `participation_rate` | decimal(20,6) | NO | Share of the commission |

### `commissions_history`
Calculated commission record per sold order line.

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `commissions_history_id` | int(11) | NO | PK |
| `sales_order` | int(11) | NO | FK → `sales_order` |
| `sales_order_detail` | int(11) | NO | FK → `sales_order_detail` |
| `salesperson` | int(11) | YES | FK → `employee` |
| `customer` | varchar(250) | NO | Customer name snapshot |
| `paid` | tinyint(4) | NO | Commission paid flag |
| `date` | datetime | YES | Commission date |
| `product` | int(11) | NO | FK → `product` |
| `quantity` | decimal(18,4) | NO | Quantity sold |
| `price` | decimal(22,2) | NO | Sale price |
| `total_detail` | decimal(37,2) | NO | Line total |
| `commission_rate` | decimal(40,12) | YES | Applied rate |
| `commission` | decimal(50,2) | YES | Commission amount |
| `participation_rate` | decimal(18,4) | NO | Agent participation rate |
| `confirmed` | tinyint(1) | NO | Confirmed by accounting |

---

## 14. SAT Catalogs

Reference tables from Mexico's SAT (tax authority) for CFDI compliance. Read-only lookups, populated from SAT published catalogs.

| Table | PK | Description |
|-------|----|-------------|
| `sat_cfdi_usage` | `sat_cfdi_usage_id` varchar(4) | CFDI usage codes (G01, G03, P01, etc.) |
| `sat_country` | `sat_country_id` varchar(3) | Country codes |
| `sat_currency` | `sat_currency_id` varchar(3) | Currency codes (MXN, USD, etc.) |
| `sat_postal_code` | `sat_postal_code_id` varchar(5) | SAT postal code with state/borough/locality |
| `sat_product_service` | `sat_product_service_id` varchar(8) | Product/service classification codes |
| `sat_reason_cancellation` | `sat_reason_cancellation_id` varchar(2) | CFDI cancellation reason codes |
| `sat_tax_regime` | `sat_tax_regime_id` varchar(3) | Tax regime codes |
| `sat_unit_of_measurement` | `sat_unit_of_measurement_id` varchar(3) | Unit of measurement codes |

---

## 15. Computed / View Tables

These tables exist in the schema but function as denormalized snapshots or reporting views. They have no foreign key constraints and are populated by triggers or batch jobs.

| Table | Purpose |
|-------|---------|
| `abc_classification` | ABC inventory analysis per product/warehouse |
| `lead_time_purchase` | Calculated lead time and reorder stats per product/warehouse |
| `product_cost` | Current weighted average cost per product |
| `details` | Flattened payment details for commission calculation |
| `payments` | Flattened order payment summary |
| `refunds` | Flattened refund summary for commission reversals |

---

### `incidence`
Generic event/incident log (polymorphic — `source` identifies document type, `instance_id` is document ID).

| Column | Type | Null | Description |
|--------|------|------|-------------|
| `incidence_id` | int(11) | NO | PK |
| `source` | int(11) | NO | Document type enum |
| `instance_id` | int(11) | NO | Document ID |
| `modification_time` | datetime | YES | Event time |
| `updater` | int(11) | NO | FK → `employee` (by ID) |
| `content` | varchar(1000) | YES | Event payload / description |
| `comment` | varchar(500) | YES | Notes |
