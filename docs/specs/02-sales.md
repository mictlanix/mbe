# Sales Specs

Covers the complete sales cycle: pricing, quoting, POS transactions, orders, payments, and refunds.

---

## 1. Pricing

**Route**: `GET /pricing`  
**SystemObject**: `Pricing`

### Purpose
Bulk price management tool. Allows viewing and editing prices for all products across all price lists simultaneously, with margin validation.

### Features
- Grid view: products as rows, price lists as columns
- Edit price inline; show computed margin vs cost
- Warn when price is below `low_profit_margin` or above `high_profit_margin`
- Filter by product label, supplier, brand, model
- Import/export via CSV

### Key Tables
- `product_price` (read/write)
- `product` (reference: cost, tax rate)
- `price_list` (column headers)

---

## 2. Sales Quotes

**Route**: `GET /quotations`  
**SystemObject**: `SalesQuotes`

### Purpose
Customer quotations (presales). A quote can be converted to a sales order.

### List View
- Filter by: store, date range, customer, salesperson, status (open/completed/cancelled)
- Columns: folio, date, customer, salesperson, total, status, due date

### Header Fields

| Field | Column | Notes |
|-------|--------|-------|
| Store | `sales_quote.store` | From user context |
| Serial/Folio | `sales_quote.serial` | Auto-generated |
| Date | `sales_quote.date` | Defaults to today |
| Salesperson | `sales_quote.salesperson` | FK → `employee` |
| Customer | `sales_quote.customer` | FK → `customer` |
| Contact | `sales_quote.contact` | FK → `contact` |
| Ship To | `sales_quote.ship_to` | FK → `address` |
| Payment Terms | `sales_quote.payment_terms` | Enum: Cash, 30d, 60d, etc. |
| Due Date | `sales_quote.due_date` | Quote expiry |
| Currency | `sales_quote.currency` | |
| Exchange Rate | `sales_quote.exchange_rate` | |
| Notes | `sales_quote.comment` | |

### Line Item Fields (`sales_quote_detail`)

| Field | Column | Notes |
|-------|--------|-------|
| Product | `product` | FK → `product` |
| Quantity | `quantity` | |
| Price | `price` | From price list or manual |
| Price Adjustment | `price_adjustment` | Manual delta |
| Discount % | `discount_rate` | 0–1 |
| Tax Rate | `tax_rate` | From product |
| Line Note | `comment` | |

### Actions
- **Convert to Order**: creates a `sales_order` from this quote, setting `sales_quote.completed = 1`
- **Cancel**: sets `cancelled = 1`
- **Print / PDF**: generates printable quote document

### Business Rules
- Expired quotes (past `due_date`) cannot be converted to orders without override.
- Price below `low_profit_margin` shows a warning.

---

## 3. Point of Sale (POS)

**Route**: `GET /pos`  
**SystemObject**: `POS`

### Purpose
Fast cash register screen for walk-in sales. Requires an open `cash_session`.

### Session Management
- User must open a cash session (select cash drawer, enter opening cash amount) before selling.
- Session close requires entering denominations (`cash_count`) and confirming totals.

### POS Screen Elements
- Customer search/select (can use a "General Public" default)
- Product search by code, name, or barcode scan
- Cart with quantity, price, discount per line
- Payment collection: supports multiple payment methods per transaction
- Change calculation for cash payments
- Receipt print trigger after completion

### Created Records
- `sales_order` with `point_sale`, `payment_terms = Cash`
- `sales_order_detail` per line
- `customer_payment` per payment method used
- `sales_order_payment` linking order to payments

### Business Rules
- POS sales default to immediate payment (no credit terms).
- Price must respect `low_profit_margin` unless user has override privilege.
- If product is `stock_verification = true`, check available stock before selling.
- Cash session must be open; block sale if none is active.

---

## 4. Sales Orders

**Route**: `GET /sales-orders`  
**SystemObject**: `SalesOrders`

### Purpose
Full sales order management for credit customers and pre-invoiced sales. Supports partial deliveries, multi-currency, and fiscal document generation.

### List View
- Filter by: store, date range, customer, salesperson, status, paid/unpaid, delivered/undelivered
- Columns: folio, date, customer, salesperson, total, balance, status, delivery status

### Header Fields

| Field | Column | Notes |
|-------|--------|-------|
| Store | `sales_order.store` | |
| Serial/Folio | `sales_order.serial` | Auto per store |
| Point of Sale | `sales_order.point_sale` | FK → `point_sale` |
| Salesperson | `sales_order.salesperson` | FK → `employee` |
| Customer | `sales_order.customer` | FK → `customer` |
| Origin Quote | `sales_order.sales_quote` | Optional |
| Payment Terms | `sales_order.payment_terms` | |
| Date | `sales_order.date` | |
| Promise Date | `sales_order.promise_date` | |
| Due Date | `sales_order.due_date` | |
| Contact | `sales_order.contact` | |
| Ship To | `sales_order.ship_to` | |
| Recipient RFC | `sales_order.recipient` | For invoice |
| Recipient Name | `sales_order.recipient_name` | |
| Currency | `sales_order.currency` | |
| Exchange Rate | `sales_order.exchange_rate` | |
| Priority | `sales_order.priority` | 1=Normal |
| Partial Deliveries | `sales_order.partial_deliveries` | Allow/deny |
| Notes | `sales_order.comment` | |

