

start transaction;
ALTER TABLE `taxpayer_certificate` CHANGE COLUMN `taxpayer_certificate_id` `taxpayer_certificate_id` CHAR(20) NOT NULL;
ALTER TABLE `fiscal_document` CHANGE COLUMN `issuer_certificate_number` `issuer_certificate_number` CHAR(20) NULL DEFAULT NULL;
commit;

ALTER TABLE `taxpayer_batch` 
	ADD COLUMN `template` VARCHAR(25) NOT NULL AFTER `approval_year`;
