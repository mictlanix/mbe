# Production Specs

---

## 1. Production Orders

**Route**: `GET /production-orders`  
**SystemObject**: `ProductionOrders`

### Purpose
Manage manufacturing/production work orders. A production order tracks what is being produced, from which production site, and the materials consumed.

> **Note**: The `production_order` table is not present in the current schema dump. This module is likely partially implemented or planned. The spec below describes expected behavior based on context from `production_site` and related catalog data.

### Expected List View
- Filter by: store, production site, date range, status (open/completed/cancelled)
- Columns: folio, date, production site, product to produce, quantity, status

### Expected Header Fields

| Field | Notes |
|-------|-------|
| Store | From user context |
| Production Site | FK → `production_site` |
| Product to Produce | FK → `product` (must be `stockable=true`) |
| Quantity | Target output quantity |
| Date | Planned production date |
| Completion Date | Actual completion |
| Notes | Free text |

### Expected Line Items (Bill of Materials)

| Field | Notes |
|-------|-------|
| Raw Material / Component | FK → `product` |
| Required Quantity | Per unit of output |
| Warehouse (Source) | Where materials are drawn from |

### Expected Actions
- **Start**: records start time, reserves inventory
- **Complete**: creates an inventory receipt for output product and inventory issues for consumed materials
- **Cancel**: releases reserved materials

### Business Rules
- Completing a production order should:
  - Issue a `inventory_issue` for each consumed raw material
  - Create an `inventory_receipt` for the produced goods
  - Link both to the production order as source
- Production site must belong to the user's store.
