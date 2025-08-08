ALTER TABLE product ADD COLUMN stock_verification TINYINT(1) NOT NULL DEFAULT '0';

CREATE TABLE IF NOT EXISTS `vehicle` (
	`vehicle_id` INT(11) NOT NULL AUTO_INCREMENT,
	`license_plate` VARCHAR(8) NOT NULL DEFAULT '' COLLATE 'utf8_unicode_ci',
	`name` VARCHAR(50) NOT NULL DEFAULT '' COLLATE 'utf8_unicode_ci',
	`nickname` VARCHAR(30) NOT NULL DEFAULT '' COLLATE 'utf8_unicode_ci',
	`tons_capacity` TINYINT(4) NOT NULL DEFAULT '0',
	`active` TINYINT(1) NOT NULL DEFAULT '1',
	PRIMARY KEY (`vehicle_id`) USING BTREE,
	UNIQUE INDEX `license_plate` (`license_plate`) USING BTREE
);

CREATE TABLE IF NOT EXISTS `vehicle_operator` (
	`vehicle_operator_id` INT(11) NOT NULL AUTO_INCREMENT,
	`driver` INT(11) NOT NULL DEFAULT '0',
	`license_type` VARCHAR(3) NOT NULL DEFAULT '0' COLLATE 'utf8_unicode_ci',
	`driver_license_number` VARCHAR(15) NOT NULL DEFAULT '0' COLLATE 'utf8_unicode_ci',
	`issue_date` DATE NOT NULL,
	`expiration_date` DATE NOT NULL,
	`issuing_location` VARCHAR(30) NOT NULL DEFAULT '0' COLLATE 'utf8_unicode_ci',
	`creation_time` DATETIME NOT NULL,
	`modification_time` DATETIME NOT NULL,
	`creator` INT(11) NOT NULL DEFAULT '0',
	`updater` INT(11) NOT NULL DEFAULT '0',
	`active` TINYINT(1) NOT NULL DEFAULT '1',
	PRIMARY KEY (`vehicle_operator_id`) USING BTREE,
	INDEX `FK_vehicle_operator_employee` (`driver`) USING BTREE,
	INDEX `FK_vehicle_operator_employee_2` (`creator`) USING BTREE,
	INDEX `FK_vehicle_operator_employee_3` (`updater`) USING BTREE,
	CONSTRAINT `FK_vehicle_operator_employee` FOREIGN KEY (`driver`) REFERENCES `employee` (`employee_id`) ON UPDATE NO ACTION ON DELETE NO ACTION,
	CONSTRAINT `FK_vehicle_operator_employee_2` FOREIGN KEY (`creator`) REFERENCES `employee` (`employee_id`) ON UPDATE NO ACTION ON DELETE NO ACTION,
	CONSTRAINT `FK_vehicle_operator_employee_3` FOREIGN KEY (`updater`) REFERENCES `employee` (`employee_id`) ON UPDATE NO ACTION ON DELETE NO ACTION
)
;

