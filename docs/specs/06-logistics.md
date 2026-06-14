# Logistics Specs

Covers the delivery workflow: creating delivery orders from sales, planning routes (itineraries), and confirming deliveries.

---

## Delivery Flow

```
Sales Order (completed, not cancelled)
    │
    ▼ [create DO from SO folio]
Delivery Order   ──► IsPickedUpInStore?  ──YES──► Counter Pickup (no itinerary)
    │
    ▼ (IsConfirmed = true)
Delivery Order Approval (if WebConfig.DeliveryOrderApprovalRequired)
    │
    ▼
For Delivery view  ←── warehouse staff loads truck
    │
    ▼
Delivery Itinerary  (vehicle + operator + SentQuantity per DO line)
    │
    ▼ [Confirm itinerary]
DeliveryOrder.IsDelivered = true  (when all quantity sent)
```

**Key quantity formula** (used throughout):
> `RemainingToDeliver(detail)` = `delivery_order_detail.quantity` − SUM(`deliveries_itinerary_detail.quantity` WHERE itinerary NOT cancelled)

---

## 1. Delivery Orders

**Route**: `GET /delivery-orders`  
**Controller**: `DeliveryOrdersController`  
**SystemObject**: `DeliveryOrders`

### Purpose
Picking/shipping documents generated one per sales order. Lines are derived from the sales order's deliverable items. The system tracks how much of each line has already been loaded onto itineraries.

### List View
- Default filter: orders created or updated by current user, non-cancelled
- Search by: customer name, order folio/ID, or delivery order ID
- Wildcard search (`*`) lifts the creator filter and shows all orders
- Sort: descending by ID
- Columns: folio, date, customer, ship-to, priority, status, delivered, picked-up

### Creation from a Sales Order

Triggered by entering a Sales Order ID into the "New" action:

1. System validates the sales order:
   - Must be `completed=1` and `cancelled=0`
   - If `WebConfig.DeliveryOrderRequiresPaidOrCreditSalesOrder`: order must be paid or have credit terms (non-cash)
   - `DeliveryMode` must NOT be `PickUp` (pickup orders cannot generate a delivery order)
2. System calls `CreateFromSalesOrder(sales_order_id)`:
   - Computes deliverable lines = `sales_order_detail` lines where `delivery=true` and `GetDeliverableQuantity() > 0`
   - `GetDeliverableQuantity()` = line quantity minus quantity already covered by existing delivery order details
   - If no deliverable lines remain, returns error `AlreadyFullyDelivered`
3. `IsPickedUpInStore` is auto-detected: `true` if `ShipTo` address matches any store's address
4. If `WebConfig.DeliveryOrderApprovalRequired = false`, `IsConfirmed` is set to `true` immediately on completion
5. Sales Order's `DeliveryMode` is set to `PartialDeliveries` after first DO is created

### Header Fields

| Field | Column | Notes |
|-------|--------|-------|
| Store | `delivery_order.store` | FK → `store` |
| Serial/Folio | `delivery_order.serial` | Auto per store |
| Customer | `delivery_order.customer` | FK → `customer` |
| Ship To | `delivery_order.ship_to` | FK → `address` — delivery address |
| Contact | `delivery_order.contact` | FK → `contact` |
| Scheduled Date | `delivery_order.date` | Must be ≥ NOW + `WebConfig.MinSpanHoursForDeliveries` |
| Priority | `delivery_order.priority` | Integer (higher = more urgent) |
| Is Picked Up In Store | `delivery_order.picked_up` | Auto-detected from ShipTo address |
| Is Confirmed | `delivery_order.confirmed` | Approval flag |
| Is Delivered | `delivery_order.delivered` | Set when all lines are fully sent |
| Notes | `delivery_order.comment` | |

### Line Item Fields (`delivery_order_detail`)

