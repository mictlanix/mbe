SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0;
SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0;
SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='TRADITIONAL,ALLOW_INVALID_DATES';

CREATE TABLE `sat_unit_of_measurement` (
  `sat_unit_of_measurement_id` varchar(3) NOT NULL,
  `name` varchar(128) NOT NULL,
  `description` varchar(1024) NULL,
  `symbol` varchar(32) NULL,
  PRIMARY KEY (`sat_unit_of_measurement_id`)
);

CREATE TABLE `sat_product_service` (
  `sat_product_service_id` varchar(8) NOT NULL,
  `description` varchar(256) NOT NULL,
  `keywords` varchar(1024) NULL,
  PRIMARY KEY (`sat_product_service_id`)
);

CREATE TABLE `sat_postal_code` (
  `sat_postal_code_id` varchar(5) NOT NULL,
  `state` varchar(4) NOT NULL,
  `borough` varchar(3) NULL,
  `locality` varchar(2) NULL,
  PRIMARY KEY (`sat_postal_code_id`)
);

CREATE TABLE `sat_currency` (
  `sat_currency_id` varchar(3) NOT NULL,
  `description` varchar(256) NOT NULL,
  PRIMARY KEY (`sat_currency_id`)
);

CREATE TABLE `sat_country` (
  `sat_country_id` varchar(3) NOT NULL,
  `description` varchar(256) NOT NULL,
  PRIMARY KEY (`sat_country_id`)
);

CREATE TABLE `sat_tax_regime` (
  `sat_tax_regime_id` varchar(3) NOT NULL,
  `description` varchar(256) NOT NULL,
  PRIMARY KEY (`sat_tax_regime_id`)
);

CREATE TABLE `sat_cfdi_usage` (
  `sat_cfdi_usage_id` varchar(3) NOT NULL,
  `description` varchar(256) NOT NULL,
  PRIMARY KEY (`sat_cfdi_usage_id`)
);

UPDATE `sat_unit_of_measurement`  SET description = NULL  WHERE TRIM(description) = '';
UPDATE `sat_unit_of_measurement`  SET symbol = NULL 		  WHERE TRIM(symbol) = '';
UPDATE `sat_product_service` 	    SET keywords = NULL 	  WHERE TRIM(keywords) = '';
UPDATE `sat_postal_code`		      SET `state` = 'CDMX'    WHERE `state` = 'DIF';
UPDATE `sat_postal_code`		      SET borough = NULL 	    WHERE TRIM(borough) = '';
UPDATE `sat_postal_code` 	 	      SET locality = NULL 	  WHERE TRIM(locality) = '';

-- FiscalReports
DELETE FROM `access_privilege` WHERE `object` = 31;

INSERT INTO `sat_unit_of_measurement` VALUES ('N/A','*Requerida*',NULL,NULL);

UPDATE product SET name = TRIM(name);
UPDATE product SET unit_of_measurement = 'H87' WHERE TRIM(unit_of_measurement) IN ('Pieza', 'PZA', 'P');
UPDATE product SET unit_of_measurement = 'XBX' WHERE TRIM(unit_of_measurement) = 'Caja';
UPDATE product SET unit_of_measurement = 'HUR' WHERE TRIM(unit_of_measurement) IN ('h', 'HORA');
UPDATE product SET unit_of_measurement = 'SET' WHERE TRIM(unit_of_measurement) = 'Juego';
UPDATE product SET unit_of_measurement = 'TP'  WHERE TRIM(unit_of_measurement) = 'Decena';
UPDATE product SET unit_of_measurement = 'MIL' WHERE TRIM(unit_of_measurement) = 'Millar';
UPDATE product SET unit_of_measurement = 'MTR' WHERE TRIM(unit_of_measurement) IN ('m', 'METRO');
UPDATE product SET unit_of_measurement = 'MTK' WHERE TRIM(unit_of_measurement) IN ('m²', 'METRO CUADRADO', '2M');
UPDATE product SET unit_of_measurement = 'MTQ' WHERE TRIM(unit_of_measurement) IN ('m³', 'METRO CUBICO');
UPDATE product SET unit_of_measurement = 'LTR' WHERE TRIM(unit_of_measurement) IN ('l', 'LITRO');
UPDATE product SET unit_of_measurement = 'GRM' WHERE TRIM(unit_of_measurement) IN ('g', 'GRAMO');
UPDATE product SET unit_of_measurement = 'KGM' WHERE TRIM(unit_of_measurement) IN ('kg', 'KILOGRAMO');
UPDATE product SET unit_of_measurement = 'TNE' WHERE TRIM(unit_of_measurement) IN ('t', 'Tonelada');
UPDATE product SET unit_of_measurement = 'SEC' WHERE TRIM(unit_of_measurement) IN ('s', 'Segundo');
UPDATE product SET unit_of_measurement = 'HC'  WHERE TRIM(unit_of_measurement) = 'Ciento';
UPDATE product SET unit_of_measurement = 'PR'  WHERE TRIM(unit_of_measurement) = 'PAR';
UPDATE product SET unit_of_measurement = 'XRO' WHERE TRIM(unit_of_measurement) = 'ROLLO';

