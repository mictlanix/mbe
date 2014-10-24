ALTER TABLE `product` 
	DROP INDEX `product_supplier_fk_idx`,
	ADD INDEX `product_supplier_idx` (`supplier` ASC),
	ADD INDEX `product_brand_idx` (`brand` ASC);

ALTER TABLE `lot_serial_tracking` 
	DROP INDEX `lot_serial_tracking_warehouse_fk_idx`,
	DROP INDEX `lot_serial_tracking_product_fk_idx`,
	ADD INDEX `lot_serial_tracking_warehouse_idx` (`warehouse` ASC),
	ADD INDEX `lot_serial_tracking_product_idx` (`product` ASC),
	ADD INDEX `lot_serial_tracking_w_p_idx` (`warehouse` ASC, `product` ASC),
	ADD INDEX `lot_serial_tracking_w_p_l_idx` (`warehouse` ASC, `product` ASC, `lot_number` ASC, `expiration_date` ASC),
	ADD INDEX `lot_serial_tracking_w_p_l_s_idx` (`warehouse` ASC, `product` ASC, `lot_number` ASC, `expiration_date` ASC, `serial_number` ASC);
