# Production Specs

---

## 1. Production Orders

**Route**: `GET /production-orders`  
**Controller**: `ProductionOrdersController`  
**SystemObject**: `ProductionOrders` (57)

### Status: NOT IMPLEMENTED

> **Important**: `ProductionOrdersController.cs` exists but its entire body is commented out — no actions are active. The `production_order` table is also absent from the database schema. This module is a planned but **unimplemented** feature.

The menu entry for Production Orders exists in `_Menu.cshtml` and the `SystemObjects` enum includes `ProductionOrders = 57`, so the privilege scaffolding is in place, but no functional controller code exists.

### Implication for Migration

When migrating to Flutter + Python:
- **Do not reverse-engineer this module** — there is no working source to document.
- Design production orders from scratch or adopt an external BOM/MRP approach.
- The `production_site` entity (see Master Data spec) IS implemented and should be used as the production location anchor.

### Likely Intent (inferred from catalog data and naming)

Based on `production_site`, the menu structure, and the commented-out search query referencing non-stockable product orders:

| Concept | Notes |
|---------|-------|
| Production order | Work order to manufacture a product |
| Source | Triggered by completed sales orders with non-stockable lines (service/made-to-order) |
| Bill of Materials | Consume raw material via `inventory_issue`, produce output via `inventory_receipt` |
| Production site | Where production takes place (`production_site` table) |

### Recommendation for New Implementation

1. Add a `production_order` table with: store, production_site, product (output), quantity, date, status
2. Add `production_order_detail` for bill of materials: component product, quantity, source warehouse
3. Complete action: post inventory issues (components) and inventory receipt (output) via the same `InventoryHelpers.ChangeNotification` path used elsewhere
4. Link to sales order lines where `delivery = false` and `product.IsStockable = false`
