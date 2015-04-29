UPDATE lot_serial_tracking l
	INNER JOIN sales_order t ON l.source = 1 AND l.reference = t.sales_order_id
SET l.date = modification_time
WHERE lot_serial_tracking_id > 267696 AND l.date > modification_time;

UPDATE lot_serial_tracking l
	INNER JOIN customer_refund t ON l.source = 2 AND l.reference = t.customer_refund_id
SET l.date = modification_time
WHERE lot_serial_tracking_id > 267696 AND l.date > modification_time;

UPDATE lot_serial_tracking l
	INNER JOIN inventory_issue t ON l.source = 3 AND l.reference = t.inventory_issue_id
SET l.date = modification_time
WHERE lot_serial_tracking_id > 267696 AND date > modification_time;

UPDATE lot_serial_tracking l
	INNER JOIN inventory_receipt t ON l.source = 4 AND l.reference = t.inventory_receipt_id
SET l.date = modification_time
WHERE lot_serial_tracking_id > 267696 AND l.date > modification_time;

UPDATE lot_serial_tracking l
	INNER JOIN inventory_transfer t ON l.source = 5 AND l.reference = t.inventory_transfer_id
SET l.date = modification_time
WHERE lot_serial_tracking_id > 267696 AND l.date > modification_time;

UPDATE lot_serial_tracking l
	INNER JOIN inventory_transfer t ON l.source = 5 AND l.reference = t.inventory_transfer_id
SET l.date = modification_time
WHERE lot_serial_tracking_id > 267696 AND l.date > modification_time;

UPDATE lot_serial_tracking l
	INNER JOIN purchase_order t ON l.source = 6 AND l.reference = t.purchase_order_id
SET l.date = modification_time
WHERE lot_serial_tracking_id > 267696 AND l.date > modification_time;
