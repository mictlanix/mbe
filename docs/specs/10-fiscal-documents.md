# Fiscal Documents Specs

Covers Mexican CFDI 4.0 e-invoicing: taxpayer (issuer) configuration and fiscal document generation, stamping, and cancellation.

---

## Background: CFDI 4.0

CFDI (Comprobante Fiscal Digital por Internet) is Mexico's mandatory electronic invoicing standard governed by the SAT (Servicio de Administración Tributaria). Every issued invoice must be:

1. Generated from a registered RFC (taxpayer issuer)
2. Digitally signed with a CSD certificate
3. Stamped (timbrado) by an authorized PAC (Proveedor Autorizado de Certificación)
4. UUID-assigned by SAT

Key catalogs used: `sat_cfdi_usage`, `sat_tax_regime`, `sat_product_service`, `sat_unit_of_measurement`, `sat_currency`, `sat_reason_cancellation`.

---

## 1. Taxpayers (Issuers)

**Route**: `GET /taxpayers`  
**SystemObject**: `Taxpayers`

### Purpose
Configure the company's RFC(s) used to issue CFDI documents. Each store is linked to a taxpayer issuer.

### List View
- Columns: RFC, name, tax regime, PAC provider, active certificate

### Taxpayer Issuer Form (`taxpayer_issuer`)

| Field | Column | Notes |
|-------|--------|-------|
| RFC | `taxpayer_issuer_id` | 12–13 chars, PK |
| Legal Name | `name` | |
| Tax Regime | `regime` | FK → `sat_tax_regime` |
| PAC Provider | `provider` | Enum of configured PAC integrations |
| SAT Postal Code | `postal_code` | FK → `sat_postal_code` |
| Notes | `comment` | |

### Certificate Sub-Panel (`taxpayer_certificate`)

| Field | Column | Notes |
|-------|--------|-------|
| Certificate Number | `taxpayer_certificate_id` | 20-char SAT cert number |
| Certificate File | `certificate_data` | Upload .cer file |
| Key File | `key_data` | Upload .key file |
| Key Password | `key_password` | Encrypted passphrase |
| Valid From | `valid_from` | |
| Valid To | `valid_to` | |
| Active | `active` | Only one active cert per RFC |

### Batch/Series Sub-Panel (`taxpayer_batch`)

| Field | Column | Notes |
|-------|--------|-------|
| Series | `batch` | Folio prefix (e.g. "A", "FAC") |
| Document Type | `type` | CFDI type enum |
| XML Template | `template` | CFDI XML configuration template |

### Business Rules
- Only one certificate should be `active=1` per RFC at a time.
- Certificates must be renewed before `valid_to` date; alert 30 days prior.
- The certificate's SAT-issued `taxpayer_certificate_id` must match the RFC.

---

## 2. Fiscal Documents

**Route**: `GET /fiscal-documents`  
**SystemObject**: `FiscalDocuments`

### Purpose
View, generate, stamp, download, and cancel CFDI electronic invoices. Invoices are generated from completed `sales_order` records.

### List View
- Filter by: store, issuer RFC, date range, type, status (draft/stamped/cancelled), customer
- Columns: folio (batch+serial), UUID, date, customer, recipient RFC, type, total, status

### Header Fields (`fiscal_document`)

| Field | Column | Notes |
|-------|--------|-------|
| Store | `store` | FK → `store` |
| Issuer | `issuer` | FK → `taxpayer_issuer` — auto from store |
| Issuer Regime | `issuer_regime` | Auto from taxpayer |
| Customer | `customer` | FK → `customer` |
| Recipient RFC | `recipient` | FK → `taxpayer_recipient` |
| Recipient Name | `recipient_name` | Auto-filled |
| Recipient Address | `recipient_address` | FK → `address` |
| Document Type | `type` | I=Ingreso (Income), E=Egreso, P=Pago, T=Traslado |
| Folio Series | `batch` | From `taxpayer_batch` |
| Serial | `serial` | Auto-incremented |
| Issued Location | `issued_location` | Postal code of issuance |
| Payment Method | `payment_method` | SAT code: PUE (single) or PPD (installment) |
| Payment Terms | `payment_terms` | Matches sales order |
| Payment Reference | `payment_reference` | |
| CFDI Usage | `usage` | FK → `sat_cfdi_usage` (G01, G03, P01, etc.) |
| Currency | `currency` | |
| Exchange Rate | `exchange_rate` | |
| VAT Retention Rate | `retention_rate` | ISR retention |
| Local Tax Name | `local_retention_name` | Municipal/local tax |
| Local Tax Rate | `local_retention_rate` | |
| Taxpayer Regime | `taxpayer_regime` | Recipient's regime (CFDI 4.0 required) |
| Taxpayer Postal Code | `taxpayer_postal_code` | Recipient's postal code (CFDI 4.0) |
| Notes | `comment` | |

### Line Item Fields (`fiscal_document_detail`)

| Field | Column | Notes |
|-------|--------|-------|
| Product | `product` | FK → `product` |
| Source Order Line | `order_detail` | FK → `sales_order_detail` |
| SAT Product Key | `product_service` | FK → `sat_product_service` |
| Internal Code | `product_code` | |
| Description | `product_name` | |
| Unit of Measurement | `unit_of_measurement` | FK → `sat_unit_of_measurement` |
| Quantity | `quantity` | |
| Unit Price (pre-tax) | `price` | |
| Discount | `discount` | |
| Tax Rate | `tax_rate` | |
| Currency | `currency` | |
| Exchange Rate | `exchange_rate` | |
| Tax Included | `tax_included` | |
| Notes | `comment` | |

### Payment Complement Sub-Panel (PPD invoices — `fiscal_document_relation`)

| Field | Column | Notes |
|-------|--------|-------|
| Related Invoice | `relation` | FK → `fiscal_document` |
| Installment Number | `installment` | Payment number |
| Previous Balance | `previous_balance` | Balance before this payment |
| Amount Applied | `amount` | |
| Exchange Rate | `exchange_rate` | |
| Relation Type | `type` | SAT relation type code |

### Actions
- **Generate from Sales Order**: auto-populate all fields from a `sales_order`
- **Stamp (Timbrar)**: submit to PAC for digital signature and UUID assignment
  - On success: sets `completed=true`, `stamped`, `stamp_uuid`, `authority_digital_seal`, `authority_certificate_number`, `rfc_pac`
  - Saves XML to `fiscal_document_xml`
- **Download XML**: retrieve stamped CFDI XML
- **Download PDF**: generate PDF representation of the CFDI
- **Send by Email**: email XML + PDF to recipient's email
- **Cancel**: submit cancellation to SAT
  - Requires: `cancellation_reason` (FK → `sat_reason_cancellation`)
  - If substitution: `cancellation_substitution` = UUID of replacement document
  - On success: sets `cancelled=true`, `cancellation_date`

### Business Rules
- CFDI 4.0 requires both `taxpayer_regime` and `taxpayer_postal_code` of the recipient.
- A document cannot be edited once stamped (`completed=true`).
- Cancellation within 24 hours of issuance can be done without a substitution document.
- Cancellation after 24 hours requires customer acceptance (SAT workflow).
- PPD invoices (payment in installments) require a Payment Complement (`type=P`) for each payment received.
- `version` must be `4.0` for all new documents.
- Only certificates where `active=1` and `valid_to > NOW()` can be used for stamping.
