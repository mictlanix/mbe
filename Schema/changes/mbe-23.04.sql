ALTER TABLE taxpayer_recipient
ADD `postal_code` varchar(5) DEFAULT NULL,
ADD `regime` varchar(3) DEFAULT '000';

ALTER TABLE fiscal_document
ADD `cancellation_reason` varchar(250) DEFAULT NULL,
ADD `cancellation_substitution` varchar(250) DEFAULT NULL,
ADD `taxpayer_regime` varchar(3) DEFAULT NULL,
ADD `taxpayer_postal_code` varchar(5) DEFAULT NULL,
ADD `rfc_pac` varchar(13) DEFAULT NULL,
ADD `taxpayer_regime_name` varchar(250) DEFAULT NULL;

INSERT INTO sat_tax_regime (`sat_tax_regime_id`, `description`) VALUES ('000', 'No capturado');

CREATE TABLE sat_reason_cancellation (
    sat_reason_cancellation_id VARCHAR(2) NOT NULL,
    description VARCHAR(100),
    PRIMARY KEY (sat_reason_cancellation_id)
);
INSERT INTO sat_reason_cancellation (sat_reason_cancellation_id, description)
VALUES ('01', 'Comprobantes emitidos con errores con relación.'),
       ('02', 'Comprobantes emitidos con errores sin relación.'),
       ('03', 'No se llevó a cabo la operación.'),
       ('04', 'Operación nominativa relacionada en una factura global.');
