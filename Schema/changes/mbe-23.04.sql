ALTER TABLE taxpayer_recipient
    ADD `postal_code` varchar(5) COLLATE utf8_unicode_ci DEFAULT NULL,
    ADD `regime` varchar(3) COLLATE utf8_unicode_ci DEFAULT NULL;

ALTER TABLE fiscal_document
    ADD `cancellation_reason` varchar(250) COLLATE utf8_unicode_ci DEFAULT NULL,
    ADD `cancellation_substitution` varchar(250) COLLATE utf8_unicode_ci DEFAULT NULL,
    ADD `taxpayer_regime` varchar(3) COLLATE utf8_unicode_ci DEFAULT NULL,
    ADD `taxpayer_postal_code` varchar(5) COLLATE utf8_unicode_ci DEFAULT NULL,
    ADD `rfc_pac` varchar(13) COLLATE utf8_unicode_ci DEFAULT NULL,
    ADD `taxpayer_regime_name` varchar(250) COLLATE utf8_unicode_ci DEFAULT NULL;

CREATE TABLE sat_reason_cancellation (
    sat_reason_cancellation_id VARCHAR(2) COLLATE utf8_unicode_ci NOT NULL,
    description VARCHAR(100) COLLATE utf8_unicode_ci,
    PRIMARY KEY (sat_reason_cancellation_id)
) DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

INSERT INTO sat_reason_cancellation (sat_reason_cancellation_id, description)
VALUES ('01', 'Comprobantes emitidos con errores con relación.'),
       ('02', 'Comprobantes emitidos con errores sin relación.'),
       ('03', 'No se llevó a cabo la operación.'),
       ('04', 'Operación nominativa relacionada en una factura global.');

ALTER TABLE `sat_cfdi_usage` 
	CHANGE COLUMN `sat_cfdi_usage_id` `sat_cfdi_usage_id` VARCHAR(4) NOT NULL;

INSERT INTO sat_cfdi_usage (sat_cfdi_usage_id, description)
VALUES ('S01', 'Sin Obligaciones Fiscales'),
       ('CP01', 'Pagos'),
       ('CN01', 'Pagos Nomina');
