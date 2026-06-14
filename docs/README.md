# MBE — Migration Specs

This directory contains the reverse-engineered specifications for the Mictlanix Business Engine (MBE) system, produced as input for a migration to **Flutter (UI) + Python (API backend)**.

## Structure

| File | Contents |
|------|----------|
| [data-dictionary.md](data-dictionary.md) | All database tables with column-level descriptions |
| [constants.md](constants.md) | All enum constants from `Model/Constants/` with integer values |
| [specs/01-master-data.md](specs/01-master-data.md) | Products, Customers, Suppliers, Warehouses, etc. |
| [specs/02-sales.md](specs/02-sales.md) | Quotes, POS, Sales Orders, Payments, Refunds |
| [specs/03-production.md](specs/03-production.md) | Production Orders |
| [specs/04-inventory.md](specs/04-inventory.md) | Receipts, Issues, Transfers, Lot/Serial tracking |
| [specs/05-purchases.md](specs/05-purchases.md) | Purchase Requests, Purchase Orders, Supplier Payments |
| [specs/06-logistics.md](specs/06-logistics.md) | Delivery Itineraries and Delivery Orders |
| [specs/07-administration.md](specs/07-administration.md) | Accounts Receivable, Accounts Payable |
| [specs/08-technical-service.md](specs/08-technical-service.md) | Tech Service Receipts, Reports, Requests, Vehicle Service |
| [specs/09-front-desk.md](specs/09-front-desk.md) | Translation Requests, Notarizations |
| [specs/10-fiscal-documents.md](specs/10-fiscal-documents.md) | Taxpayers, CFDI Fiscal Documents |
| [specs/11-reports.md](specs/11-reports.md) | All report screens |
| [specs/12-users.md](specs/12-users.md) | User management and access control |

## Technology Stack (Target)

- **UI**: Flutter (cross-platform: Android, iOS, Web, Desktop)
- **API**: Python (FastAPI recommended)
- **Database**: MariaDB/MySQL (same schema, migration optional to PostgreSQL)

## Key Domain Concepts

- **Store**: Top-level org unit. All transactional data belongs to a store.
- **Warehouse**: Physical stock location, belongs to a store.
- **Point of Sale**: Terminal configuration (store + warehouse), used for POS transactions.
- **Cash Drawer**: Hardware unit tied to a store; sessions opened per cashier shift.
- **Employee**: Internal staff. Sales persons, cashiers, creators/updaters of documents.
- **Customer**: Buyer entity with credit terms and assigned price list.
- **Supplier**: Vendor entity with credit terms.
- **Product**: Catalog item with pricing, tax, stock, and lot/serial tracking flags.
- **SAT tables**: Mexican tax authority (SAT) catalogs for CFDI e-invoicing compliance.