CREATE TABLE IF NOT EXISTS `deliveries_itinerary` (
	`deliveries_itinerary_id` INT(11) NOT NULL AUTO_INCREMENT,
	`vehicle` INT(11) NOT NULL,
	`vehicle_operator` INT(11) NOT NULL,
	`date` DATE NOT NULL,
	`creator` INT(11) NOT NULL,
	`updater` INT(11) NOT NULL,
	`creation_time` DATETIME NOT NULL,
	`modification_time` DATETIME NOT NULL,
	`cancelled` TINYINT(1) NOT NULL DEFAULT '0',
	`completed` TINYINT(1) NOT NULL DEFAULT '0',
	`comment` VARCHAR(500) NULL DEFAULT '0' COLLATE 'utf8_unicode_ci',
	PRIMARY KEY (`deliveries_itinerary_id`) USING BTREE,
	INDEX `FK_deliveries_itinerary_vehicle` (`vehicle`) USING BTREE,
	INDEX `FK_deliveries_itinerary_vehicle_operator` (`vehicle_operator`) USING BTREE,
	INDEX `FK_deliveries_itinerary_employee` (`creator`) USING BTREE,
	INDEX `FK_deliveries_itinerary_employee_2` (`updater`) USING BTREE,
	CONSTRAINT `FK_deliveries_itinerary_employee` FOREIGN KEY (`creator`) REFERENCES `employee` (`employee_id`) ON UPDATE NO ACTION ON DELETE NO ACTION,
	CONSTRAINT `FK_deliveries_itinerary_employee_2` FOREIGN KEY (`updater`) REFERENCES `employee` (`employee_id`) ON UPDATE NO ACTION ON DELETE NO ACTION,
	CONSTRAINT `FK_deliveries_itinerary_vehicle` FOREIGN KEY (`vehicle`) REFERENCES `vehicle` (`vehicle_id`) ON UPDATE NO ACTION ON DELETE NO ACTION,
	CONSTRAINT `FK_deliveries_itinerary_vehicle_operator` FOREIGN KEY (`vehicle_operator`) REFERENCES `vehicle_operator` (`vehicle_operator_id`) ON UPDATE NO ACTION ON DELETE NO ACTION
)
;

CREATE TABLE IF NOT EXISTS `deliveries_itinerary_detail` (
	`deliveries_itinerary_detail_id` INT(11) NOT NULL AUTO_INCREMENT,
	`deliveries_itinerary` INT(11) NULL DEFAULT NULL,
	`delivery_order_detail` INT(11) NOT NULL,
	`quantity` DECIMAL(20,6) NOT NULL,
	`comment` VARCHAR(500) NULL DEFAULT NULL COLLATE 'utf8_unicode_ci',
	PRIMARY KEY (`deliveries_itinerary_detail_id`) USING BTREE,
	INDEX `FK_deliveries_itinerary_detail_delivery_order_detail` (`delivery_order_detail`) USING BTREE,
	INDEX `FK_deliveries_itinerary_detail_deliveries_itinerary` (`deliveries_itinerary`) USING BTREE,
	CONSTRAINT `FK_deliveries_itinerary_detail_deliveries_itinerary` FOREIGN KEY (`deliveries_itinerary`) REFERENCES `deliveries_itinerary` (`deliveries_itinerary_id`) ON UPDATE NO ACTION ON DELETE NO ACTION,
	CONSTRAINT `FK_deliveries_itinerary_detail_delivery_order_detail` FOREIGN KEY (`delivery_order_detail`) REFERENCES `delivery_order_detail` (`delivery_order_detail_id`) ON UPDATE NO ACTION ON DELETE NO ACTION
);



CREATE TABLE IF NOT EXISTS `vehicle_service_order` (
	`service_order_id` INT(11) NOT NULL AUTO_INCREMENT,
	`vehicle` INT(11) NOT NULL DEFAULT '0',
	`problem_description` VARCHAR(500) NOT NULL DEFAULT '0' COLLATE 'utf8_unicode_ci',
	`service_description` VARCHAR(500) NULL DEFAULT '0' COLLATE 'utf8_unicode_ci',
	`creator` INT(11) NOT NULL DEFAULT '0',
	`updater` INT(11) NOT NULL DEFAULT '0',
	`notifier` INT(11) NOT NULL DEFAULT '0',
	`creation_time` DATETIME NOT NULL,
	`modification_time` DATETIME NOT NULL,
	`completed` TINYINT(1) NOT NULL DEFAULT '0',
	`cancelled` TINYINT(1) NOT NULL DEFAULT '0',
	`comment` VARCHAR(250) NULL DEFAULT '0' COLLATE 'utf8_unicode_ci',
	`date` DATETIME NULL DEFAULT NULL,
	PRIMARY KEY (`service_order_id`) USING BTREE,
	INDEX `FK_vehicle` (`vehicle`) USING BTREE,
	INDEX `FK_vehicle_service_order_employee` (`creator`) USING BTREE,
	INDEX `FK_vehicle_service_order_employee_2` (`updater`) USING BTREE,
	INDEX `FK_vehicle_service_order_employee_3` (`notifier`) USING BTREE,
	CONSTRAINT `FK__vehicle` FOREIGN KEY (`vehicle`) REFERENCES `vehicle` (`vehicle_id`) ON UPDATE NO ACTION ON DELETE NO ACTION,
	CONSTRAINT `FK_vehicle_service_order_employee` FOREIGN KEY (`creator`) REFERENCES `employee` (`employee_id`) ON UPDATE NO ACTION ON DELETE NO ACTION,
	CONSTRAINT `FK_vehicle_service_order_employee_2` FOREIGN KEY (`updater`) REFERENCES `employee` (`employee_id`) ON UPDATE NO ACTION ON DELETE NO ACTION,
	CONSTRAINT `FK_vehicle_service_order_employee_3` FOREIGN KEY (`notifier`) REFERENCES `employee` (`employee_id`) ON UPDATE NO ACTION ON DELETE NO ACTION
);

