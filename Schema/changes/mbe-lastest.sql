
UPDATE taxpayer_batch  SET type = 100 WHERE type = 1;
UPDATE fiscal_document SET type = 100 WHERE type = 1;

ALTER TABLE `fiscal_document_detail` 
  ADD COLUMN `comment` VARCHAR(500) NULL;
