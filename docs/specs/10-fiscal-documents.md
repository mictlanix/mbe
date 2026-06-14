# Fiscal Documents Specs

Covers Mexican CFDI 4.0 e-invoicing: taxpayer (issuer) configuration and fiscal document generation, stamping, and cancellation.

---

## Background: CFDI 4.0

CFDI (Comprobante Fiscal Digital por Internet) is Mexico's mandatory electronic invoicing standard governed by the SAT (Servicio de Administración Tributaria). Every issued invoice must be:

1. Generated from a registered RFC (taxpayer issuer)
2. Digitally signed with a CSD certificate
3. Stamped (timbrado) by an authorized PAC (Proveedor Autorizado de Certificación)
4. UUID-assigned by SAT via the `TimbreFiscalDigital` complement

Key SAT catalog tables: `sat_cfdi_usage`, `sat_tax_regime`, `sat_product_service`, `sat_unit_of_measurement`, `sat_currency`, `sat_reason_cancellation`, `sat_postal_code`.

---

## 1. Taxpayers (Issuers)

**Route**: `GET /taxpayers`  
**Controller**: `TaxpayersController`  
**SystemObject**: `Taxpayers` (24)

### Purpose
Configure the company's RFC(s) used to issue CFDI documents. Each store is linked to a taxpayer issuer.

### List View
- Columns: RFC, name, tax regime, PAC provider, active certificate status

### Taxpayer Issuer Form (`taxpayer_issuer`)

| Field | Column | Notes |
|-------|--------|-------|
| RFC | `taxpayer_issuer_id` | 12–13 chars, PK |
| Legal Name | `name` | |
| Tax Regime | `regime` | FK → `sat_tax_regime` |
| PAC Provider | `provider` | Enum of configured PAC integrations (e.g. Finkok, Edicom) |
| SAT Postal Code | `postal_code` | FK → `sat_postal_code` |
| Notes | `comment` | |

### Certificate Sub-Panel (`taxpayer_certificate`)

| Field | Column | Notes |
|-------|--------|-------|
| Certificate Number | `taxpayer_certificate_id` | 20-char SAT cert number, PK |
| Certificate File | `certificate_data` | Upload .cer file (DER-encoded X.509) |
| Key File | `key_data` | Upload .key file (private key) |
| Key Password | `key_password` | Encrypted passphrase |
| Valid From | `valid_from` | Certificate validity start |
| Valid To | `valid_to` | Certificate validity end |
| Active | `active` | Only one active cert per RFC |

### Batch/Series Sub-Panel (`taxpayer_batch`)

| Field | Column | Notes |
|-------|--------|-------|
| Series | `batch` | Folio prefix (e.g. "A", "FAC", "E") |
| Document Type | `type` | CFDI type: `I`=Ingreso, `E`=Egreso, `P`=Pago, `T`=Traslado, `CreditNote`, `PaymentReceipt`, `AdvancePaymentsApplied` |
| XML Template | `template` | JSON blob describing print template name, logo, header/footer heights, extra info |

### Business Rules
- Only one certificate should be `active = 1` per RFC at a time.
- If no `TaxpayerBatch` exists for the issuer + document type, creating a fiscal document of that type fails.
- Certificates must be renewed before `valid_to`; alert 30 days prior.

---

## 2. Fiscal Documents

**Route**: `GET /fiscal-documents`  
**Controller**: `FiscalDocumentsController`  
**SystemObject**: `FiscalDocuments` (23)

### Purpose
View, generate, stamp, download, and cancel CFDI electronic invoices. Documents are generated from sales orders or standalone.

### List View
- Search numeric: matches document ID, serial, or linked sales order ID
- Search text: matches issuer RFC, recipient RFC, recipient name, customer name
- Filter: excludes records with `!IsCompleted && IsCancelled` (draft-cancelled)
- Sort: open documents first (non-completed, non-cancelled), then by `Issued` date descending

