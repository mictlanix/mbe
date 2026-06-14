# MBE Constants Reference

All enums defined in `Model/Constants/`. These are stored as integers in the database; the name column is the C# enum member name.

---

## AccessRight `[Flags]`

**File**: `Model/Constants/AccessRight.cs`  
**DB column**: `access_privilege.value` (bitmask)

Controls CRUD access for a user on a `SystemObjects` entry. Values are combined with bitwise OR.

| Value | Name | Meaning |
|-------|------|---------|
| 0 | None | No access |
| 1 | Create | Can create records |
| 2 | Read | Can view records |
| 4 | Update | Can edit records |
| 8 | Delete | Can delete records |

**Common combinations**: Read-only = `2`; Full access = `15` (1+2+4+8)

---

## AddressType

**File**: `Model/Constants/AddressType.cs`  
**DB column**: `address.address_type`

| Value | Name | Meaning |
|-------|------|---------|
| 0 | Other | Unclassified address |
| 1 | Home | Residential |
| 2 | Work | Work location |
| 3 | Business | Business address |
| 4 | Fiscal | Legal/fiscal registered address (used for CFDI) |

---

## CashCountType

**File**: `Model/Constants/CashCountType.cs`  
**DB column**: `cash_count.type`

| Value | Name | Meaning |
|-------|------|---------|
| 0 | StartingCash | Opening cash declared when session opens |
| 1 | CountedCash | Denomination count entered when session closes |

---

## CurrencyCode

**File**: `Model/Constants/CurrencyCode.cs`  
**DB column**: `sales_order.currency`, `fiscal_document.currency`, etc.

| Value | Name | Description |
|-------|------|-------------|
| 0 | MXN | Mexican Peso (base currency) |
| 1 | USD | US Dollar |
| 2 | EUR | Euro |

> MXN is always the `BaseCurrency` configured in `WebConfig`.

---

## DebitCreditEnum

**File**: `Model/Constants/DebitCreditEnum.cs`  
**DB column**: `bank_account_transaction.type`

| Value | Name | Meaning |
|-------|------|---------|
| 0 | Debit | Money out (debit from account) |
| 1 | Credit | Money in (credit to account) |

---

## DeliveryMode

**File**: `Model/Constants/DeliveryMode.cs`  
**DB column**: `sales_order.delivery_mode`, `delivery_order.mode`

| Value | Name | Meaning |
|-------|------|---------|
| 0 | ToBeDefined | Mode not yet set |
| 1 | PickUp | Customer picks up at store |
| 2 | PartialDeliveries | Fulfillment in multiple partial shipments |

---

## FiscalCertificationProvider

**File**: `Model/Constants/FiscalCertificationProvider.cs`  
**DB column**: `taxpayer_issuer.provider`

PAC (Proveedor Autorizado de Certificación) integrations for CFDI stamping.

| Value | Name | Description |
|-------|------|-------------|
| 0 | None | No PAC configured |
| 1 | Diverza | Diverza PAC |
| 2 | FiscoClic | FiscoClic PAC |
| 3 | Servisim | Servisim PAC |
| 4 | ProFact | ProFact PAC |

---

## FiscalDocumentType

**File**: `Model/Constants/FiscalDocumentType.cs`  
**DB column**: `fiscal_document.type`, `taxpayer_batch.type`

CFDI document types as defined by SAT. The numeric value maps to the `type` column; SAT uses letter codes (`I`, `E`, `P`, `T`) derived from context.

| Value | Name | SAT Code | Description |
|-------|------|----------|-------------|
| 0 | Invoice | I | Standard sales invoice (Ingreso) |
| 1 | FeeReceipt | I | Professional fees receipt |
| 2 | RentReceipt | I | Rent receipt |
| 3 | DebitNote | E | Debit note (Egreso) |
| 100 | CreditNote | E | Credit note (Egreso) — view: `ViewOutcome` |
| 101 | AdvancePaymentsApplied | E | Advance payment settlement — view: `ViewOutcome` |
| 200 | PaymentReceipt | P | Payment complement (Complemento de pago PPD) — view: `ViewPayment` |

