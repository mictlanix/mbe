
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
