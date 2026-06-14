# Technical Service Specs

Covers equipment service workflows: intake receipts, diagnostic reports, customer service requests, and vehicle maintenance orders.

---

## 1. Technical Service Receipts

**Route**: `GET /technical-service-receipts`  
**SystemObject**: `TechnicalServiceReceipts`

### Purpose
Equipment intake form. Records equipment received for service at the technical service counter — the physical check-in before any diagnosis or work begins.

### List View
- Filter by: date range, status, brand, equipment type
- Columns: ID, date, brand, equipment, model, serial number, status, location

### Form Fields (`tech_service_receipt`)

| Field | Column | Notes |
|-------|--------|-------|
| Brand | `brand` | Equipment brand |
| Equipment Type | `equipment` | Description (e.g. "Laptop", "Printer") |
| Model | `model` | Model name/number |
| Serial Number | `serial_number` | |
| Receipt Date | `date` | |
| Status | `status` | Free text (e.g. "Powers on", "Dead on arrival") |
| Storage Location | `location` | Bin/shelf where stored |
| Checked By | `checker` | Name of receiving technician |
| Notes | `comment` | Observed conditions |

### Components Sub-Panel (`tech_service_receipt_component`)

| Field | Column | Notes |
|-------|--------|-------|
| Component Name | `name` | Accessory included (e.g. "Power adapter") |
| Quantity | `quantity` | |
| Serial Number | `serial_number` | |
| Notes | `comment` | |

### Actions
- **Print Receipt**: customer acknowledgment slip listing equipment and components received
- **Link to Service Request**: associate with an existing `tech_service_request`

---

## 2. Technical Service Reports

**Route**: `GET /technical-service-reports`  
**SystemObject**: `TechnicalServiceReports`

### Purpose
Technician's diagnostic and repair report for a serviced piece of equipment. Documents work performed, cost, and outcome.

### List View
- Filter by: date range, equipment type, brand, technician
- Columns: ID, date, equipment, brand, model, serial, technician, cost

### Form Fields (`tech_service_report`)

| Field | Column | Notes |
|-------|--------|-------|
| Date | `date` | Report date |
| Service Location | `location` | Where service was performed |
| Service Type | `type` | Enum or free text (e.g. "Preventive", "Corrective") |
| Equipment | `equipment` | Equipment description |
| Brand | `brand` | |
| Model | `model` | |
| Serial Number | `serial_number` | |
| Equipment User | `user` | End-user name |
| Technician | `technician` | Name of technician |
| Service Cost | `cost` | Amount charged |
| Problem Reported by User | `user_report` | User's description of the issue |
| Technical Diagnosis | `description` | Technician's diagnosis and work done |
| Notes | `comment` | |

---

## 3. Technical Service Requests

**Route**: `GET /technical-service-requests`  
**SystemObject**: `TechnicalServiceRequests`

### Purpose
Customer-facing service request. A customer submits a request for equipment servicing; this tracks the full lifecycle from intake to completion.

### List View
- Filter by: customer, date range, service type, payment status, end date
- Columns: ID, date, customer, equipment, brand, model, responsible, status, end date

### Form Fields (`tech_service_request`)

| Field | Column | Notes |
|-------|--------|-------|
| Service Type | `type` | Enum |
| Brand | `brand` | |
| Equipment | `equipment` | |
| Model | `model` | |
| Serial Number | `serial_number` | |
| Request Date | `date` | |
| End Date | `end_date` | Completion/return date |
| Customer | `customer` | FK → `customer` |
| Responsible Technician | `responsible` | Name |
| Service Location | `location` | |
| Payment Status | `payment_status` | e.g. "Pending", "Paid" |
| Shipping Method | `shipping_method` | How equipment arrives/departs |
| Contact Name | `contact_name` | |
| Contact Phone | `contact_phone_number` | |
| Service Address | `address` | If on-site service |
| Customer Remarks | `remarks` | |
| Internal Notes | `comment` | |

### Components Sub-Panel (`tech_service_request_component`)
Same structure as `tech_service_receipt_component` — lists accessories and components received with the equipment.

### Actions
- **Mark Complete**: sets `end_date` to today
- **Print Work Order**: printable service request form for technician

---

## 4. Vehicle Service Orders

**Route**: `GET /vehicles/service-orders`  
**SystemObject**: `VehicleServiceOrders`

### Purpose
Internal maintenance and repair orders for fleet vehicles. Tracks problems reported, work performed, and spare parts consumed.

### List View
- Filter by: vehicle, date range, status (open/completed/cancelled)
- Columns: ID, vehicle, date, problem summary, status, completed

### Header Fields (`vehicle_service_order`)

| Field | Column | Notes |
|-------|--------|-------|
| Vehicle | `vehicle` | FK → `vehicle` |
| Problem Description | `problem_description` | Reported issue |
| Service Description | `service_description` | Work performed |
| Date | `date` | Scheduled/completion date |
| Notifier | `notifier` | FK → `employee` — who reported the issue |
| Notes | `comment` | |

### Spare Parts Sub-Panel (`service_order_detail`)

| Field | Column | Notes |
|-------|--------|-------|
| Spare Part | `spare_part` | FK → `product` (must be `purchasable=true`) |
| Quantity | `quantity` | |
| Date Used | `date` | |
| Notes | `comment` | |

### Actions
- **Complete**: sets `completed=1`, records service description and date
- **Cancel**: sets `cancelled=1`

### Business Rules
- Spare parts used are deducted from inventory via a linked `inventory_issue` (optional, if integrated).
- Vehicle must be active (`vehicle.active = 1`) to create a service order.
- Notifier and creator may be different employees.