| Field | Column | Notes |
|-------|--------|-------|
| Sales Order Line | `sales_order_detail` | Optional FK — traceability to origin SO line |
| Product | `product` | FK → `product` |
| Quantity | `quantity` | Total quantity to deliver for this DO |
| Product Code | `product_code` | Snapshot |
| Product Name | `product_name` | Snapshot |

> Remaining quantity per line = `quantity` − SUM of non-cancelled itinerary detail quantities for this line.

### Print / Ticket

- `WebConfig.DeliveryOrdersUseMiniPrinter = true`: uses thermal ticket format
  - `IsPickedUpInStore = true` → uses `WebConfig.PickUpTicket` template
  - `IsPickedUpInStore = false` → uses `WebConfig.DeliveryOrderTicket` template
- `WebConfig.DeliveryOrdersUseMiniPrinter = false`: uses standard PDF via `WebConfig.DeliveryOrderTemplate`
- Filter `only_current_warehouse=true`: prints only lines whose warehouse matches the current POS warehouse

### Actions
- **Complete** (`completed=1`): finalizes the DO; enables approval and itinerary loading
- **Confirm Delivery** (`delivered=1`): marks physical delivery as done
- **Mark Picked Up** (`picked_up=1`): for counter-pickup orders
- **Cancel**: sets `cancelled=1`; does not reverse itinerary entries

### Business Rules
- `date` must be at least `WebConfig.MinSpanHoursForDeliveries` hours from now (enforced unless user is admin)
- Counter pickups (`IsPickedUpInStore = true`) bypass the approval queue (no approval needed)
- A DO cannot be edited once `completed=1` or `cancelled=1`
- Adding items from a second sales order to the same DO is allowed (multi-SO delivery)

---

## 2. Delivery Order Approval

**Route**: `GET /delivery-orders/approval`  
**Action**: `DeliveryOrdersApproval`  
**SystemObject**: `DeliveryOrderApproval`

### Purpose
Supervisor queue to review and confirm delivery orders before they appear in the itinerary loading view. Only relevant when `WebConfig.DeliveryOrderApprovalRequired = true`.

### Approval Queue Filter
```
delivery_order WHERE:
  IsCancelled = false
  AND IsCompleted = true
  AND IsConfirmed = false (or null)
  AND ShipTo IS NOT NULL   ← counter pickups (ShipTo = store address) are excluded
```

### Actions per Order
- **Approve** (`DeliveryRequestConfirmation confirmation=true`): sets `IsConfirmed = true`
- **Reject** (`DeliveryRequestConfirmation confirmation=false`): sets `IsConfirmed = false`, logs reason via `incidence`
- **View Detail**: read-only view of the delivery order before deciding

### Business Rules
- If `WebConfig.DeliveryOrderApprovalRequired = false`, orders are auto-confirmed (`IsConfirmed = true`) when completed — this queue is empty.
- Rejection does not cancel the DO; it blocks it from appearing in the For Delivery view until re-approved.
- Counter pickup orders (ShipTo = store address) never go through this queue.

---

## 3. For Delivery (Pending Deliveries)

**Route**: `GET /delivery-itineraries/deliveries`  
**Action**: `DeliveryItinerariesController.Deliveries`  
**SystemObject**: `ForDeliver`

### Purpose
Date-grouped view of delivery order lines pending loading onto an itinerary. Used by warehouse staff to see what needs to go out each day.

### Data Filter
Shows `delivery_order_detail` records where:
- `delivery_order.IsCancelled = false`
- `delivery_order.IsCompleted = true`
- `delivery_order.ShipTo` is NOT a store pickup address
- `delivery_order.store.IsDisabled = false`

### Date Grouping
The view is organized into tabs covering a sliding window of **4 days** (1 day back, today, 1 day ahead, 1 more day ahead) plus overflow buckets:

