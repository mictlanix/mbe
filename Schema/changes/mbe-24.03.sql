ALTER TABLE `product`
	ADD COLUMN `deactivated` TINYINT(1) ZEROFILL NOT NULL AFTER `key`;
	
ALTER TABLE `delivery_order`
	ADD COLUMN `contact` INT NULL AFTER `ship_to`;
	
ALTER TABLE `sales_quote_detail`
	ADD COLUMN `price_adjustment` DECIMAL(18,7) NOT NULL DEFAULT '0' AFTER `price`;