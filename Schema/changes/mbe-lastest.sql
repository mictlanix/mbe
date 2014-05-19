
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