CREATE TABLE IF NOT EXISTS `service_order_detail` (
	`service_order_detail_id` INT(11) NOT NULL AUTO_INCREMENT,
	`vehicle_service_order` INT(11) NOT NULL DEFAULT '0',
	`spare_part` INT(11) NOT NULL DEFAULT '0',
	`quantity` DECIMAL(20,6) NOT NULL DEFAULT '0.000000',
	`comment` VARCHAR(500) NULL DEFAULT '0' COLLATE 'utf8_unicode_ci',
	`date` DATETIME NOT NULL,
	PRIMARY KEY (`service_order_detail_id`) USING BTREE,
	INDEX `FK__vehicle_service_order` (`vehicle_service_order`) USING BTREE,
	INDEX `FK__product` (`spare_part`) USING BTREE,
	CONSTRAINT `FK__product` FOREIGN KEY (`spare_part`) REFERENCES `product` (`product_id`) ON UPDATE NO ACTION ON DELETE NO ACTION,
	CONSTRAINT `FK__vehicle_service_order` FOREIGN KEY (`vehicle_service_order`) REFERENCES `vehicle_service_order` (`service_order_id`) ON UPDATE NO ACTION ON DELETE NO ACTION
);

CREATE TABLE IF NOT EXISTS `purchase_request` (
	`purchase_request_id` INT(11) NOT NULL AUTO_INCREMENT,
	`creator` INT(11) NOT NULL DEFAULT '0',
	`updater` INT(11) NOT NULL DEFAULT '0',
	`warehouse` INT(11) NOT NULL DEFAULT '0',
	`comment` VARCHAR(500) NULL DEFAULT NULL COLLATE 'utf8_unicode_ci',
	`date` DATETIME NOT NULL DEFAULT current_timestamp(),
	`creation_time` DATETIME NOT NULL,
	`modification_time` DATETIME NOT NULL,
	`completed` TINYINT(1) NULL DEFAULT '0',
	`cancelled` TINYINT(1) NULL DEFAULT '0',
	PRIMARY KEY (`purchase_request_id`) USING BTREE,
	INDEX `FK_purchase_request_employee` (`creator`) USING BTREE,
	INDEX `FK_purchase_request_employee_2` (`updater`) USING BTREE,
	INDEX `FK_purchase_request_warehouse` (`warehouse`) USING BTREE,
	CONSTRAINT `FK_purchase_request_employee` FOREIGN KEY (`creator`) REFERENCES `employee` (`employee_id`) ON UPDATE NO ACTION ON DELETE NO ACTION,
	CONSTRAINT `FK_purchase_request_employee_2` FOREIGN KEY (`updater`) REFERENCES `employee` (`employee_id`) ON UPDATE NO ACTION ON DELETE NO ACTION,
	CONSTRAINT `FK_purchase_request_warehouse` FOREIGN KEY (`warehouse`) REFERENCES `warehouse` (`warehouse_id`) ON UPDATE NO ACTION ON DELETE NO ACTION
);
CREATE TABLE IF NOT EXISTS `purchase_request_detail` (
	`purchase_request_detail_id` INT(11) NOT NULL AUTO_INCREMENT,
	`purchase_request` INT(11) NOT NULL DEFAULT '0',
	`product` INT(11) NOT NULL DEFAULT '0',
	`quantity` DECIMAL(18,2) NOT NULL,
	`warehouse` INT(11) NULL DEFAULT NULL,
	`customer` INT(11) NULL DEFAULT NULL,
	PRIMARY KEY (`purchase_request_detail_id`) USING BTREE,
	INDEX `FK_purchase_request_detail_purchase_request` (`purchase_request`) USING BTREE,
	INDEX `FK_purchase_request_detail_product` (`product`) USING BTREE,
	INDEX `FK_purchase_request_detail_warehouse` (`warehouse`) USING BTREE,
	INDEX `FK_purchase_request_detail_customer` (`customer`) USING BTREE,
	CONSTRAINT `FK_purchase_request_detail_customer` FOREIGN KEY (`customer`) REFERENCES `customer` (`customer_id`) ON UPDATE NO ACTION ON DELETE NO ACTION,
	CONSTRAINT `FK_purchase_request_detail_product` FOREIGN KEY (`product`) REFERENCES `product` (`product_id`) ON UPDATE NO ACTION ON DELETE NO ACTION,
	CONSTRAINT `FK_purchase_request_detail_purchase_request` FOREIGN KEY (`purchase_request`) REFERENCES `purchase_request` (`purchase_request_id`) ON UPDATE NO ACTION ON DELETE NO ACTION,
	CONSTRAINT `FK_purchase_request_detail_warehouse` FOREIGN KEY (`warehouse`) REFERENCES `warehouse` (`warehouse_id`) ON UPDATE NO ACTION ON DELETE NO ACTION
);

