DELETE FROM price_list;

INSERT INTO price_list (`price_list_id`,`name`, `high_profit_margin`, `low_profit_margin`)
VALUES (1,'Mostrador',0,0),(2,'Super Mostrador',0,0),(3,'Mayoreo',0,0),(4,'Especial',0,0);

INSERT INTO customer (customer_id, name, credit_limit, credit_days, price_list)
VALUES (1,'Público en General',0,0,1);