### Create Flow
1. Select document type (`FiscalDocumentType`: `I`=Ingreso, `E`=Egreso, `P`=Pago, etc.)
2. Select issuer (from store's taxpayer): system finds matching `TaxpayerBatch` for the type; error if none found
3. Select customer and recipient RFC (`TaxpayerRecipient`): auto-fills `TaxpayerRegime`, `TaxpayerPostalCode`
   - If recipient is the general public RFC (`TaxpayerGeneralReceiptId`): `TaxpayerPostalCode` = store's location postal code
4. Document is created with `Batch` from the matched `TaxpayerBatch`

### Header Fields (`fiscal_document`)

| Field | Column | Notes |
|-------|--------|-------|
| Store | `store` | FK → `store` |
| Issuer | `issuer` | FK → `taxpayer_issuer` — auto from store |
| Issuer Regime | `issuer_regime` | Auto from taxpayer |
| Customer | `customer` | FK → `customer` |
| Recipient RFC | `recipient` | FK → `taxpayer_recipient` |
| Recipient Name | `recipient_name` | Auto-filled from recipient |
| Recipient Address | `recipient_address` | FK → `address` |
| Taxpayer Regime | `taxpayer_regime` | Recipient's SAT tax regime (CFDI 4.0 required) |
| Taxpayer Postal Code | `taxpayer_postal_code` | Recipient's postal code (CFDI 4.0 required) |
| Document Type | `type` | `I`=Ingreso, `E`=Egreso, `P`=Pago, `T`=Traslado |
| Folio Series | `batch` | From `taxpayer_batch` |
| Serial | `serial` | Auto-incremented |
| Version | `version` | CFDI version (e.g. `4.0`) |
| Issued Date | `issued` | Stamping timestamp |
| Issued At | `issued_at` | FK → `address` — store's address |
| Payment Method | `payment_method` | SAT code: `PUE` (single) or `PPD` (installments) |
| Payment Terms | `payment_terms` | Matches sales order |
| Payment Reference | `payment_reference` | |
| CFDI Usage | `usage` | FK → `sat_cfdi_usage` (G01, G03, P01, etc.) |
| Currency | `currency` | |
| Exchange Rate | `exchange_rate` | |
| VAT Retention Rate | `retention_rate` | ISR retention rate |
| Local Tax Name | `local_retention_name` | Municipal/local tax label |
| Local Tax Rate | `local_retention_rate` | |
| Notes | `comment` | |
| Stamp UUID | `stamp_id` | SAT-assigned UUID after timbrado |
| Stamped Date | `stamped` | Timestamp from PAC response |
| Authority Digital Seal | `authority_digital_seal` | PAC certificate seal |
| Authority Certificate | `authority_certificate_number` | PAC certificate number |
| RFC PAC | `rfc_pac` | PAC's RFC |
| Cancellation Reason | `cancellation_reason` | FK → `sat_reason_cancellation` |
| Cancellation Date | `cancellation_date` | |
| Cancellation Substitution | `cancellation_substitution` | UUID of replacement document |

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

### Payment Complement Sub-Panel (`fiscal_document_relation`, for PPD invoices)

| Field | Column | Notes |
|-------|--------|-------|
| Related Invoice | `relation` | FK → `fiscal_document` |
| Installment Number | `installment` | Payment sequence number |
| Previous Balance | `previous_balance` | Balance before this payment |
| Amount Applied | `amount` | |
| Exchange Rate | `exchange_rate` | |
| Relation Type | `type` | SAT relation type code |

### Confirm Action (Stamp / Timbrar)
1. Builds CFDI XML using CFDv40 helper
2. Submits to PAC via `CFDHelpers40.StampCFD`
3. Extracts `TimbreFiscalDigital` complement from PAC response:
   - `entity.StampId = tfd.UUID`
   - `entity.Stamped = tfd.FechaTimbrado`
4. Saves XML to `FiscalDocumentXml` table
5. Sets `IsCompleted = true`

### Print Template Selection
Template is derived from batch configuration:
```
view = string.Format("Print{0:00}{1}", model.Version * 10, template.Name)
// e.g. for version 4.0 + template "Standard" → "Print40Standard"
```

### Cancel Action
1. Sets `CancellationDate = now`, `IsCancelled = true`
2. If `CancellationReason != null`: calls `CFDHelpers40.CancelCFD` (PAC API)
3. `CancelNoReason` skips the reason check for certain cancellation scenarios

### Actions
- **New** (from scratch): select type → create blank document
- **Generate from Sales Order**: populate all header and line fields from a `sales_order`; lines linked to `sales_order_detail` via `order_detail` FK
- **Stamp (Timbrar)** (`Confirm`): submit to PAC for UUID + digital seal
- **Download XML** (`Download`): streams `FiscalDocumentXml.Data` as `text/xml`
- **Download PDF** (`Pdf`): generates PDF using configured template
- **QR Code** (`QRCode`): generates QR code with: `issuer.RFC|recipient.RFC|total|UUID|...`
- **Send by Email** (`SendEmail`): sends XML + PDF as MIME attachments; uses `WebConfig.DefaultSender` and optional CC
- **Cancel**: PAC cancellation with reason; or `CancelNoReason`

### Document Type Views
Different view templates per document type:
- `FiscalDocumentType.PaymentReceipt` → `ViewPayment`
- `FiscalDocumentType.CreditNote` or `AdvancePaymentsApplied` → `ViewOutcome`
- All others → default `View`

### Standalone Fiscal Documents

**SystemObject**: `StandaloneFiscalDocuments` (56)

Fiscal documents not generated from a sales order (e.g., service invoices, advance payment receipts). Use the same `FiscalDocument` model and workflow; the distinction is access control — this gate allows creating documents without a source SO.

### Taxpayer Recipients

**Route**: `GET /taxpayer-recipients`  
**Controller**: `TaxpayerRecipientsController`  
**SystemObject**: `TaxpayerRecipients` (54)

Catalog of customer RFC records used as CFDI recipients. See Master Data spec for full field details.

### Business Rules
- CFDI 4.0 mandates both `taxpayer_regime` and `taxpayer_postal_code` for the recipient.
- A document cannot be edited once `IsCompleted = true`.
- Cancellation within 24 hours of issuance can be done without a substitution document.
- Cancellation after 24 hours requires customer acceptance (SAT 2-step workflow).
- PPD invoices (payment in installments) require a Payment Complement (`type = P`) for each payment received.
- Only certificates with `active = 1` and `valid_to > now()` can be used for stamping.
- `version = "4.0"` for all new documents.
