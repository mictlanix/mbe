-- DELETE FROM customer;
-- DELETE FROM price_list;

INSERT INTO price_list (`price_list_id`,`name`, `high_profit_margin`, `low_profit_margin`)
VALUES (1,'General',0,0),(999999,'Costo',0,0);

UPDATE price_list SET price_list_id = 0 WHERE price_list_id = 999999;

ALTER TABLE price_list AUTO_INCREMENT=2;

INSERT INTO customer (customer_id, code, name, credit_limit, credit_days, price_list)
VALUES (1,'PG','Público en General',0,0,1);
