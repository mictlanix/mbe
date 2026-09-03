-- customer.shipping and customer.shipping_required_document dropped
-- (see mictlanix/mbe#40, companion to mictlanix/mbe-api#199).
--
-- Both columns were added in mbe-14.05.sql. The toggles no longer reflect how
-- the business operates: mbe-api retired them from every request and response
-- schema and mbe-ui removed the POS delivery-method gate that read `shipping`.
-- This repository was the last writer -- CustomersController.Edit -- and no
-- longer maps either column.
--
-- Both are TINYINT(1) NOT NULL with no default, so under STRICT_TRANS_TABLES an
-- insert omitting them fails with error 1364. That is the only reason mbe-api
-- still mapped them and wrote 0 on create; it drops that mapping once this runs.
--
-- The 899 rows with `shipping` = 1 and the 1,013 with
-- `shipping_required_document` = 1 are not retained.
ALTER TABLE `customer`
	DROP COLUMN `shipping`,
	DROP COLUMN `shipping_required_document`;