ALTER TABLE purchase_order_detail ADD COLUMN purchase_request_detail INT(11) NULL DEFAULT NULL;

UPDATE product p SET p.invoiceable = 1, p.stockable = 0, p.salable = 1, p.purchasable = 0,
p.seriable = 0
WHERE p.name LIKE 'CONCRETO%';

DELETE lst FROM lot_serial_tracking lst JOIN product p ON lst.product = p.product_id
WHERE p.stockable = FALSE;

ALTER TABLE `purchase_order`
	ADD COLUMN `estimated_receipt_date` DATETIME NULL AFTER `cancelled`;
	
	ALTER TABLE `purchase_request_detail`
	ADD COLUMN `product_name` VARCHAR(250) NULL DEFAULT NULL AFTER `product`;
	
	ALTER TABLE `delivery_order`
	ADD COLUMN `confirmed` TINYINT(1) NULL DEFAULT NULL;
	
ALTER TABLE `inventory_receipt_detail`
	ADD COLUMN `purchase_order_detail` INT NULL AFTER `product`,
	ADD CONSTRAINT `FK_inventory_receipt_detail_purchase_order_detail` FOREIGN KEY (`purchase_order_detail`) REFERENCES `purchase_order_detail` (`purchase_order_detail_id`) ON UPDATE NO ACTION ON DELETE NO ACTION;

ALTER TABLE `purchase_request`
	ADD COLUMN `serial` INT NULL AFTER `comment`;

ALTER TABLE `user`
	ADD COLUMN `disabled` TINYINT(1) NOT NULL DEFAULT '0' AFTER `administrator`;

ALTER TABLE `product`
	ADD COLUMN `bar_code` CHAR(13) NULL DEFAULT NULL AFTER `model`;
	
ALTER TABLE `product`
	DROP INDEX `code_UNIQUE`,
	ADD UNIQUE INDEX `code_UNIQUE` (`code`, `bar_code`) USING BTREE;
	
