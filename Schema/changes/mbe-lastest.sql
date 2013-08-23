
UPDATE employee SET birthday = '1900-01-01' WHERE birthday IS NULL;

ALTER TABLE employee
	ADD COLUMN `nickname` VARCHAR(50) NOT NULL AFTER `last_name`,
	ADD COLUMN `sales_person` TINYINT(1) NOT NULL AFTER `gender`,
	ADD COLUMN `active` TINYINT(1) NOT NULL AFTER `sales_person`,
	CHANGE COLUMN `gender` `gender` TINYINT(4) NOT NULL AFTER `nickname`,
	CHANGE COLUMN `birthday` `birthday` DATE NOT NULL;

UPDATE employee SET active = 1, sales_person = 1;
