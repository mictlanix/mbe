
start transaction;
ALTER TABLE `taxpayer_certificate` CHANGE COLUMN `taxpayer_certificate_id` `taxpayer_certificate_id` CHAR(20) NOT NULL;
ALTER TABLE `fiscal_document` CHANGE COLUMN `issuer_certificate_number` `issuer_certificate_number` CHAR(20) NULL DEFAULT NULL;

ALTER TABLE `taxpayer_batch` 
	ADD COLUMN `template` VARCHAR(25) NOT NULL AFTER `approval_year`;

ALTER TABLE `taxpayer` 
	DROP FOREIGN KEY `taxpayer_address_fk`,
	DROP INDEX `taxpayer_address_fk_idx`;

ALTER TABLE `taxpayer`
	RENAME TO  `taxpayer_issuer`,
	CHANGE COLUMN `taxpayer_id` `taxpayer_issuer_id` VARCHAR(13) NOT NULL;

ALTER TABLE `taxpayer_issuer`
	ADD INDEX `taxpayer_issuer_address_idx` (`address` ASC),
	ADD CONSTRAINT `taxpayer_issuer_address_fk`
  		FOREIGN KEY (`address`) REFERENCES `address` (`address_id`)
  		ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE `customer_taxpayer` 
	DROP FOREIGN KEY `customer_taxpayer_address`,
	DROP FOREIGN KEY `customer_taxpayer_customer_fk`,
	DROP INDEX `customer_taxpayer_customer_fk_idx`,
	DROP INDEX `customer_taxpayer_address_idx`;

ALTER TABLE `customer_taxpayer`
	RENAME TO `taxpayer_recipient`,
	CHANGE COLUMN `customer_taxpayer_id` `taxpayer_recipient_id` VARCHAR(13) NOT NULL;
	
ALTER TABLE `taxpayer_recipient`
	ADD INDEX `taxpayer_recipient_address_idx` (`address` ASC),
	ADD CONSTRAINT `taxpayer_recipient_address`
		FOREIGN KEY (`address`) REFERENCES `address` (`address_id`)
  		ON DELETE NO ACTION ON UPDATE NO ACTION;

CREATE TABLE IF NOT EXISTS `customer_taxpayer` (
	`customer` INT NOT NULL,
	`taxpayer` VARCHAR(13) NOT NULL,
	PRIMARY KEY (`customer`, `taxpayer`),
	INDEX `customer_taxpayer_customer_idx` (`customer` ASC),
	INDEX `customer_taxpayer_taxpayer_idx` (`taxpayer` ASC),
	CONSTRAINT `customer_taxpayer_customer_fk`
		FOREIGN KEY (`customer`) REFERENCES `customer` (`customer_id`)
		ON DELETE NO ACTION ON UPDATE NO ACTION,
	CONSTRAINT `customer_taxpayer_taxpayer_fk`
		FOREIGN KEY (`taxpayer`) REFERENCES `taxpayer_recipient` (`taxpayer_recipient_id`)
		ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE = InnoDB;

INSERT INTO
	customer_taxpayer
SELECT
	customer, taxpayer_recipient_id
FROM
	taxpayer_recipient;

ALTER TABLE `taxpayer_recipient`
	DROP COLUMN `customer`;

ALTER TABLE `customer` 
	CHANGE COLUMN `comment` `comment` VARCHAR(1024) NULL,
	ADD COLUMN `shipping` TINYINT(1) NOT NULL,
	ADD COLUMN `shipping_required_document` TINYINT(1) NOT NULL;

CREATE TABLE IF NOT EXISTS `customer_discount` (
  `customer_discount_id` INT NOT NULL AUTO_INCREMENT,
  `customer` INT NOT NULL,
  `product` INT NOT NULL,
  `discount` DECIMAL(5,4) NOT NULL,
  PRIMARY KEY (`customer_discount_id`),
  INDEX `customer_discount_customer_fk` (`customer` ASC),
  INDEX `customer_discount_product_fk` (`product` ASC),
  UNIQUE INDEX `customer_discount_unique_idx` (`customer` ASC, `product` ASC),
  CONSTRAINT `customer_discount_customer_fk`
    FOREIGN KEY (`customer`) REFERENCES `customer` (`customer_id`)
    ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `customer_discount_product_fk`
    FOREIGN KEY (`product`) REFERENCES `product` (`product_id`)
    ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE = InnoDB;

commit;

