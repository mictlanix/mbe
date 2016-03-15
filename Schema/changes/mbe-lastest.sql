
SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0;
SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0;
SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='TRADITIONAL,ALLOW_INVALID_DATES';

CREATE TABLE time_clock (
  time_clock_id char(36) COLLATE utf8_unicode_ci NOT NULL,
  address varchar(16) COLLATE utf8_unicode_ci NOT NULL,
  last_sync datetime NOT NULL,
  sync tinyint(1) NOT NULL,
  PRIMARY KEY (time_clock_id)
) ENGINE=InnoDB;

CREATE TABLE time_clock_record (
  time_clock_record_id char(36) COLLATE utf8_unicode_ci NOT NULL,
  enroll_number int(11) NOT NULL,
  datetime datetime NOT NULL,
  type int(11) NOT NULL,
  employee int(11) DEFAULT NULL,
  PRIMARY KEY (time_clock_record_id),
  KEY time_clock_record_employee_idx (employee),
  CONSTRAINT time_clock_record_employee_fk FOREIGN KEY (employee) REFERENCES employee (employee_id) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB;

ALTER TABLE inventory_receipt 
	DROP FOREIGN KEY inventory_receipt_employee_creator,
	DROP FOREIGN KEY inventory_receipt_employee_updater,
	DROP FOREIGN KEY inventory_receipt_purchase_order,
    DROP FOREIGN KEY inventory_receipt_warehouse,
    DROP INDEX inventory_receipt_employee_creator_idx,
    DROP INDEX inventory_receipt_employee_updater_idx;

ALTER TABLE inventory_receipt 
	ADD COLUMN store INT(11) NOT NULL AFTER inventory_receipt_id,
	ADD COLUMN serial INT(11) NULL AFTER store,
	ADD INDEX inventory_receipt_creator_idx (creator ASC),
	ADD INDEX inventory_receipt_updater_idx (updater ASC),
	ADD INDEX inventory_receipt_store_idx (store ASC),
	ADD CONSTRAINT inventory_receipt_creator_fk
		FOREIGN KEY (creator) REFERENCES employee (employee_id)
		ON DELETE NO ACTION ON UPDATE NO ACTION,
	ADD CONSTRAINT inventory_receipt_updater_fk
		FOREIGN KEY (updater) REFERENCES employee (employee_id)
		ON DELETE NO ACTION ON UPDATE NO ACTION,
	ADD CONSTRAINT inventory_receipt_warehouse_fk
		FOREIGN KEY (warehouse) REFERENCES warehouse (warehouse_id)
		ON DELETE NO ACTION ON UPDATE NO ACTION,
	ADD CONSTRAINT inventory_receipt_purchase_order_fk
		FOREIGN KEY (purchase_order) REFERENCES purchase_order (purchase_order_id)
		ON DELETE NO ACTION ON UPDATE NO ACTION,
	ADD CONSTRAINT inventory_receipt_store_fk
		FOREIGN KEY (store) REFERENCES store (store_id)
		ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE inventory_issue 
	DROP FOREIGN KEY inventory_issue_employee_creator,
	DROP FOREIGN KEY inventory_issue_employee_updater,
    DROP FOREIGN KEY inventory_issue_warehouse,
    DROP FOREIGN KEY inventory_issue_supplier_return_fk,
    DROP INDEX inventory_issue_supplier_return_fk_idx;

ALTER TABLE inventory_issue 
	ADD COLUMN store INT(11) NOT NULL AFTER inventory_issue_id,
	ADD COLUMN serial INT(11) NULL AFTER store,
	ADD INDEX inventory_issue_supplier_return_idx (supplier_return ASC),
	ADD INDEX inventory_issue_store_idx (store ASC),
	ADD CONSTRAINT inventory_issue_employee_creator_fk
		FOREIGN KEY (creator) REFERENCES employee (employee_id)
		ON DELETE NO ACTION ON UPDATE NO ACTION,
	ADD CONSTRAINT inventory_issue_employee_updater_fk
		FOREIGN KEY (updater) REFERENCES employee (employee_id)
		ON DELETE NO ACTION ON UPDATE NO ACTION,
	ADD CONSTRAINT inventory_issue_warehouse_fk
		FOREIGN KEY (warehouse) REFERENCES warehouse (warehouse_id)
		ON DELETE NO ACTION ON UPDATE NO ACTION,
	ADD CONSTRAINT inventory_issue_supplier_return_fk
		FOREIGN KEY (supplier_return) REFERENCES supplier_return (supplier_return_id)
		ON DELETE NO ACTION ON UPDATE NO ACTION,
	ADD CONSTRAINT inventory_issue_store_fk
		FOREIGN KEY (store) REFERENCES store (store_id)
		ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE inventory_transfer 
	DROP FOREIGN KEY inventory_transfer_employee_creator_fk,
	DROP FOREIGN KEY inventory_transfer_employee_updater_fk,
	DROP FOREIGN KEY inventory_transfer_warehouse_from_fk,
	DROP FOREIGN KEY inventory_transfer_warehouse_to_fk,
    DROP INDEX inventory_transfer_warehouse_from_fk_idx,
	DROP INDEX inventory_transfer_warehouse_to_fk_idx,
    DROP INDEX inventory_transfer_employee_creator_fk_idx,
    DROP INDEX inventory_transfer_employee_updater_fk_idx;

ALTER TABLE inventory_transfer 
	ADD COLUMN store INT(11) NOT NULL AFTER inventory_transfer_id,
	ADD COLUMN serial INT(11) NULL AFTER store,
	ADD INDEX inventory_transfer_from_idx (warehouse ASC),
	ADD INDEX inventory_transfer_to_idx (warehouse_to ASC),
	ADD INDEX inventory_transfer_creator_idx (creator ASC),
	ADD INDEX inventory_transfer_updater_idx (updater ASC),
	ADD INDEX inventory_transfer_store_idx (store ASC),
	ADD CONSTRAINT inventory_transfer_from_fk
		FOREIGN KEY (warehouse) REFERENCES warehouse (warehouse_id)
		ON DELETE NO ACTION ON UPDATE NO ACTION,
	ADD CONSTRAINT inventory_transfer_to_fk
		FOREIGN KEY (warehouse_to) REFERENCES warehouse (warehouse_id)
		ON DELETE NO ACTION ON UPDATE NO ACTION,
	ADD CONSTRAINT inventory_transfer_creator_fk
		FOREIGN KEY (creator) REFERENCES employee (employee_id)
		ON DELETE NO ACTION ON UPDATE NO ACTION,
	ADD CONSTRAINT inventory_transfer_updater_fk
		FOREIGN KEY (updater) REFERENCES employee (employee_id)
		ON DELETE NO ACTION ON UPDATE NO ACTION,
	ADD CONSTRAINT inventory_transfer_store_fk
		FOREIGN KEY (store) REFERENCES store (store_id)
		ON DELETE NO ACTION ON UPDATE NO ACTION;

UPDATE inventory_issue i,    warehouse w SET i.store = w.store WHERE w.warehouse_id = i.warehouse;
UPDATE inventory_receipt i,  warehouse w SET i.store = w.store WHERE w.warehouse_id = i.warehouse;
UPDATE inventory_transfer i, warehouse w SET i.store = w.store WHERE w.warehouse_id = i.warehouse;

SET @row_number:=0;
SET @store:=0;

UPDATE inventory_issue
SET serial=(@row_number := CASE WHEN @store=store THEN @row_number+1 ELSE 1 END),
	store = (@store := store)
ORDER BY store, inventory_issue_id;

SET @row_number:=0;
SET @store:=0;

UPDATE inventory_receipt
SET serial=(@row_number := CASE WHEN @store=store THEN @row_number+1 ELSE 1 END),
	store = (@store := store)
ORDER BY store, inventory_receipt_id;

SET @row_number:=0;
SET @store:=0;

UPDATE inventory_transfer
SET serial=(@row_number := CASE WHEN @store=store THEN @row_number+1 ELSE 1 END),
	store = (@store := store)
ORDER BY store, inventory_transfer_id;

ALTER TABLE inventory_receipt
	ADD UNIQUE INDEX inventory_receipt_store_serial_idx (store ASC, serial ASC);

ALTER TABLE inventory_issue
	ADD UNIQUE INDEX inventory_issue_store_serial_idx (store ASC, serial ASC);

ALTER TABLE inventory_transfer
	ADD UNIQUE INDEX inventory_transfer_store_serial_idx (store ASC, serial ASC);

SET SQL_MODE=@OLD_SQL_MODE;
SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS;
