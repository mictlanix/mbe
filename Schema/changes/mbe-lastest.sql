
UPDATE employee SET birthday = '1900-01-01' WHERE birthday IS NULL;

ALTER TABLE employee
	ADD COLUMN `nickname` VARCHAR(50) NOT NULL AFTER `last_name`,
	ADD COLUMN `sales_person` TINYINT(1) NOT NULL AFTER `gender`,
	ADD COLUMN `active` TINYINT(1) NOT NULL AFTER `sales_person`,
	CHANGE COLUMN `gender` `gender` TINYINT(4) NOT NULL AFTER `nickname`,
	CHANGE COLUMN `birthday` `birthday` DATE NOT NULL;

UPDATE employee SET active = 1, sales_person = 1;

ALTER TABLE product_price 
  DROP COLUMN `currency`;

ALTER TABLE product
  ADD COLUMN `currency` INT(11) NOT NULL AFTER `price_type`,
  ADD COLUMN `min_order_qty` INT(11) NOT NULL AFTER `currency`;

UPDATE product SET currency = 0, min_order_qty = 1;

ALTER TABLE store
  DROP COLUMN `comment`,
  CHANGE COLUMN `taxpayer` `taxpayer` VARCHAR(13) NOT NULL AFTER `address`,
  CHANGE COLUMN `logo` `logo` VARCHAR(255) NOT NULL,
  CHANGE COLUMN `receipt_message` `receipt_message` VARCHAR(250) NULL DEFAULT NULL AFTER `logo`,
  DROP INDEX `store_address_fk_idx` ,
  ADD INDEX `store_address_idx` (`address` ASC),
  ADD INDEX `store_taxpayer_idx` (`taxpayer` ASC);

ALTER TABLE store
  ADD CONSTRAINT `store_taxpayer_fk`
    FOREIGN KEY (`taxpayer`)
    REFERENCES `mbe_db`.`taxpayer` (`taxpayer_id`)
    ON DELETE NO ACTION ON UPDATE NO ACTION;
  