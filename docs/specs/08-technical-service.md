# Technical Service Specs

Covers equipment service workflows: intake receipts, diagnostic reports, customer service requests, and fleet vehicle maintenance orders.

---

## 1. Technical Service Receipts

**Route**: `GET /technical-service-receipts`  
**Controller**: `TechnicalServiceReceiptsController`  
**SystemObject**: `TechnicalServiceReceipts` (65)

### Purpose
Equipment intake form. Records equipment received for service at the technical service counter — the physical check-in before any diagnosis or work begins.

### List View
- Search by: equipment name/type, brand, model, serial number
- Columns: ID, date, brand, equipment, model, serial number, location

### Form Fields (`tech_service_receipt`)

| Field | Column | Notes |
|-------|--------|-------|
| Brand | `brand` | Equipment brand |
| Equipment Type | `equipment` | Description (e.g. "Laptop", "Printer") |
| Model | `model` | Model name/number |
| Serial Number | `serial_number` | |
| Receipt Date | `date` | |
| Status | `status` | Free text condition description (e.g. "Powers on", "Dead on arrival") |
| Storage Location | `location` | Bin/shelf where equipment is stored |
| Checked By | `checker` | Receiving technician's name |
| Notes | `comment` | Observed conditions, accessories noted |

### Components Sub-Panel (`tech_service_receipt_component`)

| Field | Column | Notes |
|-------|--------|-------|
| Component Name | `name` | Accessory included (e.g. "Power adapter") |
| Quantity | `quantity` | |
| Serial Number | `serial_number` | |
| Notes | `comment` | |

### Actions
- **Create**: creates receipt record directly; redirects to Edit
- **Edit**: update fields and manage components
- **Print Receipt**: customer acknowledgment slip listing equipment and components received
- **Delete**: hard delete (no soft-delete flag); only allowed on non-linked receipts

### Business Rules
- Components sub-panel is managed inline on the Edit view.
- No workflow states (no confirm/cancel flow) — receipt is just a record.

---

## 2. Technical Service Reports

**Route**: `GET /technical-service-reports`  
**Controller**: `TechnicalServiceReportsController`  
**SystemObject**: `TechnicalServiceReports` (58)

### Purpose
Technician's diagnostic and repair report for a serviced piece of equipment. Documents work performed, cost, and outcome.

### List View
- Search by: service type, equipment name, brand, model, serial number
- Columns: ID, date, equipment, brand, model, serial number, technician, cost

### Form Fields (`tech_service_report`)

| Field | Column | Notes |
|-------|--------|-------|
| Date | `date` | Report date |
| Service Location | `location` | Where service was performed |
| Service Type | `type` | Searched as text; e.g. "Preventive", "Corrective" |
| Equipment | `equipment` | Equipment description |
| Brand | `brand` | |
| Model | `model` | |
| Serial Number | `serial_number` | |
| Equipment User | `user` | End-user name |
| Technician | `technician` | Name of technician who performed service |
| Service Cost | `cost` | Amount charged |
| Problem Reported by User | `user_report` | User's description of the issue |
| Technical Diagnosis | `description` | Technician's diagnosis and work performed |
| Notes | `comment` | |

### Actions
- **Create / Edit / Delete**: standard CRUD; no workflow states
- **Print**: service report document

---

## 3. Technical Service Requests

**Route**: `GET /technical-service-requests`  
**Controller**: `TechnicalServiceRequestsController`  
**SystemObject**: `TechnicalServiceRequests` (64)

### Purpose
Customer-facing service request. Tracks the full service lifecycle: intake → diagnosis → work → delivery.

### List View
- Search by: equipment name, brand, model, serial number
- Sort: all records, no creator-filter default
- Columns: ID, date, customer, equipment, brand, model, responsible technician, end date

### Form Fields (`tech_service_request`)

| Field | Column | Notes |
|-------|--------|-------|
| Service Type | `type` | Enum or category |
| Brand | `brand` | |
| Equipment | `equipment` | |
| Model | `model` | |
| Serial Number | `serial_number` | |
| Request Date | `date` | |
| End Date | `end_date` | Completion/return date |
| Customer | `customer` | FK → `customer` |
| Responsible Technician | `responsible` | Name (free text) |
| Service Location | `location` | |
| Payment Status | `payment_status` | e.g. "Pending", "Paid" |
| Shipping Method | `shipping_method` | How equipment arrives/departs |
| Contact Name | `contact_name` | |
| Contact Phone | `contact_phone_number` | |
| Service Address | `address` | If on-site service |
| Customer Remarks | `remarks` | |
| Internal Notes | `comment` | |

### Components Sub-Panel (`tech_service_request_component`)
Same structure as receipt components: name, quantity, serial number, notes. Lists accessories received with the equipment.

### Actions
- **Create**: creates immediately, redirects to Edit
- **Edit**: update all fields and manage components
- **Delete** (`DeleteConfirmed`): hard delete — no soft delete
- **Print Work Order**: printable service request document for technician

### Business Rules
- No confirm/cancel flow; status is tracked via `payment_status` and `end_date` fields.
- Customer is optional (walk-in service without account).

---

## 4. Vehicle Service Orders

**Route**: `GET /vehicles/service-orders`  
**Controller**: `VehiclesController` (action: `ServiceOrders`)  
**SystemObject**: `VehicleServiceOrders` (90)

### Purpose
Internal maintenance and repair orders for fleet vehicles. Tracks problems reported, work performed, and spare parts consumed.

### List View
- Search numeric: matches order ID
- Search text: matches vehicle name, vehicle nickname, or notifier's full name
- Columns: ID, vehicle, date, problem summary, status (completed flag)

### Header Fields (`service_order` / `vehicle_service_order`)

| Field | Column | Notes |
|-------|--------|-------|
| Vehicle | `vehicle` | FK → `vehicle` |
| Problem Description | `problem_description` | Reported issue |
| Service Description | `service_description` | Work performed |
| Date | `date` | Scheduled or completion date |
| Notifier | `notifier` | FK → `employee` — who reported the issue |
| Notes | `comment` | |

### Spare Parts Sub-Panel (`service_order_detail`)

| Field | Column | Notes |
|-------|--------|-------|
| Spare Part | `spare_part` | FK → `product` |
| Quantity | `quantity` | |
| Date Used | `date` | |
| Notes | `comment` | |

### Actions
- **Create** (`CreateServiceOrder`): creates order immediately; redirects to Edit
- **Edit** (`EditServiceOrder`): update all fields and spare parts
- **Delete** (`DeleteServiceOrderConfirmed`): hard delete
- **Add Item**: add spare part line to the order

### Business Rules
- Vehicle must be active (`vehicle.active = 1`) to create a service order.
- Spare parts are tracked for maintenance cost reporting but do NOT automatically deduct stock (no auto-inventory issue).
- Notifier and creator may be different employees.