ALTER TABLE `purchase_request`
	ADD COLUMN `approved` TINYINT(1) NOT NULL DEFAULT '0' AFTER `cancelled`;
	
ALTER TABLE `purchase_order`
	ADD COLUMN `approved` TINYINT(1) NOT NULL DEFAULT '0' AFTER `cancelled`;
	
ALTER TABLE `purchase_order`
	ADD COLUMN `approver` INT NULL AFTER `comment`;
ALTER TABLE `purchase_order`
	ADD CONSTRAINT `FK_purchase_order_employee` FOREIGN KEY (`approver`) REFERENCES `employee` (`employee_id`) ON UPDATE NO ACTION ON DELETE NO ACTION;
	
ALTER TABLE `address`
	ADD COLUMN `url_address` VARCHAR(200) NULL DEFAULT NULL AFTER `country`;
	
ALTER TABLE `customer_payment`
	ADD COLUMN `payment_type` TINYINT(2) NULL DEFAULT NULL AFTER `currency`;
	
ALTER TABLE `address`
	ADD COLUMN `nickname` CHAR(100) NULL DEFAULT NULL AFTER `address_id`;
	
	
CREATE TABLE `incidence` (
	`incidence_id` INT(11) NOT NULL AUTO_INCREMENT,
	`source` INT(11) NOT NULL DEFAULT '0',
	`instance_id` INT(11) NOT NULL DEFAULT '0',
	`modification_time` DATETIME NULL DEFAULT NULL,
	`updater` INT(11) NOT NULL DEFAULT '0',
	`content` VARCHAR(1000) NOT NULL DEFAULT '0' COLLATE 'armscii8_bin',
	`comment` VARCHAR(500) NULL DEFAULT NULL COLLATE 'armscii8_bin',
	PRIMARY KEY (`incidence_id`) USING BTREE
);

ALTER TABLE `sales_quote`
	CHANGE COLUMN `serial` `serial` INT(11) NULL AFTER `store`;
	
ALTER TABLE `sales_order`
	CHANGE COLUMN `serial` `serial` INT(11) NULL AFTER `store`;
	
ALTER TABLE `sales_order_detail`
	CHANGE COLUMN `discount` `discount_rate` DECIMAL(9,8) NOT NULL AFTER `price`;
	
ALTER TABLE `sales_quote_detail`
	CHANGE COLUMN `discount` `discount_rate` DECIMAL(9,8) NOT NULL AFTER `price_adjustment`;
	
ALTER TABLE `product_price`
	ADD COLUMN `low_profit` DECIMAL(20,6) NOT NULL DEFAULT '0' AFTER `price`,
	ADD COLUMN `high_profit` DECIMAL(20,6) NOT NULL DEFAULT '1' AFTER `low_profit`;
	
CREATE TABLE `production_site` (
	`production_site_id` INT(11) NOT NULL AUTO_INCREMENT,
	`store` INT(11) NOT NULL,
	`code` VARCHAR(25) NOT NULL COLLATE 'utf8mb3_unicode_ci',
	`name` VARCHAR(250) NOT NULL COLLATE 'utf8mb3_unicode_ci',
	`comment` VARCHAR(500) NULL DEFAULT NULL COLLATE 'utf8mb3_unicode_ci',
	`disabled` TINYINT(4) NULL DEFAULT '0',
	PRIMARY KEY (`production_site_id`) USING BTREE,
	UNIQUE INDEX `code_UNIQUE` (`code`) USING BTREE,
	INDEX `production_site_store_fk_idx` (`store`) USING BTREE,
	CONSTRAINT `production_site_store_fk` FOREIGN KEY (`store`) REFERENCES `store` (`store_id`) ON UPDATE NO ACTION ON DELETE NO ACTION
);

ALTER TABLE `product`
	ADD COLUMN `creation_time` DATETIME NULL AFTER `stock_verification`,
	ADD COLUMN `modification_time` DATETIME NULL AFTER `creation_time`,
	ADD COLUMN `creator` INT NULL DEFAULT NULL AFTER `modification_time`,
	ADD COLUMN `updater` INT NULL DEFAULT NULL AFTER `creator`;
	
