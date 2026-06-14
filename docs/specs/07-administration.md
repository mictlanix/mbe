# Administration Specs

Covers receivables and payables tracking — the financial position view of outstanding customer balances and supplier obligations.

---

## 1. Accounts Receivable

**Route**: `GET /accounts-receivables`  
**Controller**: `AccountsReceivablesController`  
**SystemObject**: `AccountsReceivable` (18)

### Purpose
Shows all outstanding customer balances: completed, non-cancelled, unpaid sales orders with `payment_terms = NetD (1)`, grouped and sortable by aging.

### Features

#### Customer Balance Summary
The AR view runs a raw SQL query joining `sales_order`, `customer`, `employee`, and `fiscal_document`. Each row represents one unpaid sales order.

Columns:
- Date, Sales Order ID, Due Date, Customer, **Sales Agent** (from `customer.salesperson`), **Salesperson** (from `sales_order.salesperson`), Store code, Paid flag, Invoice folios (from linked fiscal documents), Total (in order currency), Total (in base currency), Refunds, Currency

> Note: Two distinct employee fields — the customer's assigned sales agent vs. the salesperson on the order. These can differ.

#### Optional Customer Filter
When called with a `customer` parameter, filters to a single customer's open orders.

#### Store Scope
- Default: current user's store only
- If user has `SearchCreditsFromAllStores.AllowRead` (101): can view all stores' AR

#### Actions
- **Apply Payment**: shortcut to record a customer payment and apply to selected orders
- **Generate Statement**: printable account statement per customer
- **Send Statement**: email statement to customer contact

### Balance Calculation
```sql
-- Total due per order line (base currency)
SUM(ROUND(d.quantity * d.price * d.exchange_rate
          * (1 - d.discount_rate)
          * IF(d.tax_included = 0, 1 + d.tax_rate, 1), 2))
-- Minus refunds
MINUS SUM(refund details for same order)
-- Minus payments applied
-- (separate query via sales_order_payment)
```

### Business Rules
- Only `payment_terms = NetD` orders appear (credit sales). Cash orders (`Immediate`) are excluded.
- Orders past `due_date` are highlighted as overdue.
- Credit limit utilization shown as percentage of `customer.credit_limit`.
- `balance_zeroed_time` is set when a supervisor manually zeros the remaining balance.

---

## 2. Accounts Payable

**Route**: `GET /accounts-payables`  
**Controller**: `AccountsPayablesController`  
**SystemObject**: `AccountsPayable` (19)

### Purpose
Shows all outstanding supplier obligations: purchase orders received but not fully paid, grouped by supplier.

### Features

#### Supplier Balance Summary
- List suppliers with unpaid PO amounts > 0
- Columns: supplier, total POs received, total paid (from `supplier_payment`), balance, oldest receipt date

#### Supplier Detail Drill-down
- Show all relevant `purchase_order` records per supplier
- Per PO: ID, date, estimated receipt, total amount, paid amount, balance

#### Actions
- **Record Payment**: shortcut to create a `supplier_payment`
- **Generate Remittance**: printable payment advice

### Balance Calculation
The schema has no direct `purchase_order ↔ supplier_payment` link table. Balance is reconciled by matching:
- Total received goods value from `purchase_order` + `inventory_receipt` lines
- Against total `supplier_payment.amount` for that supplier

### Business Rules
- Overdue obligations: `purchase_order` expected payment date = inventory receipt date + `supplier.credit_days`.
- `supplier.credit_limit` and `supplier.credit_days` define payment terms.

---

## 3. Expenses

**Route**: `GET /expenses`  
**Controller**: `ExpensesController`  
**SystemObject**: `Expenses` (81)

### Purpose
Expense category catalog used in expense vouchers (petty cash). This is the catalog management screen for expense types, not the voucher entry itself (see Purchases spec for voucher entry).

### Form Fields

| Field | Column | Notes |
|-------|--------|-------|
| Name | `expenses.expense` | Category label, required |
| Notes | `expenses.comment` | |

---

## 4. Cash Session Management

Cash sessions are managed within the Payments flow (`PaymentsController`), not as a standalone admin screen, but they are a critical administrative concept.

### Cash Session Lifecycle

| Step | Action | Details |
|------|--------|---------|
| 1 | Open session | Select cash drawer, enter opening cash amount → creates `cash_session` with `Start = now` |
| 2 | Accept payments | All POS and customer payments in the session are linked via `cash_session_id` |
| 3 | Session date check | If session was opened on a prior calendar day → forced close before new sales |
| 4 | Close session | Enter denomination counts (`cash_count`), confirm totals → sets `End = now` |

### Key Table: `cash_session`

| Field | Notes |
|-------|-------|
| `cash_session_id` | PK |
| `cash_drawer` | FK → `cash_drawer` |
| `cashier` | FK → `employee` — who opened the session |
| `start` | Session open time |
| `end` | Session close time (NULL = still open) |
| `initial_amount` | Opening cash declared |
| `comment` | Notes at close |

### SystemObject
- `CashSessionClose` (111): gates who can close a cash session

---

## 5. Credit Customer Configuration

**SystemObject**: `CreditCustomerConfiguration` (98)

### Purpose
Administrative screen to configure credit terms and limits for credit customers. Controls which customers are eligible for `NetD` payment terms.

### Key Fields (on `customer`)
- `credit_limit`: maximum outstanding balance allowed
- `credit_days`: days until payment is due
- `has_credit`: computed from whether `credit_limit > 0`

### Business Rules
- `HasCredit = credit_limit > 0`
- Customers without credit (`HasCredit = false`) cannot use `NetD` payment terms on sales orders.
- Expired credits = `credit_days` past due date; blocks new credit orders until cleared.