ALTER TABLE `product` 
  ADD COLUMN `key` VARCHAR(8) NULL,
  CHANGE COLUMN `unit_of_measurement` `unit_of_measurement` VARCHAR(3) NOT NULL,
  CHANGE COLUMN `tax_rate` `tax_rate` DECIMAL(7,6) NOT NULL,
  ADD INDEX `product_uom_idx` (`unit_of_measurement` ASC),
  ADD INDEX `product_key_idx` (`key` ASC),
  ADD CONSTRAINT `product_uom_fk`
    FOREIGN KEY (`unit_of_measurement`) REFERENCES `sat_unit_of_measurement` (`sat_unit_of_measurement_id`)
    ON DELETE NO ACTION ON UPDATE NO ACTION,
  ADD CONSTRAINT `product_key_fk`
    FOREIGN KEY (`key`) REFERENCES `sat_product_service` (`sat_product_service_id`)
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- UPDATE taxpayer_issuer SET regime = '601';

ALTER TABLE `taxpayer_issuer`
  DROP FOREIGN KEY `taxpayer_issuer_address_fk`,
  DROP INDEX `taxpayer_issuer_address_idx`,
  DROP COLUMN `address`,
  DROP COLUMN `scheme`;

ALTER TABLE `taxpayer_issuer`
  CHANGE COLUMN `regime` `regime` VARCHAR(3) NOT NULL,
  ADD INDEX `taxpayer_issuer_regime_idx` (`regime` ASC),
  ADD CONSTRAINT `taxpayer_issuer_regime_fk`
    FOREIGN KEY (`regime`) REFERENCES `sat_tax_regime` (`sat_tax_regime_id`)
    ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE `taxpayer_recipient`
  DROP FOREIGN KEY `taxpayer_recipient_address`,
  DROP INDEX `taxpayer_recipient_address_idx`,
  DROP COLUMN `address`;

ALTER TABLE `taxpayer_recipient` 
  CHANGE COLUMN `name` `name` VARCHAR(250) NULL;

-- UPDATE store SET location = '03810';

ALTER TABLE `store` 
  CHANGE COLUMN `location` `location` VARCHAR(5) NOT NULL,
  ADD INDEX `store_location_idx` (`location` ASC),
  ADD CONSTRAINT `store_location_fk`
    FOREIGN KEY (`location`) REFERENCES `sat_postal_code` (`sat_postal_code_id`)
    ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE `fiscal_document` 
  CHANGE COLUMN `issuer_regime` `issuer_regime_name` VARCHAR(250) NULL;

ALTER TABLE `fiscal_document`
  ADD COLUMN `issuer_regime` VARCHAR(3) NULL AFTER `issuer_name`,
  ADD COLUMN `payment_terms` TINYINT NOT NULL AFTER `currency`,
  ADD COLUMN `usage` VARCHAR(3) NULL AFTER `payment_terms`,
  ADD INDEX `fiscal_document_issuer_regime_idx` (`issuer_regime` ASC),
  ADD INDEX `fiscal_document_usage_idx` (`usage` ASC),
  ADD CONSTRAINT `fiscal_document_issuer_regime_fk`
    FOREIGN KEY (`issuer_regime`) REFERENCES `sat_tax_regime` (`sat_tax_regime_id`)
    ON DELETE NO ACTION ON UPDATE NO ACTION,
  ADD CONSTRAINT `fiscal_document_usage_fk`
    FOREIGN KEY (`usage`) REFERENCES `sat_cfdi_usage` (`sat_cfdi_usage_id`)
    ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE `fiscal_document_detail` 
  CHANGE COLUMN `unit_of_measurement` `unit_of_measurement_name` VARCHAR(25) NOT NULL;