ALTER TABLE `sales_quote_detail`
	ADD COLUMN `warehouse` INT NULL AFTER `comment`,
	ADD CONSTRAINT `FK_sales_quote_detail_warehouse` FOREIGN KEY (`warehouse`) REFERENCES `warehouse` (`warehouse_id`) ON UPDATE NO ACTION ON DELETE NO ACTION;
	
ALTER TABLE `sales_quote_detail`
	DROP COLUMN `warehouse`,
	DROP FOREIGN KEY `FK_sales_quote_detail_warehouse`;
	
ALTER TABLE `customer_refund_detail`
	ADD COLUMN `warehouse` INT NULL AFTER `tax_included`,
	ADD CONSTRAINT `FK_customer_refund_detail_warehouse` FOREIGN KEY (`warehouse`) REFERENCES `warehouse` (`warehouse_id`) ON UPDATE NO ACTION ON DELETE NO ACTION;

ALTER TABLE `purchase_request_detail`
	ADD COLUMN `to_purchase` TINYINT(1) NOT NULL DEFAULT '0' AFTER `customer`;
	
ALTER TABLE `customer_payment`
	ADD COLUMN `verifier` INT NULL AFTER `updater`,
	ADD CONSTRAINT `customer_payment_verifier_fk` FOREIGN KEY (`verifier`) REFERENCES `employee` (`employee_id`) ON UPDATE NO ACTION ON DELETE NO ACTION;
	
ALTER TABLE `customer_refund`
	CHANGE COLUMN `serial` `serial` INT(11) NULL AFTER `store`;

ALTER TABLE `incidence`
	CHANGE COLUMN `content` `content` VARCHAR(1000) NOT NULL DEFAULT '0' COLLATE 'utf8mb3_spanish2_ci' AFTER `updater`;
	
ALTER TABLE `incidence`
	CHANGE COLUMN `comment` `comment` VARCHAR(500) NULL DEFAULT NULL COLLATE 'utf8mb3_bin' AFTER `content`;
	
ALTER TABLE `incidence`
	CHANGE COLUMN `content` `content` VARCHAR(1000) NULL DEFAULT '0' COLLATE 'utf8mb3_bin' AFTER `updater`;
	
ALTER TABLE `cash_session`
	ADD COLUMN `cash_supervisor` INT NULL AFTER `cash_drawer`,
	ADD CONSTRAINT `FK_cash_session_employee` FOREIGN KEY (`cash_supervisor`) REFERENCES `employee` (`employee_id`) ON UPDATE NO ACTION ON DELETE NO ACTION;
	
ALTER TABLE `expense_voucher`
	ADD COLUMN `payment_method` TINYINT NULL DEFAULT NULL AFTER `cancelled`;
	
ALTER TABLE `customer_payment`
	CHANGE COLUMN `payment_type` `payment_type` TINYINT(2) NOT NULL AFTER `currency`;

ALTER TABLE `sales_order_payment`
	ADD COLUMN `applier` INT NULL AFTER `amount_change`,
	ADD COLUMN `date` DATETIME NULL AFTER `applier`,
	ADD COLUMN `confirmed` TINYINT(1) NULL DEFAULT NULL AFTER `date`,
	ADD CONSTRAINT `FK_sales_order_payment_employee` FOREIGN KEY (`applier`) REFERENCES `employee` (`employee_id`) ON UPDATE NO ACTION ON DELETE NO ACTION;	

ALTER TABLE `user_settings`
	CHANGE COLUMN `point_sale` `point_sale` INT(11) NULL AFTER `store`;
	
ALTER TABLE `sales_order`
	ADD COLUMN `partial_deliveries` TINYINT(1) NULL DEFAULT NULL AFTER `priority`;

