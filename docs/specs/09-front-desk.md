# Front Desk Specs

Covers administrative/clerical services managed at the front desk: document translation requests and notarization tracking.

---

## 1. Translation Requests

**Route**: `GET /translation-requests`  
**Controller**: `TranslationRequestsController`  
**SystemObject**: `TranslationRequests` (59)

### Purpose
Track requests for document translation sent to external agencies. Used by administrative staff to manage turnaround, cost, and delivery of translated documents.

### List View
- Search by: agency name, document name
- No user-scope filter — all requests are visible
- Columns: ID, date, requester, agency, document name, amount, delivery date

### Form Fields (`translation_request`)

| Field | Column | Notes |
|-------|--------|-------|
| Requester | `requester` | FK → `employee` — who is requesting |
| Request Date | `date` | |
| Agency | `agency` | Translation agency name (searchable) |
| Document Name | `document_name` | Name/title of document being translated (searchable) |
| Amount | `amount` | Cost of translation |
| Expected Delivery Date | `delivery_date` | When translated document is expected |
| Notes | `comment` | |

### Actions
- **Create**: creates immediately, redirects to Edit
- **Edit**: update all fields
- **View**: read-only detail view
- **Delete** (`DeleteConfirmed`): hard delete — no soft delete, no workflow states

### Business Rules
- No confirm/cancel workflow — purely informational tracking.
- `requester` must be an active employee.

---

## 2. Notarizations

**Route**: `GET /notarizations`  
**Controller**: `NotarizationsController`  
**SystemObject**: `Notarizations` (60)

### Purpose
Track notarization processes managed through a notary office. Records request details, cost, payment, and expected delivery of notarized documents.

### List View
- Search by: notary office name, document description
- No user-scope filter — all records are visible
- Columns: ID, date, requester, notary office, document description, amount, payment date, delivery date

### Form Fields (`notarization`)

| Field | Column | Notes |
|-------|--------|-------|
| Requester | `requester` | FK → `employee` |
| Notary Office | `notary_office` | Office name (searchable) |
| Request Date | `date` | |
| Document Description | `document_description` | What is being notarized (searchable) |
| Amount | `amount` | Notarization fee |
| Payment Date | `payment_date` | When fee is/was paid |
| Expected Delivery Date | `delivery_date` | When notarized document returns |
| Notes | `comment` | |

### Actions
- **Create**: creates immediately, redirects to Edit
- **Edit**: update all fields
- **View**: read-only detail view
- **Delete** (`DeleteConfirmed`): hard delete — no soft delete

### Business Rules
- No confirm/cancel workflow — purely informational tracking.
- `payment_date` and `delivery_date` are tracked independently (payment often precedes document delivery).
- `requester` must be an active employee.
