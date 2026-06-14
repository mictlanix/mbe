# Purchases Specs

Covers the complete procurement cycle: purchase requests, purchase order creation, goods receipt (handled in Inventory), expense vouchers, and supplier payments.

---

## 1. Purchase Requests

**Route**: `GET /purchase-requests`  
**Controller**: `PurchaseRequestsController`  
**SystemObject**: `PurchaseRequest` (86)

### Purpose
Internal requests for goods before a formal purchase order is sent to a supplier. Originators (warehouse staff, salespeople) submit requests that purchasing agents consolidate into purchase orders.

### List View
- Default: shows requests created or updated by current user, non-cancelled
- Search numeric: matches request ID
- Search text: matches updater name or comment field
- Wildcard `*`: shows all requests (no user filter)
- Sort: descending by ID

### Create Behavior
- Creates immediately with `Date = now`; redirects to Edit
- Note: the original commented-out per-line Create action has been superseded; new requests are created as a header first, then lines are added

### Header Fields

| Field | Column | Notes |
|-------|--------|-------|
| Warehouse | `purchase_request.warehouse` | Default destination |
| Serial/Folio | `purchase_request.serial` | Auto |
| Date | `purchase_request.date` | Set on create |
| Notes | `purchase_request.comment` | |

### Line Item Fields (`purchase_request_detail`)

| Field | Column | Notes |
|-------|--------|-------|
| Product | `product` | FK → `product` |
| Product Name | `product_name` | Snapshot or manual override |
| Quantity | `quantity` | Requested amount |
| Destination Warehouse | `warehouse` | Per-line override |
| Customer | `customer` | If requested for a specific customer |
| Mark for Purchase | `to_purchase` | Boolean — purchasing agent flags this line for PO conversion |
| Accepted | `accepted` | Boolean — approved for PO inclusion |

### Actions
- **Approve** (requires `PurchaseRequestApproval.AllowCreate`): sets `approved = 1`
- **Complete**: sets `completed = 1` after PO is created
- **Cancel**: sets `cancelled = 1`

### Business Rules
- If `WebConfig.PurchaseRequestApprovalRequired = true`: only lines where `to_purchase = 1` (marked by purchasing agent) are included when auto-generating POs.
- Minimum order quantity (`product.min_order_qty`) warning shown on line entry.

---

## 2. Purchase Request Approval

**Route**: `GET /purchase-requests/approval`  
**SystemObject**: `PurchaseRequestApproval` (96)

### Purpose
Supervisor queue to review and approve/reject pending purchase requests.

### Approval Queue Filter
- Requests where `approved = 0` and `cancelled = 0`

### Features
- List pending requests with drill-down to detail view
- Bulk approve: marks `approved = 1` for selected requests
- Reject: logs reason via `incidence` log
- View request detail before deciding

---

## 3. Purchase Orders

**Route**: `GET /purchases`  
**Controller**: `PurchasesController`  
**SystemObject**: `PurchasesOrders` (20)

### Purpose
Formal purchase orders sent to suppliers. Can be created manually, from approved purchase requests, or auto-generated per supplier from the "To Purchase by Supplier" view.

### List View
- Default: shows orders where current user is creator or updater, non-cancelled
- Search numeric: matches order ID
- Search text: matches supplier name or warehouse name; wildcard `*` shows all non-cancelled
- Sort: descending by ID

### Create from Purchase Requests (`CreatePurchaseBySupplier`)
The "To Purchase by Supplier" view (`ToPurchaseBySupplier`) groups pending purchase request lines by supplier:
- Shows: supplier name, count of pending lines, estimated total (at cost price)
- One click creates a full `PurchaseOrder` + `PurchaseOrderDetail` lines for all pending PR lines of that supplier
- Price taken from cost price list (`product_price WHERE list = 0`)
- If `WebConfig.PurchaseRequestApprovalRequired`: only includes lines where `to_purchase = 1`
- PR lines already linked to a PO detail are excluded

### Header Fields

| Field | Column | Notes |
|-------|--------|-------|
| Supplier | `purchase_order.supplier` | FK → `supplier` |
| Estimated Receipt Date | `purchase_order.estimated_receipt_date` | |
| Invoice Number | `purchase_order.invoice_number` | Supplier's invoice reference |
| Notes | `purchase_order.comment` | |