| Tab | Content |
|-----|---------|
| Previous Deliveries | DO lines with `date < today − 1 day` (up to page size) |
| Yesterday | DO lines scheduled for yesterday |
| **Today** (selected by default) | DO lines scheduled for today |
| Tomorrow | DO lines scheduled for tomorrow |
| Day after tomorrow | DO lines for +2 days |
| Following Deliveries | DO lines with `date > today + 2 days` (up to page size) |

Lines within a tab are sorted descending by sales order priority.

### Remaining Quantity Display
For each line: `Remaining = delivery_order_detail.quantity − SUM(deliveries_itinerary_detail.quantity WHERE itinerary NOT cancelled)`

Lines where remaining = 0 are already fully loaded.

### Actions
- **Add to Itinerary**: select lines and assign to an existing open itinerary
- **Mark Picked Up**: sets `delivery_order.picked_up = true` for counter pickups that appear here

---

## 4. Delivery Itineraries

**Route**: `GET /delivery-itineraries`  
**Controller**: `DeliveryItinerariesController`  
**SystemObject**: `DeliveryItineraries`

### Purpose
Route/trip plan for a delivery date. Groups delivery order lines onto a vehicle with a driver. Tracks how much of each line is loaded (`SentQuantity`) and confirms the route as completed.

### List View
- Filter: non-cancelled itineraries; search by ID or customer name
- Default warehouse scope: from current user's POS warehouse setting
- Sort: descending by ID

### Header Fields

| Field | Column | Notes |
|-------|--------|-------|
| Date | `deliveries_itinerary.date` | Delivery date (defaults to today) |
| Vehicle | `deliveries_itinerary.vehicle` | FK → `vehicle` (optional) |
| Vehicle Operator | `deliveries_itinerary.vehicle_operator` | FK → `vehicle_operator` (optional) |
| Dispatch Warehouse | `deliveries_itinerary.warehouse` | Auto-set from user's POS warehouse |
| Notes | `deliveries_itinerary.comment` | |

### Line Item Fields (`deliveries_itinerary_detail`)

| Field | Column | Notes |
|-------|--------|-------|
| Delivery Order Line | `delivery_order_detail` | FK → `delivery_order_detail` |
| Sent Quantity | `quantity` (`SentQuantity`) | How much is loaded — ≤ `RemainingToDeliver` |
| Note | `comment` | |

**`SentQuantity` rules:**
- Default when adding: set to `GetRemainingQuantityToDeliver(detail)` (fill to remaining)
- User can reduce it for partial loads
- Capped at: `RemainingToDeliver + existing SentQuantity for this line on this itinerary`
- Lines with `SentQuantity = 0` are excluded from Confirm

### Actions
- **New**: creates itinerary immediately with warehouse from user's POS setting, redirects to Edit
- **Add DO Line**: pick a delivery order detail from the For Delivery queue and set SentQuantity
- **Add All Remaining from a DO**: fills SentQuantity = RemainingToDeliver for all lines of a delivery order
- **Confirm** (`Confirm(id)`): finalizes the route
  1. Validates: no line has `SentQuantity > RemainingToDeliver + original quantity`
  2. Filters to lines where `SentQuantity > 0`
  3. Validates not all SentQuantity = 0 (would be `AlreadyFullyDelivered`)
  4. Saves `deliveries_itinerary_detail` records
  5. Sets `deliveries_itinerary.IsCompleted = true`
  6. Updates `delivery_order.IsDelivered = true` for any DO whose all lines are now fully sent
- **Cancel**: sets `IsCancelled = true`; releases all SentQuantity back to remaining pool

### Business Rules
- Vehicle operator license must be active and not expired before assignment (advisory check).
- A DO line can appear on multiple itineraries only if partially loaded each time (cumulative SentQuantity ≤ line quantity).
- Once `IsCompleted = true`, itinerary is locked — no edits or additions.
- `WebConfig.DeliveryOrderApprovalRequired`: if true, only `IsConfirmed` delivery orders can have lines added to itineraries.
