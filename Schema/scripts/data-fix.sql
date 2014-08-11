
UPDATE product
SET unit_of_measurement = 'PIEZA'
WHERE unit_of_measurement IN ('PZA', 'PZAS', 'piezas', 'PZS', 'PZ', 'PZA2', 'ZA', '?ZA', 'P', 'PZA370');

UPDATE product
SET unit_of_measurement = 'CIENTO'
WHERE unit_of_measurement IN ('CIENTOQA', 'CIENTO|');

UPDATE product
SET unit_of_measurement = 'BULTO'
WHERE unit_of_measurement IN ('BULTP');

UPDATE product
SET unit_of_measurement = 'KILOGRAMO'
WHERE unit_of_measurement IN ('KG');

UPDATE product
SET unit_of_measurement = 'LITRO'
WHERE unit_of_measurement IN ('LITROS');

UPDATE product
SET unit_of_measurement = 'METRO'
WHERE unit_of_measurement IN ('MTS');

SELECT DISTINCT unit_of_measurement
FROM product
ORDER BY 1;

SELECT fiscal_document_xml_id
FROM fiscal_document_xml x
INNER JOIN fiscal_document f ON x.fiscal_document_xml_id = f.fiscal_document_id
WHERE version = 3.2 AND LOCATE('tfd:TimbreFiscalDigital version', data) <> 0

UPDATE fiscal_document_xml x  
INNER JOIN fiscal_document f  ON x.fiscal_document_xml_id = f.fiscal_document_id 
SET  x.data = REPLACE(x.data, 'tfd:TimbreFiscalDigital version', 'tfd:TimbreFiscalDigital xsi:schemaLocation="http://www.sat.gob.mx/TimbreFiscalDigital http://www.sat.gob.mx/sitio_internet/TimbreFiscalDigital/TimbreFiscalDigital.xsd" version') 
WHERE f.version = 3.2 AND LOCATE('tfd:TimbreFiscalDigital version', x.data) <> 0