ALTER TABLE `fiscal_document_detail`
  CHANGE COLUMN `tax_rate` `tax_rate` DECIMAL(7,6) NOT NULL,
  ADD COLUMN `product_service` VARCHAR(8) NULL AFTER `order_detail`,
  ADD COLUMN `unit_of_measurement` VARCHAR(3) NULL AFTER `product_name`,
  ADD INDEX `fiscal_document_product_service_idx` (`product_service` ASC),
  ADD INDEX `fiscal_document_uom_idx` (`unit_of_measurement` ASC),
  ADD CONSTRAINT `fiscal_document_uom_fk`
    FOREIGN KEY (`unit_of_measurement`) REFERENCES `sat_unit_of_measurement` (`sat_unit_of_measurement_id`)
    ON DELETE NO ACTION ON UPDATE NO ACTION,
  ADD CONSTRAINT `fiscal_document_product_service_fk`
    FOREIGN KEY (`product_service`) REFERENCES `sat_product_service` (`sat_product_service_id`)
    ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE `taxpayer_batch` 
	CHANGE COLUMN `template` `template` TEXT NOT NULL;

-- complemento de pago

ALTER TABLE `fiscal_document`
  ADD COLUMN `payment_date` DATE NULL,
  ADD COLUMN `payment_amount` decimal(18,2) NULL;

