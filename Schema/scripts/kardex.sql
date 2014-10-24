
TRUNCATE TABLE lot_serial_tracking;

INSERT INTO lot_serial_tracking (warehouse, product, quantity, date, reference, source)
SELECT d.warehouse, d.product, -d.quantity, m.modification_time, m.sales_order_id, 1 
FROM sales_order m INNER JOIN sales_order_detail d ON m.sales_order_id = d.sales_order
WHERE m.completed = 1 AND m.cancelled = 0
UNION ALL
SELECT d2.warehouse, d.product, d.quantity, m.modification_time, m.customer_refund_id, 2
FROM customer_refund m INNER JOIN customer_refund_detail d ON m.customer_refund_id = d.customer_refund
					   INNER JOIN sales_order_detail d2 ON d2.sales_order_detail_id = d.sales_order_detail
WHERE m.completed = 1 AND m.cancelled = 0
UNION ALL
SELECT m.warehouse, d.product, -d.quantity, m.modification_time, m.inventory_issue_id, 3
FROM inventory_issue m INNER JOIN inventory_issue_detail d ON m.inventory_issue_id = d.issue
WHERE m.completed = 1 AND m.cancelled = 0
UNION ALL
SELECT m.warehouse, d.product, d.quantity, m.modification_time, m.inventory_receipt_id, 4
FROM inventory_receipt m INNER JOIN inventory_receipt_detail d ON m.inventory_receipt_id = d.receipt
WHERE m.completed = 1 AND m.cancelled = 0
UNION ALL
SELECT m.warehouse, d.product, -d.quantity, m.modification_time, m.inventory_transfer_id, 5
FROM inventory_transfer m INNER JOIN inventory_transfer_detail d ON m.inventory_transfer_id = d.transfer
WHERE m.completed = 1 AND m.cancelled = 0
UNION ALL
SELECT m.warehouse_to, d.product, d.quantity, m.modification_time, m.inventory_transfer_id, 5
FROM inventory_transfer m INNER JOIN inventory_transfer_detail d ON m.inventory_transfer_id = d.transfer
WHERE m.completed = 1 AND m.cancelled = 0;
