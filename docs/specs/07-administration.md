# Administration Specs

Covers receivables and payables tracking — the financial position view of outstanding customer balances and supplier obligations.

---

## 1. Accounts Receivable

**Route**: `GET /accounts-receivables`  
**SystemObject**: `AccountsReceivable`

### Purpose
Shows all outstanding customer balances: sales orders that are not fully paid, grouped by customer and aging bucket.

### Features

#### Customer Balance Summary
- List customers with unpaid balance > 0
- Columns: customer, total orders, total billed, total paid, balance, oldest due date
- Aging buckets: Current, 1–30 days overdue, 31–60 days, 61–90 days, 90+ days

#### Customer Detail Drill-down
- Show all open `sales_order` records for a customer
- Per order: folio, date, due date, total, paid, balance, days overdue
- Show linked `customer_payment` applications per order

#### Actions
- **Apply Payment**: shortcut to record a customer payment and apply to selected orders
- **Generate Statement**: printable account statement per customer
- **Send Statement**: email statement to customer contact

### Key Queries
```sql
-- Outstanding balance per customer
SELECT so.customer,
       SUM(so.total) - SUM(sop.amount_applied) AS balance
FROM sales_order so
LEFT JOIN sales_order_payment sop ON sop.sales_order = so.sales_order_id
WHERE so.paid = 0 AND so.cancelled = 0
GROUP BY so.customer;
```

### Business Rules
- Balance is calculated as: `SUM(sales_order lines total)` minus `SUM(sales_order_payment.amount)` where `sales_order_payment.cancelled = 0`.
- Orders past `due_date` are highlighted in the overdue aging column.
- Credit limit utilization shown as percentage of `customer.credit_limit`.

---

## 2. Accounts Payable

**Route**: `GET /accounts-payables`  
**SystemObject**: `AccountsPayable`

### Purpose
Shows all outstanding supplier obligations: purchase orders received but not fully paid, grouped by supplier.

### Features

#### Supplier Balance Summary
- List suppliers with unpaid PO amounts > 0
- Columns: supplier, total POs received, total paid, balance, oldest receipt date

#### Supplier Detail Drill-down
- Show all relevant `purchase_order` records per supplier
- Per PO: ID, date, estimated receipt, total amount, paid amount (from `supplier_payment`), balance

#### Actions
- **Record Payment**: shortcut to create a `supplier_payment`
- **Generate Remittance**: printable payment advice

### Key Concept
The schema does not have a direct `purchase_order ↔ supplier_payment` link table. Balance reconciliation is done by matching:
- Total received goods value from `purchase_order` + `inventory_receipt` lines
- Against total `supplier_payment.amount` for that supplier

### Business Rules
- Overdue obligations are those where the `purchase_order`'s expected payment date (based on `supplier.credit_days` from receipt date) has passed.
- Supplier's `credit_limit` and `credit_days` define payment terms.