### Line Item Fields (`sales_order_detail`)

| Field | Column | Notes |
|-------|--------|-------|
| Product | `product` | |
| Quantity | `quantity` | |
| Cost | `cost` | At time of sale (for margin tracking) |
| Price | `price` | |
| Discount % | `discount_rate` | |
| Tax Rate | `tax_rate` | |
| Warehouse | `warehouse` | Fulfillment source |
| Requires Delivery | `delivery` | Boolean |
| Currency | `currency` | |
| Tax Included | `tax_included` | |
| Note | `comment` | |

### Actions
- **Apply Payment**: link a `customer_payment` to this order
- **Create Delivery Order**: from lines marked `delivery=true`
- **Create Fiscal Document**: generate CFDI from this order
- **Cancel**: sets `cancelled=1`
- **Print**: order confirmation document

### Business Rules
- Customer credit check: sum of unpaid orders must not exceed `credit_limit`.
- Payment due date enforcement (block new orders for customers past due).
- `balance_zeroed_time` is set when a supervisor manually zeros the remaining balance.

---

## 5. Customer Payments

**Route**: `GET /payments`  
**SystemObject**: `CustomerPayments`

### Purpose
Record and manage payments received from customers. Payments are then applied to one or more sales orders.

### List View
- Filter by: store, date range, customer, method, verified
- Columns: folio, date, customer, method, amount, currency, verified

### Form Fields

| Field | Column | Notes |
|-------|--------|-------|
| Customer | `customer_payment.customer` | FK → `customer` |
| Amount | `customer_payment.amount` | |
| Currency | `customer_payment.currency` | |
| Payment Method | `customer_payment.method` | Enum |
| Payment Option | `customer_payment.payment_charge` | FK → `payment_method_option` |
| Reference | `customer_payment.reference` | Check/transfer ref |
| Date | `customer_payment.date` | |
| Store | `customer_payment.store` | |
| Cash Session | `customer_payment.cash_session` | If POS payment |
| Payment Type | `customer_payment.payment_type` | Normal / Credit / COD |

### Actions
- **Apply to Order**: creates `sales_order_payment` record
- **Verify**: supervisor confirms the payment (`verifier` field)
- **Unapply**: removes application from order

---

## 6. Customer Refunds

**Route**: `GET /customer-refunds`  
**SystemObject**: `CustomerRefunds`

### Purpose
Process product returns from customers. Generates credit or cash refund.

### List View
- Filter by: store, date range, customer, status

### Header Fields

| Field | Column | Notes |
|-------|--------|-------|
| Original Sales Order | `customer_refund.sales_order` | FK → `sales_order` |
| Customer | `customer_refund.customer` | Auto-filled from order |
| Salesperson | `customer_refund.sales_person` | FK → `employee` |
| Store | `customer_refund.store` | |
| Date | `customer_refund.date` | |
| Currency | `customer_refund.currency` | |
| Exchange Rate | `customer_refund.exchange_rate` | |

### Line Item Fields (`customer_refund_detail`)

| Field | Column | Notes |
|-------|--------|-------|
| Original Order Line | `sales_order_detail` | FK |
| Product | `product` | Auto-filled |
| Return Quantity | `quantity` | Must ≤ refundable qty on that line |
| Price | `price` | Auto-filled from original |
| Return-to Warehouse | `warehouse` | Where stock is restocked |
| Discount | `discount` | |
| Tax Rate | `tax_rate` | |

### Actions
- **Complete**: finalizes refund, restores stock, creates `credit_note`
- **Cancel**: voids the refund

### Business Rules
- Return quantity cannot exceed `GetRefundableQuantity()` — quantity sold minus previously returned.
- On completion, stock is added back to the specified warehouse via `lot_serial_tracking`.
- A `credit_note` is created automatically, which can be applied as payment credit.

---

## 7. Credit Payments

**Route**: `GET /payments/credit`  
**Action**: `CreditPayments`  
**SystemObject**: `CreditPayments`

### Purpose
Specialized view to manage credit payments — payments applied from credit notes or payment plans.

### Features
- List open credit notes per customer
- Apply credit note balance toward outstanding orders
- Shows credit note origin (which refund generated it)

---

## 8. Payments Editor

**Route**: `GET /payments/edit`  
**Action**: `Payments`  
**SystemObject**: `PaymentsEditor`

### Purpose
Supervisor tool to re-apply, split, or reassign payments between orders. Handles edge cases where payments were applied incorrectly.

### Features
- Search by customer, payment reference, date
- View all `sales_order_payment` applications for a payment
- Cancel an application (sets `cancelled=1`)
- Reapply to different order

---

## 9. Payments Verification

**Route**: `GET /payments/validation`  
**Action**: `PaymentsValidation`  
**SystemObject**: `PaymentsVerification`

### Purpose
Supervisory queue to verify unverified customer payments. Typically used when payments arrive by bank transfer and must be confirmed against bank statements.

### List View
- Shows payments where `verifier IS NULL`
- Filter by: store, date, method, amount range

### Actions
- **Verify**: sets `verifier = current_employee_id`
- **Reject**: flag for investigation (status mechanism via `incidence`)
