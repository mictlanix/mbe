# Inventory Specs

Covers all physical stock movements: receipts (in), issues (out), transfers (between warehouses), adjustments, and lot/serial tracking.

All inventory transactions post to `lot_serial_tracking` via `InventoryHelpers.ChangeNotification`, which creates entries with `TransactionType` identifying the source document.

---

## Common Patterns

### Warehouse Scope
Every inventory action is scoped to the **user's POS warehouse** (`WebConfig.PointOfSale.Warehouse`). Users only see and can edit receipts/issues/transfers for their assigned warehouse. The `*` wildcard (with `AllowDelete` privilege) shows all records globally.

### Confirm / Complete Sequence
Every movement type follows: Create → Edit lines → Confirm (post to ledger). Once confirmed (`IsCompleted = true`), documents are read-only. Cancellation sets `IsCancelled = true` and may reverse ledger entries depending on type.

### Incidence Logging
All operations log inventory change notifications via `InventoryHelpers.ChangeNotification(TransactionType, docId, datetime, warehouse, null, product, ±quantity)`.

---

## 1. Inventory Receipts

**Route**: `GET /inventory/receipts`  
**Controller**: `InventoryController` (action: `Receipts`)  
**SystemObject**: `InventoryReceipts` (15)

### Purpose
Record goods arriving into a warehouse. Typically follows a purchase order but can be standalone.

### List View
- Default filter: receipts for user's POS warehouse OR created by current user
- Search numeric: matches PO ID, receipt ID, or serial
- Search text: matches warehouse name
- Wildcard `*` (requires `AllowDelete` privilege): shows all receipts globally
- Sort: descending by ID

### Header Fields

| Field | Column | Notes |
|-------|--------|-------|
| Store | `inventory_receipt.store` | Auto-set from warehouse's store |
| Serial/Folio | `inventory_receipt.serial` | Assigned on Confirm (MAX+1 for store) |
| Warehouse | `inventory_receipt.warehouse` | Destination; must match user's POS warehouse to edit |
| Linked Purchase Order | `inventory_receipt.purchase_order` | Optional FK → `purchase_order` |
| Notes | `inventory_receipt.comment` | |

### Line Item Fields (`inventory_receipt_detail`)

| Field | Column | Notes |
|-------|--------|-------|
| Product | `product` | FK → `product` (must be `stockable = true`) |
| Linked PO Line | `purchase_order_detail` | Optional FK |
| Quantity Ordered | `quantity_ordered` | From PO line (display reference) |
| Quantity Received | `quantity` | Actual quantity received |
| Product Code | `product_code` | Snapshot of code at time of receipt |
| Product Name | `product_name` | Snapshot |

> Lines can only be added/removed/edited when `receipt.Warehouse == user's POS warehouse`.

### Complementary Receipt
An additional "complementary receipt" can be created from an existing one (`CreateComplementaryReceipt`) for the same PO — used to handle back-ordered items.

### Confirm Action (`ConfirmReceipt`)
1. Validates: all products must be `IsStockable = true` (non-stockable products rejected)
2. Sets `Store = warehouse.Store`
3. Assigns `Serial = MAX(serial FOR store) + 1`
4. Sets `IsCompleted = true`
5. Posts `InventoryHelpers.ChangeNotification(TransactionType.InventoryReceipt, ..., +quantity)` for each line

### Cancel Action (`CancelReceipt`)
- Sets `IsCancelled = true`
- Does NOT automatically reverse ledger entries (cancellation of completed receipts must be handled via Inventory Issue or Adjustment)

### Business Rules
- Cannot complete if warehouse doesn't match user's POS warehouse (non-admin).
- Completed receipts are locked — only cancellation is allowed.
- If linked to a purchase order, receiving more than ordered shows a warning (no hard block).

---

## 2. Inventory Issues

**Route**: `GET /inventory/issues`  
**Controller**: `InventoryController` (action: `Issues`)  
**SystemObject**: `InventoryIssues` (16)

### Purpose
Record goods leaving a warehouse (write-offs, waste, supplier returns, internal consumption).

### List View
- Filter: user's POS warehouse scope
- Columns: folio, date, warehouse, linked supplier return, status

### Header Fields

| Field | Column | Notes |
|-------|--------|-------|
| Store | `inventory_issue.store` | |
| Serial/Folio | `inventory_issue.serial` | |
| Warehouse | `inventory_issue.warehouse` | Source warehouse |
| Linked Supplier Return | `inventory_issue.supplier_return` | Optional FK → `supplier_return` |
| Notes | `inventory_issue.comment` | |

