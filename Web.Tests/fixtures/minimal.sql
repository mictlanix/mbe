-- Smallest set of parent rows the controller tests need.
--
-- Web.Tests never creates stores, customers, employees or points of sale -- it
-- reads whichever ones the target database already has. Against a developer
-- database that is fine. Against an empty schema loaded from
-- Schema/model/mbe_schema.sql there is nothing to read, so load this first.
--
--   mysql -u developer mbe_test < Schema/model/mbe_schema.sql
--   mysql -u developer mbe_test < Web.Tests/fixtures/minimal.sql

INSERT INTO `sat_postal_code` (`sat_postal_code_id`, `state`, `borough`, `locality`)
	VALUES ('00000', 'MEX', NULL, NULL);

INSERT INTO `sat_tax_regime` (`sat_tax_regime_id`, `description`)
	VALUES ('601', 'General de Ley Personas Morales');

INSERT INTO `taxpayer_issuer` (`taxpayer_issuer_id`, `regime`, `provider`, `postal_code`)
	VALUES ('XAXX010101000', '601', 0, '00000');

INSERT INTO `address` (`address_id`, `type`, `street`, `exterior_number`, `postal_code`,
	`neighborhood`, `borough`, `state`, `country`)
	VALUES (1, 0, 'Test Street', '1', '00000', 'Centro', 'Centro', 'Test State', 'MEX');

INSERT INTO `store` (`store_id`, `code`, `name`, `location`, `address`, `taxpayer`, `logo`)
	VALUES (1, 'TEST', 'Test Store', '00000', 1, 'XAXX010101000', 'logo.png');

INSERT INTO `warehouse` (`warehouse_id`, `store`, `code`, `name`)
	VALUES (1, 1, 'TEST-WH', 'Test Warehouse');

INSERT INTO `point_sale` (`point_sale_id`, `store`, `code`, `name`, `warehouse`)
	VALUES (1, 1, 'TEST-POS', 'Test Point of Sale', 1);

INSERT INTO `price_list` (`price_list_id`, `name`, `high_profit_margin`, `low_profit_margin`)
	VALUES (1, 'Test Price List', 0.3000, 0.1000);

INSERT INTO `customer` (`customer_id`, `code`, `name`, `credit_limit`, `credit_days`, `price_list`)
	VALUES (1, 'TEST-CUST', 'Test Customer', 100000.0000, 30, 1);

INSERT INTO `employee` (`employee_id`, `first_name`, `last_name`, `nickname`, `gender`,
	`birthday`, `sales_person`, `active`, `start_job_date`)
	VALUES (1, 'Test', 'Employee', 'tester', 0, '1990-01-01', 1, 1, '2020-01-01');
