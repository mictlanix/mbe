
DROP TABLE IF EXISTS `delivery_order_detail`;
DROP TABLE IF EXISTS `delivery_address`;
DROP TABLE IF EXISTS `delivery_order`;

CREATE TABLE IF NOT EXISTS `delivery_order` (
  `delivery_order_id` INT NOT NULL AUTO_INCREMENT,
  `creator` INT NOT NULL,
  `updater` INT NOT NULL,
  `creation_time` DATETIME NOT NULL,
  `modification_time` DATETIME NOT NULL,
  `store` INT NOT NULL,
  `serial` INT NOT NULL,
  `customer` INT NOT NULL,
  `ship_to` INT NULL,
  `date` DATETIME NOT NULL,
  `completed` TINYINT(1) NOT NULL,
  `cancelled` TINYINT(1) NOT NULL,
  `comment` VARCHAR(500) NULL,
  PRIMARY KEY (`delivery_order_id`))
ENGINE = InnoDB;

CREATE TABLE IF NOT EXISTS `delivery_order_detail` (
  `delivery_order_detail_id` INT NOT NULL AUTO_INCREMENT,
  `delivery_order` INT NOT NULL,
  `sales_order_detail` INT NULL,
  `product` INT NOT NULL,
  `quantity` DECIMAL(18,4) NOT NULL,
  `product_code` VARCHAR(425) NOT NULL,
  `product_name` VARCHAR(250) NOT NULL,
  PRIMARY KEY (`delivery_order_detail_id`),
  INDEX `dod_delivery_order_idx` (`delivery_order` ASC),
  INDEX `dod_sales_order_detail_idx` (`sales_order_detail` ASC),
  CONSTRAINT `dod_delivery_order_fk`
    FOREIGN KEY (`delivery_order`)
    REFERENCES `delivery_order` (`delivery_order_id`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT `dod_sales_order_detail_fk`
    FOREIGN KEY (`sales_order_detail`)
    REFERENCES `sales_order_detail` (`sales_order_detail_id`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB;

ALTER TABLE `fiscal_document`
	ADD COLUMN `retention_rate` DECIMAL(5,4) NOT NULL;
	