### Line Item Fields (`purchase_order_detail`)

| Field | Column | Notes |
|-------|--------|-------|
| Product | `product` | FK → `product` |
| Destination Warehouse | `warehouse` | FK → `warehouse` |
| Quantity | `quantity` | |
| Unit Price | `price` | Purchase cost |
| Discount % | `discount` | 0–1 |
| Tax Rate | `tax_rate` | From product |
| Tax Included | `tax_included` | |
| Currency | `currency` | |
| Exchange Rate | `exchange_rate` | |
| Origin PR Line | `purchase_request_detail` | Optional FK for traceability |

### Approval Queue (`Approvals`)
Filter: `IsCompleted = true AND IsApproved = false AND IsCancelled = false`
- Search by supplier name or creator nickname

### Approve Action
1. Sets `Approver = current_employee`
2. Sets `IsApproved = approve` (can approve or reject)
3. Sets `IsCompleted = approve` (unapproved orders revert to non-completed)
4. If `WebConfig.PurchaseOrderApprovalRequired = true AND approve = true`: calls `GenerateInventoryEntries` (auto-creates inventory receipts from this PO)

### Actions
- **Complete**: sets `IsCompleted = true`; moves to approval queue if approval required
- **Approve**: from approval queue (see above)
- **Receive**: navigate to Inventory → Receipts, create a receipt linked to this PO
- **Cancel**: sets `IsCancelled = true`
- **Print/Email**: purchase order document to supplier

### Business Rules
- A PO cannot be edited once `IsCompleted = true` or `IsCancelled = true`.
- `IsCompleted` is set to `true` when the order is submitted for approval or when fully received.
- If `WebConfig.PurchaseOrderApprovalRequired = true` and PO is not approved, receiving (inventory entry) is blocked.

---

## 4. Purchase Order Approval

**Route**: `GET /purchases/approvals`  
**Action**: `PurchasesController.Approvals`  
**SystemObject**: `PurchaseOrderApproval` (95)

Supervisor queue for approving completed-but-not-approved purchase orders (see Purchase Orders § Approval Queue above).

---

## 5. Expense Vouchers (Ticket de Gastos)

**Route**: `GET /expense-voucher`  
**Controller**: `ExpenseVoucherController`  
**SystemObject**: `ExpenseTicket` (82)

### Purpose
Petty cash / expense ticket issued during a cash session. Records miscellaneous cash outflows from the drawer.

### List View
- Filter by: store, cash session, date range, status
- Columns: ID, date, store, session, amount, status

### Header Fields

| Field | Column | Notes |
|-------|--------|-------|
| Store | `expense_voucher.store` | From user context |
| Cash Session | `expense_voucher.cash_session` | Must be open |
| Date | `expense_voucher.date` | |
| Notes | `expense_voucher.comment` | |

### Line Item Fields (`expense_voucher_detail`)

| Field | Column | Notes |
|-------|--------|-------|
| Expense Category | `expense` | FK → `expenses` |
| Amount | `amount` | |
| Note | `comment` | |

### Actions
- **Complete**: posts the expense; reduces expected cash in session closing count
- **Cancel**: voids

### Business Rules
- An open `cash_session` is required to create expense vouchers.
- Completed vouchers reduce the session's expected closing cash amount.

---

## 6. Supplier Payments

**Route**: `GET /supplier-payments`  
**Controller**: `SupplierPaymentsController`  
**SystemObject**: `SupplierPayment` (21)

### Purpose
Record payments made to suppliers for purchase orders.

### List View
- Filter by: supplier, date range, payment method
- Columns: ID, date, supplier, method, amount, reference

### Form Fields (`supplier_payment`)

| Field | Column | Notes |
|-------|--------|-------|
| Supplier | `supplier_payment.supplier` | FK → `supplier` |
| Amount | `supplier_payment.amount` | |
| Payment Method | `supplier_payment.method` | Enum |
| Date | `supplier_payment.date` | |
| Reference | `supplier_payment.reference` | Check/transfer reference |
| Notes | `supplier_payment.comment` | |

### Business Rules
- Supplier payments are standalone records not directly FK-linked to a specific PO in the schema.
- Reconciliation between PO amounts and payments is done via the Accounts Payable view.
- Total paid by supplier is computed by summing `supplier_payment.amount` per supplier.