CREATE TABLE `fiscal_document_relation` (
  `fiscal_document_relation_id` int(11) NOT NULL AUTO_INCREMENT,
  `document` int(11) NOT NULL,
  `relation` int(11) NOT NULL,
  `exchange_rate` decimal(8,4) NOT NULL DEFAULT 1.0000,
  `installment` int(11) NOT NULL,
  `previous_balance` decimal(18,2) NOT NULL,
  `amount` decimal(18,2) NOT NULL,
  PRIMARY KEY (`fiscal_document_relation_id`),
  UNIQUE KEY `document_relation_idx` (`document`, `relation`),
  KEY `fiscal_document_relation_document_idx` (`document`),
  CONSTRAINT `fiscal_document_relation_document_fk` FOREIGN KEY (`document`) REFERENCES `fiscal_document` (`fiscal_document_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fiscal_document_relation_relation_fk` FOREIGN KEY (`relation`) REFERENCES `fiscal_document` (`fiscal_document_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB;

ALTER TABLE `sales_order`
	ADD COLUMN `customer_shipto` VARCHAR(200) NULL AFTER `customer_name`;
	
CREATE TABLE `special_receipt` (
	`special_receipt_id` INT(11) NOT NULL AUTO_INCREMENT,
	`creation_time` DATETIME NOT NULL,
	`modification_time` DATETIME NOT NULL,
	`creator` INT(11) NOT NULL DEFAULT '0',
	`updater` INT(11) NOT NULL DEFAULT '0',
	`salesperson` INT(11) NOT NULL DEFAULT '0',
	`store` INT(11) NOT NULL DEFAULT '0',
	`serial` INT(11) NOT NULL DEFAULT '0',
	`customer` VARCHAR(100) NULL DEFAULT '' COLLATE 'utf8_spanish_ci',
	`ship_to` VARCHAR(100) NULL DEFAULT '' COLLATE 'utf8_spanish_ci',
	`date` DATETIME NOT NULL,
	`completed` TINYINT(1) NOT NULL DEFAULT '0',
	`cancelled` TINYINT(1) NOT NULL DEFAULT '0',
	`delivered` TINYINT(1) NOT NULL DEFAULT '0',
	`comment` VARCHAR(500) NULL DEFAULT '' COLLATE 'utf8_spanish_ci',
	`JSON` VARCHAR(1500) NULL DEFAULT '' COLLATE 'utf8_spanish_ci',
	PRIMARY KEY (`special_receipt_id`),
	INDEX `FK__employee` (`creator`),
	INDEX `FK__employee_2` (`updater`),
	INDEX `FK__store` (`store`),
	INDEX `FK_special_receipt_employee` (`salesperson`),
	CONSTRAINT `FK__employee` FOREIGN KEY (`creator`) REFERENCES `employee` (`employee_id`),
	CONSTRAINT `FK__employee_2` FOREIGN KEY (`updater`) REFERENCES `employee` (`employee_id`),
	CONSTRAINT `FK__store` FOREIGN KEY (`store`) REFERENCES `store` (`store_id`),
	CONSTRAINT `FK_special_receipt_employee` FOREIGN KEY (`salesperson`) REFERENCES `employee` (`employee_id`)
)
COLLATE='utf8_spanish_ci' ENGINE=InnoDB;

ALTER TABLE `expense_voucher_detail` 
	CHANGE COLUMN `amount` `amount` DECIMAL(18,2) NOT NULL;
    
ALTER TABLE `fiscal_document_detail` 
	CHANGE COLUMN `price` `price` DECIMAL(18,7) NOT NULL;
    
ALTER TABLE `purchase_order_detail` 
	CHANGE COLUMN `price` `price` DECIMAL(18,7) NOT NULL;
    
ALTER TABLE `sales_order_detail` 
	CHANGE COLUMN `price` `price` DECIMAL(18,7) NOT NULL;
    
ALTER TABLE `sales_quote_detail` 
	CHANGE COLUMN `price` `price` DECIMAL(18,7) NOT NULL;


-- NOV 2020

UPDATE `contact` SET `mobile` = '' WHERE `mobile` IS NULL;

ALTER TABLE `contact` 
	CHANGE COLUMN `phone` `phone` VARCHAR(25) NULL,
	CHANGE COLUMN `mobile` `mobile` VARCHAR(25) NOT NULL DEFAULT '';
	
ALTER TABLE `sales_order_detail`
	ADD COLUMN `lot_serial_tracking` INT NULL DEFAULT NULL AFTER `product`;

ALTER TABLE `sales_order_detail`
	ADD CONSTRAINT `FK_sales_order_detail_lot_serial_tracking` FOREIGN KEY (`lot_serial_tracking`) REFERENCES `lot_serial_tracking` (`lot_serial_tracking_id`);


	
ALTER TABLE `supplier_payment`
	ADD COLUMN `cancelled` TINYINT(1) NULL DEFAULT NULL;

ALTER TABLE `supplier_payment`
	ADD COLUMN `cash_session` INT(11) NULL DEFAULT NULL AFTER `cancelled`,
	ADD CONSTRAINT `FK_supplier_payment_cash_session` FOREIGN KEY (`cash_session`) REFERENCES `cash_session` (`cash_session_id`);
	
CREATE TABLE IF NOT EXISTS `purchase_clearance` (
  `purchase_clearance_id` int(11) NOT NULL AUTO_INCREMENT,
  `purchase_order` int(11) NOT NULL,
  `creation_time` datetime NOT NULL,
  `modification_time` datetime NOT NULL,
  `creator` int(11) NOT NULL DEFAULT '0',
  `updater` int(11) NOT NULL DEFAULT '0',
  `comment` varchar(250) DEFAULT '',
  `completed` tinyint(1) NOT NULL DEFAULT '0',
  `cancelled` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`purchase_clearance_id`),
  KEY `FK__purchase_order` (`purchase_order`),
  KEY `FK__employee_creator` (`creator`),
  KEY `FK__employee_updater` (`updater`),
  CONSTRAINT `FK__employee_creator` FOREIGN KEY (`creator`) REFERENCES `employee` (`employee_id`),
  CONSTRAINT `FK__employee_updater` FOREIGN KEY (`updater`) REFERENCES `employee` (`employee_id`),
  CONSTRAINT `FK__purchase_order` FOREIGN KEY (`purchase_order`) REFERENCES `purchase_order` (`purchase_order_id`)
) ENGINE=InnoDB AUTO_INCREMENT=32 DEFAULT CHARSET=utf8;

CREATE TABLE IF NOT EXISTS `fiscal_document_detail_sales_order_detail` (
  `fiscal_document_detail_sales_order_detail_id` int(11) NOT NULL AUTO_INCREMENT,
  `fiscal_document_detail` int(11) NOT NULL,
  `sales_order_detail` int(11) NOT NULL,
  PRIMARY KEY (`fiscal_document_detail_sales_order_detail_id`),
  UNIQUE KEY `sales_order_id` (`sales_order_detail`),
  KEY `FK_fiscal_document_sales_orders_fiscal_document` (`fiscal_document_detail`),
  CONSTRAINT `FK_fiscal_document_sales_orders_fiscal_document_detail` FOREIGN KEY (`fiscal_document_detail`) REFERENCES `fiscal_document_detail` (`fiscal_document_detail_id`),
  CONSTRAINT `FK_fiscal_document_sales_orders_sales_order_detail` FOREIGN KEY (`sales_order_detail`) REFERENCES `sales_order_detail` (`sales_order_detail_id`)
);
CREATE TABLE IF NOT EXISTS `purchase_clearance` (
  `purchase_clearance_id` int(11) NOT NULL AUTO_INCREMENT,
  `purchase_order` int(11) NOT NULL,
  `supplier` varchar(50) NOT NULL DEFAULT '',
  `lot_code` varchar(50) NOT NULL DEFAULT '',
  `creation_time` datetime NOT NULL,
  `modification_time` datetime NOT NULL,
  `creator` int(11) NOT NULL DEFAULT '0',
  `updater` int(11) NOT NULL DEFAULT '0',
  `comment` varchar(250) DEFAULT '',
  `commission` decimal(6,4) NOT NULL DEFAULT '0.0000',
  `completed` tinyint(1) NOT NULL DEFAULT '0',
  `cancelled` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`purchase_clearance_id`),
  KEY `FK__purchase_order` (`purchase_order`),
  KEY `FK__employee_creator` (`creator`),
  KEY `FK__employee_updater` (`updater`),
  CONSTRAINT `FK__employee_creator` FOREIGN KEY (`creator`) REFERENCES `employee` (`employee_id`),
  CONSTRAINT `FK__employee_updater` FOREIGN KEY (`updater`) REFERENCES `employee` (`employee_id`)
);

-- La exportación de datos fue deseleccionada.

-- Volcando estructura para tabla mbe_q13.purchase_clearance_detail
CREATE TABLE IF NOT EXISTS `purchase_clearance_detail` (
  `purchase_clearance_detail_id` int(11) NOT NULL AUTO_INCREMENT,
  `purchase_clearance` int(11) NOT NULL,
  `warehouse` varchar(50) DEFAULT NULL,
  `quantity` decimal(10,0) DEFAULT NULL,
  `price` decimal(10,0) DEFAULT NULL,
  `discount` decimal(10,0) DEFAULT NULL,
  `tax_rate` decimal(10,0) DEFAULT NULL,
  `product_code` varchar(50) DEFAULT NULL,
  `product_name` varchar(50) DEFAULT NULL,
  `unit_of_measurement` varchar(50) DEFAULT NULL,
  `exchange_rate` decimal(10,0) DEFAULT NULL,
  `currency` decimal(10,0) DEFAULT NULL,
  `tax_included` tinyint(1) NOT NULL DEFAULT '0',
  `is_charge` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`purchase_clearance_detail_id`),
  KEY `FK_purchase_clearance_detail_purchase_clearance` (`purchase_clearance`),
  CONSTRAINT `FK_purchase_clearance_detail_purchase_clearance` FOREIGN KEY (`purchase_clearance`) REFERENCES `purchase_clearance` (`purchase_clearance_id`)
);

-- La exportación de datos fue deseleccionada.

-- Volcando estructura para tabla mbe_q13.purchase_clearance_entry
CREATE TABLE IF NOT EXISTS `purchase_clearance_entry` (
  `purchase_clearance_entry_id` int(11) NOT NULL AUTO_INCREMENT,
  `purchase_clearance_detail` int(11) NOT NULL,
  `product_name` varchar(200) DEFAULT NULL,
  `quantity` int(11) NOT NULL,
  `price` decimal(18,7) NOT NULL DEFAULT '0.0000000',
  `discount` decimal(9,8) NOT NULL DEFAULT '0.00000000',
  `tax_rate` decimal(5,4) NOT NULL DEFAULT '0.0000',
  `exchange_rate` decimal(8,4) NOT NULL DEFAULT '0.0000',
  `currency` int(11) NOT NULL DEFAULT '0',
  `tax_included` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`purchase_clearance_entry_id`),
  KEY `FK_purchase_clearance_entry_purchase_clearance_detail` (`purchase_clearance_detail`),
  CONSTRAINT `FK_purchase_clearance_entry_purchase_clearance_detail` FOREIGN KEY (`purchase_clearance_detail`) REFERENCES `purchase_clearance_detail` (`purchase_clearance_detail_id`)
) ;

ALTER TABLE `expense_voucher`
	ADD COLUMN `purchase_order` INT NULL AFTER `cancelled`;

SET SQL_MODE=@OLD_SQL_MODE;
SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS;