CREATE TABLE `credit_note` (
	`credit_note_id` INT(11) NOT NULL,
	`sales_order` INT(11) NOT NULL,
	`customer_refund` INT(11) NOT NULL,
	`customer_payment` INT(11) NOT NULL,
	`customer` INT(11) NOT NULL,
	`refunded` TINYINT(4) NOT NULL,
	`cash_session` INT(11) NULL DEFAULT '0',
	`date` DATETIME NULL DEFAULT NULL,
	PRIMARY KEY (`credit_note_id`) USING BTREE,
	INDEX `FK__sales_order` (`sales_order`) USING BTREE,
	INDEX `FK__customer_payment` (`customer_payment`) USING BTREE,
	INDEX `FK__customer` (`customer`) USING BTREE,
	INDEX `FK__cash_session` (`cash_session`) USING BTREE,
	INDEX `FK_credit_note_customer_refund` (`customer_refund`) USING BTREE,
	CONSTRAINT `FK__cash_session` FOREIGN KEY (`cash_session`) REFERENCES `cash_session` (`cash_session_id`) ON UPDATE NO ACTION ON DELETE NO ACTION,
	CONSTRAINT `FK__customer` FOREIGN KEY (`customer`) REFERENCES `customer` (`customer_id`) ON UPDATE NO ACTION ON DELETE NO ACTION,
	CONSTRAINT `FK__customer_payment` FOREIGN KEY (`customer_payment`) REFERENCES `customer_payment` (`customer_payment_id`) ON UPDATE NO ACTION ON DELETE NO ACTION,
	CONSTRAINT `FK__sales_order` FOREIGN KEY (`sales_order`) REFERENCES `sales_order` (`sales_order_id`) ON UPDATE NO ACTION ON DELETE NO ACTION,
	CONSTRAINT `FK_credit_note_customer_refund` FOREIGN KEY (`customer_refund`) REFERENCES `customer_refund` (`customer_refund_id`) ON UPDATE NO ACTION ON DELETE NO ACTION
);

ALTER TABLE `sales_order`
	ADD COLUMN `balance_zeroed_time` DATETIME NULL AFTER `modification_time`;
	ADD COLUMN `sales_quote` INT NULL AFTER `customer`,
	ADD CONSTRAINT `FK_sales_order_sales_quote` FOREIGN KEY (`sales_quote`) REFERENCES `sales_quote` (`sales_quote_id`) ON UPDATE NO ACTION ON DELETE NO ACTION;

ALTER TABLE `deliveries_itinerary`
	CHANGE COLUMN `vehicle` `vehicle` INT(11) NULL AFTER `deliveries_itinerary_id`,
	CHANGE COLUMN `vehicle_operator` `vehicle_operator` INT(11) NULL AFTER `vehicle`;


ALTER TABLE `taxpayer_issuer`
	ADD CONSTRAINT `FK_taxpayer_issuer_sat_postal_code` FOREIGN KEY (`postal_code`) REFERENCES `sat_postal_code` (`sat_postal_code_id`) ON UPDATE NO ACTION ON DELETE NO ACTION;
	
ALTER TABLE `delivery_order`
	ADD COLUMN `priority` TINYINT(3) NOT NULL DEFAULT '1' AFTER `date`;
ALTER TABLE `customer`
	ADD COLUMN `creator` INT NULL AFTER `disabled`,
	ADD CONSTRAINT `FK_customer_employee` FOREIGN KEY (`creator`) REFERENCES `employee` (`employee_id`) ON UPDATE NO ACTION ON DELETE NO ACTION;

ALTER TABLE `delivery_order`
	ADD COLUMN `picked_up` TINYINT(1) NOT NULL DEFAULT '1' AFTER `confirmed`;
	
ALTER TABLE `deliveries_itinerary`
	ADD COLUMN `warehouse` INT NULL AFTER `comment`,
	ADD CONSTRAINT `FK_deliveries_itinerary_warehouse` FOREIGN KEY (`warehouse`) REFERENCES `warehouse` (`warehouse_id`) ON UPDATE NO ACTION ON DELETE NO ACTION;