> Gap at 0–3 = Ingreso/Egreso types; 100+ = special complement types added later.  
> `TaxpayerBatch` must exist for each `type` before a document of that type can be created.

---

## FiscalScheme

**File**: `Model/Constants/FiscalScheme.cs`

Legacy fiscal scheme identifier. CFDI is the current standard; CFD is obsolete.

| Value | Name | Description |
|-------|------|-------------|
| 0 | CFD | Comprobante Fiscal Digital (obsolete pre-2014 format) |
| 1 | CFDI | Comprobante Fiscal Digital por Internet (current SAT standard) |

---

## GenderEnum

**File**: `Model/Constants/GenderEnum.cs`  
**DB column**: `employee.gender`, `customer.gender`

| Value | Name |
|-------|------|
| 0 | Female |
| 1 | Male |

---

## PaymentMethod

**File**: `Model/Constants/PaymentMethod.cs`  
**DB column**: `customer_payment.method`, `fiscal_document.payment_method`

SAT-aligned payment method codes (SAT forma de pago catalog). Used on CFDI and customer payments.

| Value | Name | SAT Code | Description |
|-------|------|----------|-------------|
| 0 | NA | — | Not applicable / unspecified |
| 1 | Cash | 01 | Cash (Efectivo) |
| 2 | Check | 02 | Cheque nominativo |
| 3 | EFT | 03 | Electronic funds transfer (SPEI/SPID) |
| 4 | CreditCard | 04 | Credit card |
| 5 | ElectronicPurse | 05 | Electronic purse |
| 6 | ElectronicMoney | 06 | Electronic money |
| 8 | FoodVouchers | 08 | Food vouchers (vales de despensa) |
| 12 | Giving | 12 | Goods in kind (dación en pago) |
| 27 | ToTheSatisfactionOfTheCreditor | 27 | To creditor's satisfaction |
| 28 | DebitCard | 28 | Debit card |
| 29 | ServiceCard | 29 | Service card |
| 30 | AdvancePayments | 30 | Advance payments applied |
| 99 | ToBeDefined | 99 | To be defined (PPD invoices) |
| 1001 | GovernmentFunding | — | Government funding (non-SAT extension) |

---

## PaymentTerms

**File**: `Model/Constants/PaymentTerms.cs`  
**DB column**: `sales_order.payment_terms`

| Value | Name | Description |
|-------|------|-------------|
| 0 | Immediate | Cash-on-delivery / POS sale; requires open cash session |
| 1 | NetD | Credit sale; customer must have `HasCredit = true` |

> `NetD` orders are blocked for the `DefaultCustomer`. POS `Confirm` redirects to `PayOrder` when `Immediate`.

---

## PaymentType

**File**: `Model/Constants/PaymentType.cs`  
**DB column**: `customer_payment.type`

Classifies what a `customer_payment` record represents.

| Value | Name | Description |
|-------|------|-------------|
| 0 | NA | Unclassified |
| 1 | Immediate | Direct cash payment at time of sale |
| 2 | CreditPayment | Payment against a credit (NetD) order |
| 3 | PaymentInAdvance | Advance payment deposited before delivery |
| 4 | CreditNote | Payment offset via credit note |
| 5 | Expense | Internal expense record |

---

## PriceType

**File**: `Model/Constants/PriceType.cs`  
**DB column**: `price_list.type`

| Value | Name | Description |
|-------|------|-------------|
| 0 | Fixed | Fixed price list — exact amounts per product |
| 1 | Variable | Variable / percentage-based price list |

---

## Priority

**File**: `Model/Constants/PriorityEnum.cs`  
**DB column**: `purchase_request.priority`, `tech_service_request.priority`

