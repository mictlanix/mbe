
CREATE TABLE IF NOT EXISTS `service_report` (
	`service_report_id` INT(11) NOT NULL AUTO_INCREMENT,
	`date` DATETIME NOT NULL,
	`customer` INT(11) NOT NULL,
	`type` VARCHAR(128) NOT NULL,
	`location` VARCHAR(512) NULL DEFAULT NULL,
	`supplier` INT(11) NOT NULL,
	`model` VARCHAR(64) NOT NULL,
	`brand` VARCHAR(64) NOT NULL,
	`user_report` VARCHAR(1024) NULL DEFAULT NULL,
	`user_description` VARCHAR(1024) NULL DEFAULT NULL,
	`comment` VARCHAR(1024) NULL DEFAULT NULL,
	PRIMARY KEY (`service_report_id`),
	INDEX `service_report_supplier_idx` (`supplier` ASC),
	INDEX `service_report_customer_idx` (`customer` ASC),
	CONSTRAINT `service_report_customer_fk`
		FOREIGN KEY (`customer`) REFERENCES `customer` (`customer_id`)
		ON DELETE NO ACTION ON UPDATE NO ACTION,
	CONSTRAINT `service_report_supplier_fk`
		FOREIGN KEY (`supplier`) REFERENCES `supplier` (`supplier_id`)
	  	ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE = InnoDB;

CREATE TABLE IF NOT EXISTS `translation_request` (
	`translation_request_id` INT(11) NOT NULL AUTO_INCREMENT,
	`requester` INT(11) NOT NULL,
	`date` DATETIME NOT NULL,
	`agency` VARCHAR(256) NOT NULL,
	`document_name` VARCHAR(128) NOT NULL,
	`amount` DECIMAL(18,4) NOT NULL,
	`delivery_date` DATETIME NOT NULL,
	`comment` VARCHAR(1024) NULL DEFAULT NULL,
	PRIMARY KEY (`translation_request_id`),
	INDEX `translation_request_requester_idx` (`requester` ASC),
	CONSTRAINT `translation_request_requester_fk`
		FOREIGN KEY (`requester`) REFERENCES `employee` (`employee_id`)
		ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE = InnoDB;

CREATE TABLE IF NOT EXISTS `notarization` (
	`notarization_id` INT(11) NOT NULL AUTO_INCREMENT,
	`requester` INT(11) NOT NULL,
	`notary_office` VARCHAR(256) NOT NULL,
	`date` DATETIME NOT NULL,
	`document_description` VARCHAR(512) NOT NULL,
	`amount` DECIMAL(18,4) NOT NULL,
	`payment_date` DATETIME NOT NULL,
	`delivery_date` DATETIME NOT NULL,
	`comment` VARCHAR(1024) NULL DEFAULT NULL,
	PRIMARY KEY (`notarization_id`),
	INDEX `notarization_requester_idx` (`requester` ASC),
	CONSTRAINT `notarization_requester_fk`
		FOREIGN KEY (`requester`) REFERENCES `employee` (`employee_id`)
		ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE = InnoDB;

ALTER TABLE `service_report` 
	DROP FOREIGN KEY `service_report_supplier_fk`,
	DROP FOREIGN KEY `service_report_customer_fk`,
	DROP INDEX `service_report_customer_idx`,
	DROP INDEX `service_report_supplier_idx`,
	DROP COLUMN `supplier`,
	DROP COLUMN `customer`;

ALTER TABLE `service_report` 
	ADD COLUMN `equipment` VARCHAR(64) NOT NULL AFTER `type`,
	ADD COLUMN `serial_number` VARCHAR(64) NULL DEFAULT NULL AFTER `model`,
	ADD COLUMN `user` VARCHAR(128) NULL DEFAULT NULL AFTER `serial_number`,
	ADD COLUMN `technician` VARCHAR(128) NULL DEFAULT NULL AFTER `user`,
	ADD COLUMN `cost` DECIMAL(18,4) NOT NULL AFTER `technician`,
	CHANGE COLUMN `location` `location` VARCHAR(128) NOT NULL AFTER `date`,
	CHANGE COLUMN `brand` `brand` VARCHAR(64) NOT NULL AFTER `equipment`,
	CHANGE COLUMN `user_description` `description` VARCHAR(1024) NULL DEFAULT NULL AFTER `user_report`;
