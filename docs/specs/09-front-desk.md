# Front Desk Specs

Covers administrative/clerical services managed at the front desk: document translation requests and notarization tracking.

---

## 1. Translation Requests

**Route**: `GET /translation-requests`  
**SystemObject**: `TranslationRequests`

### Purpose
Track requests for document translation sent to external agencies. Used by administrative staff to manage turnaround, cost, and delivery of translated documents.

### List View
- Filter by: requester, date range, agency, delivery date range
- Columns: ID, date, requester, agency, document name, amount, delivery date

### Form Fields (`translation_request`)

| Field | Column | Notes |
|-------|--------|-------|
| Requester | `requester` | FK → `employee` — who is requesting |
| Request Date | `date` | |
| Agency | `agency` | Translation agency name |
| Document Name | `document_name` | Name/title of document being translated |
| Amount | `amount` | Cost of translation |
| Expected Delivery Date | `delivery_date` | When translated document is expected |
| Notes | `comment` | |

### Actions
- **Mark Delivered**: update status / close request (via `comment` or status flag if added)
- **Print Request**: printable request form for agency

### Business Rules
- `requester` must be an active employee.
- Cost tracking supports budget reporting for front desk services.

---

## 2. Notarizations

**Route**: `GET /notarizations`  
**SystemObject**: `Notarizations`

### Purpose
Track notarization processes managed through a notary office. Records request details, cost, payment, and expected delivery of notarized documents.

### List View
- Filter by: requester, notary office, date range, delivery date range
- Columns: ID, date, requester, notary office, document description, amount, payment date, delivery date

### Form Fields (`notarization`)

| Field | Column | Notes |
|-------|--------|-------|
| Requester | `requester` | FK → `employee` |
| Notary Office | `notary_office` | Office name |
| Request Date | `date` | |
| Document Description | `document_description` | What is being notarized |
| Amount | `amount` | Notarization fee |
| Payment Date | `payment_date` | When fee is/was paid |
| Expected Delivery Date | `delivery_date` | When notarized document returns |
| Notes | `comment` | |

### Actions
- **Mark Received**: record that notarized document has been returned
- **Print**: request summary

### Business Rules
- Both `payment_date` and `delivery_date` are tracked independently (payment may occur before document is ready).
- `requester` must be an active employee.