| Value | Name |
|-------|------|
| 0 | Low |
| 1 | Normal |
| 2 | High |
| 3 | Critical |

---

## SourceType

**File**: `Model/Constants/SourceType.cs`  
**DB column**: `incidence_log.source_type`

Polymorphic discriminator on the `incidence_log` audit table. Identifies which entity type a log entry references.

| Value | Name | Referenced Entity |
|-------|------|-------------------|
| 1 | DeliveryOrder | `delivery_order` |
| 2 | CustomerPayment | `customer_payment` |
| 3 | SalesOrder | `sales_order` |
| 4 | PurchaseRequest | `purchase_request` |
| 5 | PurchaseOrder | `purchase_order` |
| 6 | Pricing | `price_list` / pricing change |
| 7 | Customer | `customer` |
| 8 | UserSettings | `user_settings` |
| 9 | Product | `product` |

---

## SupplierPaymentMethod

**File**: `Model/Constants/SupplierPaymentMethod.cs`  
**DB column**: `supplier.payment_method`, `supplier_payment.method`

| Value | Name | Description |
|-------|------|-------------|
| 1 | Cash | Cash payment |
| 2 | CreditCard | Credit card |
| 3 | DebitCard | Debit card |
| 4 | Check | Check / cheque |
| 5 | WireTransfer | Wire transfer (SPEI) |

> Note: starts at 1 (no 0/NA value).

---

## TechnicalServiceRequestType

**File**: `Model/Constants/TechnicalServiceRequestType.cs`  
**DB column**: `tech_service_request.type`

| Value | Name | Description |
|-------|------|-------------|
| 0 | Demonstration | Product demo for a customer |
| 1 | Installation | On-site or in-store installation |
| 2 | Loan | Equipment loan to customer |

---

## TransactionType

**File**: `Model/Constants/TransactionType.cs`  
**DB column**: `lot_serial_tracking.transaction_type`, `incidence_log.transaction_type`

Used by `InventoryHelpers.ChangeNotification` to classify every inventory ledger entry.

| Value | Name | Description |
|-------|------|-------------|
| 1 | SalesOrder | Stock out from a confirmed sales order |
| 2 | CustomerRefund | Stock return from a customer refund |
| 3 | InventoryIssue | Manual inventory issue |
| 4 | InventoryReceipt | Inventory received (from purchase or manual receipt) |
| 5 | InventoryTransfer | Transfer between warehouses |
| 6 | PurchaseOrder | Receipt associated with a purchase order |
| 7 | SupplierReturn | Stock returned to supplier |
| 8 | InventoryAdjustment | Physical count correction |
| 9 | ProductConversion | Product conversion / transformation |

---

## UserSettingsMode

**File**: `Model/Constants/UserSettingsMode.cs`  
**DB column**: `WebConfig.UserSettingsMode` (application config)

Controls how each user's store/POS/drawer context is set.

| Value | Name | Description |
|-------|------|-------------|
| 0 | SelfService | User selects their own store, POS, and cash drawer at login |
| 1 | Managed | Administrator assigns store/POS/drawer to the user; user cannot change |

---

## SystemObjects

**File**: `Model/Constants/SystemObjects.cs`  
**DB table**: `access_privilege` — column `system_object`

Complete permission gate catalog. Every `SystemObjects` value maps to a feature or sub-feature that can be independently gated by `AccessRight` bitmask per user.

Commented-out entries (31, 70, 76, 77, 78, 104, 105) are **disabled/unused** in the codebase.

