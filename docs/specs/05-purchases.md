# Purchases Specs

Covers the complete procurement cycle: purchase requests, purchase order creation, goods receipt (in Inventory), expense vouchers, and supplier payments.

---

## 1. Purchase Requests

**Route**: `GET /purchase-requests`  
**SystemObject**: `PurchaseRequest`

### Purpose
Internal requests for goods before a formal purchase order is sent to a supplier. Originators (warehouse staff, salespeople) submit requests that purchasing agents consolidate into purchase orders.

### List View
- Filter by: warehouse, date range, status (open/approved/completed/cancelled), creator
- Columns: folio, date, warehouse, creator, status, approved

### Header Fields

| Field | Column | Notes |
|-------|--------|-------|
| Warehouse | `purchase_request.warehouse` | Destination |
| Serial/Folio | `purchase_request.serial` | Auto |
| Date | `purchase_request.date` | Defaults to today |
| Notes | `purchase_request.comment` | |

### Line Item Fields (`purchase_request_detail`)

| Field | Column | Notes |
|-------|--------|-------|
| Product | `product` | FK → `product` |
| Product Name | `product_name` | Snapshot or manual |
| Quantity | `quantity` | Requested amount |
| Destination Warehouse | `warehouse` | Override per line |
| Customer | `customer` | If requested for specific customer |
| Mark for Purchase | `to_purchase` | Boolean — purchasing agent flags this |
| Accepted | `Accepted` | Boolean — approved for PO inclusion |

### Actions
- **Submit**: sets date, notifies purchasing
- **Approve**: sets `approved=1` (requires approval privilege)
- **Complete**: sets `completed=1` after PO is created
- **Cancel**: sets `cancelled=1`

### Business Rules
- If `WebConfig.PurchaseRequestApprovalRequired = true`, only approved requests can be converted to purchase orders.
- Minimum order quantity (`product.min_order_qty`) warning shown on line entry.

---

## 2. Purchase Request Approval

**Route**: `GET /purchase-requests/approval`  
**Action**: `PurchaseRequestsApproval`  
**SystemObject**: `PurchaseRequestApproval`

### Purpose
Supervisor queue to review and approve/reject pending purchase requests.

### Features
- List all requests where `approved=0` and `cancelled=0`
- Bulk approve/reject
- View request detail before approving
- Rejection reason tracked via `incidence` log

---

## 3. Purchase Orders

**Route**: `GET /purchases`  
**SystemObject**: `PurchasesOrders`

### Purpose
Formal purchase orders sent to suppliers. Can be created from approved purchase requests or directly.

### List View
- Filter by: supplier, date range, status (open/approved/completed/cancelled)
- Columns: ID, creation date, supplier, invoice number, status, approved, estimated receipt

### Header Fields

| Field | Column | Notes |
|-------|--------|-------|
| Supplier | `purchase_order.supplier` | FK → `supplier` |
| Estimated Receipt Date | `purchase_order.estimated_receipt_date` | |
| Invoice Number | `purchase_order.invoice_number` | Supplier's invoice ref |
| Notes | `purchase_order.comment` | |

### Line Item Fields (`purchase_order_detail`)

| Field | Column | Notes |
|-------|--------|-------|
| Product | `product` | FK → `product` |
| Destination Warehouse | `warehouse` | FK → `warehouse` |
| Quantity | `quantity` | |
| Unit Price | `price` | Purchase cost |
| Discount % | `discount` | 0–1 |
| Tax Rate | `tax_rate` | |
| Tax Included | `tax_included` | |
| Currency | `currency` | |
| Exchange Rate | `exchange_rate` | |
| Origin PR Line | `purchase_request_detail` | Optional traceability link |

### Actions
- **Approve**: sets `approved=1`, `approver = current_employee` (if `WebConfig.PurchaseOrderApprovalRequired`)
- **Receive**: navigate to Inventory → Receipts, create receipt linked to this PO
- **Cancel**: sets `cancelled=1`
- **Print/Email**: purchase order document to supplier

### Business Rules
- A PO cannot be edited once fully received (`completed=1`).
- If approval is required and PO is not approved, it cannot be sent.
- `completed` is set automatically when all lines are fully received.

---

## 4. Expense Vouchers (Ticket de Gastos)

**Route**: `GET /expense-voucher`  
**SystemObject**: `ExpenseTicket`

### Purpose
Petty cash / expense ticket issued during a cash session. Records miscellaneous cash outflows from the drawer.

### List View
- Filter by: store, cash session, date range, status
- Columns: ID, date, store, session, amount, status

### Header Fields

| Field | Column | Notes |
|-------|--------|-------|
| Store | `expense_voucher.store` | From user context |
| Cash Session | `expense_voucher.cash_session` | Must be open session |
| Date | `expense_voucher.date` | |
| Notes | `expense_voucher.comment` | |

### Line Item Fields (`expense_voucher_detail`)

| Field | Column | Notes |
|-------|--------|-------|
| Expense Category | `expense` | FK → `expenses` |
| Amount | `amount` | |
| Note | `comment` | |

### Actions
- **Complete**: posts the expense; reduces session cash balance
- **Cancel**: voids

### Business Rules
- A cash session must be open to create expense vouchers.
- Completed vouchers reduce the expected cash in the session's closing count.
- Cannot be created without an active `cash_session`.

---

## 5. Supplier Payments

**Route**: `GET /supplier-payments`  
**SystemObject**: `SupplierPayment`

### Purpose
Record payments made to suppliers for purchase orders.

### List View
- Filter by: supplier, date range, method
- Columns: ID, date, supplier, method, amount, reference

### Form Fields

| Field | Column | Notes |
|-------|--------|-------|
| Supplier | `supplier_payment.supplier` | FK → `supplier` |
| Amount | `supplier_payment.amount` | |
| Payment Method | `supplier_payment.method` | Enum |
| Date | `supplier_payment.date` | |
| Reference | `supplier_payment.reference` | Check/transfer ref |
| Notes | `supplier_payment.comment` | |

### Business Rules
- A supplier payment is currently a standalone record (not directly linked to a specific PO in the schema).
- Reconciliation between PO amounts and payments is done via the Accounts Payable report.
