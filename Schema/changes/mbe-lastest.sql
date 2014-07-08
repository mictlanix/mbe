
ALTER TABLE `service_report` 
	DROP COLUMN `service_report_id`,
	DROP PRIMARY KEY,
	ADD COLUMN `tech_service_report_id` INT(11) NOT NULL AUTO_INCREMENT FIRST,
	ADD PRIMARY KEY (`tech_service_report_id`),
	RENAME TO `tech_service_report`;


CREATE TABLE IF NOT EXISTS `tech_service_request` (
	`tech_service_request_id` INT NOT NULL AUTO_INCREMENT,
	`type` INT NOT NULL,
	`brand` VARCHAR(64) NOT NULL,
	`equipment` VARCHAR(64) NOT NULL,
	`model` VARCHAR(64) NOT NULL,
	`serial_number` VARCHAR(64) NULL,
	`date` DATETIME NOT NULL,
	`end_date` DATETIME NULL,
	`customer` INT NOT NULL,
	`responsible` VARCHAR(128) NOT NULL,
	`location` VARCHAR(128) NOT NULL,
	`payment_status` VARCHAR(64) NULL,
	`shipping_method` VARCHAR(64) NULL,
	`contact_name` VARCHAR(128) NULL,
	`contact_phone_number` VARCHAR(64) NULL,
	`contact_address` VARCHAR(256) NULL,
	`remarks` VARCHAR(1024) NULL,
	`comment` VARCHAR(1024) NULL,
	PRIMARY KEY (`tech_service_request_id`),
	INDEX `tech_service_request_customer_idx` (`customer` ASC),
	CONSTRAINT `tech_service_request_customer_fk`
		FOREIGN KEY (`customer`) REFERENCES `customer` (`customer_id`)
		ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE = InnoDB;


CREATE TABLE IF NOT EXISTS `tech_service_request_component` (
	`tech_service_request_component_id` INT NOT NULL AUTO_INCREMENT,
	`request` INT NOT NULL,
	`name` VARCHAR(128) NOT NULL,
	`quantity` INT NOT NULL,
	`serial_number` VARCHAR(64) NULL,
	`comment` VARCHAR(256) NULL,
	PRIMARY KEY (`tech_service_request_component_id`),
	INDEX `tech_service_request_component_request_idx` (`request` ASC),
	CONSTRAINT `tech_service_request_component_request_fk`
		FOREIGN KEY (`request`) REFERENCES `tech_service_request` (`tech_service_request_id`)
		ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE = InnoDB;


