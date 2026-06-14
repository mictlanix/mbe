# Inventory Specs

Covers all physical stock movements: receipts (in), issues (out), transfers (between warehouses), and lot/serial tracking.

All inventory transactions post to `lot_serial_tracking` for products where `perishable=true` or `seriable=true`.

---

## 1. Inventory Receipts

**Route**: `GET /inventory/receipts`  
**SystemObject**: `InventoryReceipts`

### Purpose
Record goods arriving into a warehouse. Typically follows a purchase order but can also be standalone.

### List View
- Filter by: store, warehouse, date range, status (open/completed/cancelled)
- Columns: folio, date, warehouse, linked PO, creator, status

### Header Fields

| Field | Column | Notes |
|-------|--------|-------|
| Store | `inventory_receipt.store` | From user context |
| Serial/Folio | `inventory_receipt.serial` | Auto per store |
| Warehouse | `inventory_receipt.warehouse` | Destination warehouse |
| Linked Purchase Order | `inventory_receipt.purchase_order` | Optional FK → `purchase_order` |
| Notes | `inventory_receipt.comment` | |

### Line Item Fields (`inventory_receipt_detail`)

| Field | Column | Notes |
|-------|--------|-------|
| Product | `product` | FK → `product` |
| Linked PO Line | `purchase_order_detail` | Optional — if from PO |
| Quantity Ordered | `quantity_ordered` | From PO line (display only) |
| Quantity Received | `quantity` | Actual amount received |
| Product Code | `product_code` | Snapshot |
| Product Name | `product_name` | Snapshot |

### Lot/Serial Entry
When a line's product has `perishable=true` or `seriable=true`, an additional sub-form captures:
- Lot number (`lot_serial_tracking.lot_number`)
- Expiration date (`lot_serial_tracking.expiration_date`) — perishable only
- Serial number (`lot_serial_tracking.serial_number`) — seriable only
- Quantity per lot/serial entry

### Actions
- **Complete**: posts stock to warehouse; creates `lot_serial_tracking` entries (`source = InventoryReceipt`)
- **Cancel**: voids the document; reverses any partial entries

### Business Rules
- Cannot complete if any line has `quantity = 0`.
- If linked to a purchase order, receiving more than ordered shows a warning.
- Completed receipts cannot be edited; only cancellation is allowed.
- Cancelled receipts reverse all `lot_serial_tracking` entries they created.

---

## 2. Inventory Issues

**Route**: `GET /inventory/issues`  
**SystemObject**: `InventoryIssues`

### Purpose
Record goods leaving a warehouse (write-offs, waste, supplier returns).

### List View
- Filter by: store, warehouse, date range, status
- Columns: folio, date, warehouse, linked supplier return, status

### Header Fields

| Field | Column | Notes |
|-------|--------|-------|
| Store | `inventory_issue.store` | |
| Serial/Folio | `inventory_issue.serial` | |
| Warehouse | `inventory_issue.warehouse` | Source warehouse |
| Linked Supplier Return | `inventory_issue.supplier_return` | Optional FK |
| Notes | `inventory_issue.comment` | |

### Line Item Fields (`inventory_issue_detail`)

| Field | Column | Notes |
|-------|--------|-------|
| Product | `product` | FK → `product` |
| Quantity | `quantity` | Quantity to remove |
| Product Code | `product_code` | Snapshot |
| Product Name | `product_name` | Snapshot |

### Lot/Serial Selection
For products with tracking, user must specify which lot/serial numbers to consume (FIFO enforced by default).

### Actions
- **Complete**: removes stock from warehouse; creates negative `lot_serial_tracking` entries
- **Cancel**: reverses

### Business Rules
- Cannot issue more than current available stock.
- Completed issues are locked; only cancellation possible.

---

## 3. Inventory Transfers

**Route**: `GET /inventory/transfers`  
**SystemObject**: `InventoryTransfers`

### Purpose
Move stock between two warehouses within the same store.

### List View
- Filter by: store, from warehouse, to warehouse, date range, status

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

### Actions
- **Complete**: creates a negative `lot_serial_tracking` entry for source warehouse and a positive one for destination
- **Cancel**: reverses

### Business Rules
- Source and destination warehouses must be different.
- Cannot transfer more than available stock in source.
- Cross-store transfers are not supported (use issue + receipt between stores).

---

## 4. Lot / Serial Numbers

**Route**: `GET /inventory/lot-serial-numbers`  
**SystemObject**: `LotSerialNumbers`

### Purpose
View and query the lot/serial tracking ledger. Shows current inventory by lot or serial number, with full movement history.

### Features

#### Current Stock View
- Filter by: warehouse, product, lot number, serial number, expiration date range
- Columns: warehouse, product, lot/serial, expiration, qty on hand

#### Movement History
- Shows all `lot_serial_tracking` entries for selected filters
- Columns: date, source document type, document folio/ID, warehouse, quantity (±)

#### Expiry Alerts
- Highlight lots expiring within configurable days threshold
- Export expiring-soon list

### Key Tables
- `lot_serial_tracking` — full ledger
- `lot_serial_rqmt` — pending requirements (reserved but not yet tracked)

### Business Rules
- Stock on hand = SUM(quantity) grouped by warehouse + product + lot + serial, where sum > 0
- A serial number should appear in exactly one location at a time (quantity must be 0 or 1 per location)
- FIFO picking order: oldest lot (earliest expiration_date, then earliest tracking date) first