### Line Item Fields (`inventory_issue_detail`)

| Field | Column | Notes |
|-------|--------|-------|
| Product | `product` | FK → `product` |
| Quantity | `quantity` | Quantity to remove |
| Product Code | `product_code` | Snapshot |
| Product Name | `product_name` | Snapshot |

### Lot/Serial Selection
For products with `perishable = true` or `seriable = true`, user must specify which lot/serial numbers to consume. FIFO (earliest expiration date, then earliest tracking date) is the recommended default.

### Confirm Action
- Posts `InventoryHelpers.ChangeNotification(TransactionType.InventoryIssue, ..., −quantity)` for each line
- Sets `IsCompleted = true`

### Business Rules
- Cannot issue more than current available stock.
- Completed issues are locked; only cancellation possible.

---

## 3. Inventory Transfers

**Route**: `GET /inventory/transfers`  
**Controller**: `InventoryController` (action: `Transfers`)  
**SystemObject**: `InventoryTransfers` (17)

### Purpose
Move stock between two warehouses within the same store.

### List View
- Filter: user's POS warehouse scope (from/to)

### Header Fields

| Field | Column | Notes |
|-------|--------|-------|
| Store | `inventory_transfer.store` | |
| Serial/Folio | `inventory_transfer.serial` | |
| From Warehouse | `inventory_transfer.warehouse` | Source |
| To Warehouse | `inventory_transfer.warehouse_to` | Destination |
| Notes | `inventory_transfer.comment` | |

### Line Item Fields (`inventory_transfer_detail`)

| Field | Column | Notes |
|-------|--------|-------|
| Product | `product` | FK → `product` |
| Quantity | `quantity` | |
| Product Code | `product_code` | Snapshot |
| Product Name | `product_name` | Snapshot |

### Confirm Action
- Posts two `InventoryHelpers.ChangeNotification` calls per line:
  - Negative entry for source warehouse
  - Positive entry for destination warehouse
- `TransactionType.InventoryTransfer`

### Business Rules
- Source and destination warehouses must be different.
- Cannot transfer more than available stock in source.
- Cross-store transfers are not supported; use Issue + Receipt between stores.

---

## 4. Inventory Adjustments

**Route**: `GET /inventory/adjustments`  
**SystemObject**: `InventoryAdjustments` (93)

### Purpose
Manual stock corrections after a physical count. Adds or removes stock without a linked purchase or sales document.

### Features
- Enter actual counted quantities per product per warehouse
- System computes delta vs current ledger balance
- Positive delta → issues a positive `lot_serial_tracking` entry
- Negative delta → issues a negative entry

### Key Tables
- `lot_serial_tracking` (direct insert/update)

---

## 5. Physical Count Adjustment

**SystemObject**: `PhysicalCountAdjustment` (74)

### Purpose
Supervisor-level tool for bulk physical inventory count entry. Requires elevated privilege separate from normal adjustments.

### Distinction from Inventory Adjustments
- `PhysicalCountAdjustment (74)` is the privilege gate for bulk count operations.
- `InventoryAdjustments (93)` gates the standard adjustment form.
- In practice, both post to `lot_serial_tracking`.

---

## 6. Lot / Serial Numbers

**Route**: `GET /inventory/lot-serial-numbers`  
**SystemObject**: `LotSerialNumbers` (42)

### Purpose
View and query the lot/serial tracking ledger. Shows current inventory by lot or serial number with full movement history.

### Features

#### Current Stock View
- Filter by: warehouse, product, lot number, serial number, expiration date range
- Columns: warehouse, product, lot/serial, expiration, qty on hand

#### Movement History
- Shows all `lot_serial_tracking` entries for selected filters
- Columns: date, `TransactionType` (source document type), document ID, warehouse, quantity (±)

#### Expiry Alerts
- Highlight lots expiring within a configurable threshold
- Export expiring-soon list

### Key Tables
- `lot_serial_tracking` — full movement ledger (one row per document line)
- `lot_serial_rqmt` — pending reservations (reserved but not yet fulfilled)

### Quantity Calculation
Stock on hand = `SUM(lst.quantity)` grouped by `(warehouse, product, lot_number, serial_number)` where sum > 0

### Business Rules
- A serial number should appear in exactly one location (quantity = 0 or 1 per location).
- FIFO picking order: oldest lot (earliest `expiration_date`, then earliest tracking `date`) is consumed first.
- Products with `perishable = false` and `seriable = false` still get tracking entries — the lot/serial fields are null.
