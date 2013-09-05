
ALTER TABLE `taxpayer_batch` 
  CHANGE COLUMN `approval_number` `approval_number` INT(11) NULL DEFAULT NULL ,
  CHANGE COLUMN `approval_year` `approval_year` INT(11) NULL DEFAULT NULL;

ALTER TABLE `fiscal_document` 
  DROP FOREIGN KEY `fiscal_document_recipient_fk`,
  DROP INDEX `fiscal_document_recipient_fk_idx`;

ALTER TABLE `fiscal_document` 
  ADD COLUMN `provider` INT(11) NOT NULL AFTER `version`;

ALTER TABLE `fiscal_document_detail` 
  CHANGE COLUMN `product_code` `product_code` VARCHAR(35) NULL DEFAULT NULL;