| Value | Name | Module | Description |
|-------|------|--------|-------------|
| 0 | Products | Master Data | Product catalog |
| 1 | Labels | Master Data | Product label/category catalog |
| 2 | Customers | Master Data | Customer catalog |
| 3 | Suppliers | Master Data | Supplier catalog |
| 4 | Warehouses | Master Data | Warehouse catalog |
| 5 | PriceLists | Master Data | Price list catalog |
| 6 | Employees | Master Data | Employee catalog |
| 7 | SalesOrders | Sales | Sales orders |
| 8 | CustomerPayments | Sales | Customer payment records |
| 9 | PointsOfSale | Master Data | POS terminal catalog |
| 10 | CashDrawers | Master Data | Cash drawer catalog |
| 11 | Addresses | Master Data | Address records |
| 12 | Contacts | Master Data | Contact records |
| 13 | BankAccounts | Master Data | Bank account catalog |
| 14 | SupplierAgreements | Purchases | Supplier agreements |
| 15 | InventoryReceipts | Inventory | Inventory receipt documents |
| 16 | InventoryIssues | Inventory | Inventory issue documents |
| 17 | InventoryTransfers | Inventory | Inventory transfer documents |
| 18 | AccountsReceivable | Administration | AR view |
| 19 | AccountsPayable | Administration | AP view |
| 20 | PurchasesOrders | Purchases | Purchase orders |
| 21 | SupplierPayment | Purchases | Supplier payment records |
| 22 | CustomerRefunds | Sales | Customer refund documents |
| 23 | FiscalDocuments | Fiscal | CFDI fiscal documents |
| 24 | Taxpayers | Fiscal | Taxpayer/issuer configuration |
| 25 | SupplierReturns | Purchases | Supplier return documents |
| 26 | SalesOrdersHistoric | Sales | Historical sales orders (read-only) |
| 27 | CustomerRefundsHistoric | Sales | Historical refunds (read-only) |
| 28 | SupplierReturnHistoric | Purchases | Historical supplier returns (read-only) |
| 29 | Stores | Master Data | Store catalog |
| 30 | SalesQuotes | Sales | Sales quotations |
| 32 | Kardex | Reports | Product movement history report |
| 33 | ReceivedPayments | Reports | Received payments report |
| 34 | SalesByCustomer | Reports | Sales by customer report |
| 35 | SalesBySalesPerson | Reports | Sales by salesperson report |
| 36 | SalesByProduct | Reports | Sales by product report |
| 37 | GrossProfitsByCustomer | Reports | Gross profits by customer |
| 38 | GrossProfitsBySalesPerson | Reports | Gross profits by salesperson |
| 39 | GrossProfitsByProduct | Reports | Gross profits by product |
| 40 | BestSellingProductsByCustomer | Reports | Best sellers per customer |
| 41 | BestSellingProductsBySalesPerson | Reports | Best sellers per salesperson |
| 42 | LotSerialNumbers | Inventory | Lot/serial number tracking catalog |
| 43 | ExchangeRates | Master Data | Currency exchange rate catalog |
| 44 | POS | Sales | Point of sale terminal (POS module) |
| 45 | SerialNumberKardex | Reports | Serial number movement history |
| 46 | CustomerDebtReport | Reports | Customer debt / aging report |
| 47 | SalesOrderSummaryReport | Reports | Sales order summary report |
| 48 | FiscalDocumentsReport | Reports | Fiscal documents listing report |
| 49 | SalesPersonOrdersReport | Reports | Orders by salesperson report |
| 50 | CustomerSalesOrdersReport | Reports | Orders by customer report |
| 51 | ProductSalesByCustomerReport | Reports | Product sales per customer |
| 52 | ProductSalesByModelReport | Reports | Product sales by model |
| 53 | ProductSalesByBrandReport | Reports | Product sales by brand |
| 54 | TaxpayerRecipients | Fiscal | Taxpayer recipient (RFC) catalog |
| 55 | ProductSalesBySalesPerson | Reports | Product sales by salesperson |
| 56 | StandaloneFiscalDocuments | Fiscal | Fiscal documents without source SO |
| 57 | ProductionOrders | Production | Production orders (**NOT IMPLEMENTED** — controller commented out) |
| 58 | TechnicalServiceReports | Technical Service | Technician service reports |
| 59 | TranslationRequests | Front Desk | Document translation requests |
| 60 | Notarizations | Front Desk | Notarization tracking |
| 61 | ProductSalesBySalesPersonAndLabel | Reports | Product sales by salesperson × label |
| 62 | ProductSalesBySalesPersonAndBrand | Reports | Product sales by salesperson × brand |
| 63 | ProductSalesBySalesPersonAndModel | Reports | Product sales by salesperson × model |
| 64 | TechnicalServiceRequests | Technical Service | Customer service requests |
| 65 | TechnicalServiceReceipts | Technical Service | Equipment intake receipts |
| 66 | CustomersReport | Reports | Customer directory export |
| 67 | WarehouseStockReport | Reports | Current stock levels report |
| 68 | WarehouseStockByLotReport | Reports | Stock by lot/expiry report |
| 69 | WarehouseStockBySerialNumberReport | Reports | Stock by serial number report |
| 71 | DeliveryOrders | Logistics | Delivery order documents |
| 72 | SalesPersonOrdersAndRefundsReport | Reports | Net sales after refunds per salesperson |
| 73 | ProductsMerge | Master Data | Merge duplicate products (destructive) |
| 74 | PhysicalCountAdjustment | Inventory | Physical count / stock adjustment |
| 75 | ProductsBySupplierReport | Reports | Products by supplier report |
| 79 | ProductsOrdersAndRefundsBySalesPerson | Reports | Units sold/refunded by salesperson |
| 80 | PendantDeliveries | Logistics | Pending deliveries queue |
| 81 | Expenses | Administration | Expense category catalog |
| 82 | ExpenseTicket | Purchases | Expense voucher entry |
| 83 | CreditPayments | Sales | Credit payment processing |
| 84 | PaymentMethodOptions | Sales | Payment method configuration |
| 85 | PaymentReceipt | Sales | Payment receipt printing |
| 86 | PurchaseRequest | Purchases | Purchase request documents |
| 87 | DeliveryItineraries | Logistics | Delivery itinerary / route planning |
| 88 | Vehicle | Logistics | Fleet vehicle catalog |
| 89 | VehicleOperators | Logistics | Vehicle operator catalog |
| 90 | VehicleServiceOrders | Technical Service | Fleet vehicle service/maintenance orders |
| 91 | ForDeliver | Logistics | "Ready to deliver" queue |
| 92 | Users | Users | User account management |
| 93 | InventoryAdjustments | Inventory | Inventory adjustment documents |
| 94 | DeliveryOrderApproval | Logistics | Delivery order approval queue |
| 95 | PurchaseOrderApproval | Purchases | Purchase order approval queue |
| 96 | PurchaseRequestApproval | Purchases | Purchase request approval queue |
| 97 | ReceivedPaymentsAdvancedSearchFilter | Reports | Advanced filter on received payments report |
| 98 | CreditCustomerConfiguration | Administration | Customer credit limit/terms configuration |
| 99 | StoreMovementsSummary | Reports | Store movements summary report |
| 100 | PaymentsEditor | Sales | Modify/edit posted payments |
| 101 | SearchCreditsFromAllStores | Administration | View AR across all stores (not just own store) |
| 102 | ExcludePriceRangeValidation | Sales | Bypass price range validation on SO lines |
| 103 | IssuedLocationId | Fiscal | Set issued-location on fiscal documents |
| 106 | Pricing | Master Data | Price management / pricing tool |
| 107 | ProductionSites | Production | Production site catalog |
| 108 | PaymentsVerification | Sales | Validate/verify received payments |
| 109 | ReceivedPaymentsSummary | Reports | Received payments summary report |
| 110 | CustomerRefundConfirm | Sales | Confirm customer refund (separate gate) |
| 111 | CashSessionClose | Administration | Close a cash session |
| 112 | CommissionsBySalesPerson | Reports | Commissions by salesperson report |
| 113 | DownloadCSVFiles | Reports | Bulk CSV data export |
