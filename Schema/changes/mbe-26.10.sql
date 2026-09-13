-- sales_order_payment.cancelled dropped (see mictlanix/mbe#55, companion to
-- mictlanix/mbe-api#212).
--
-- The column never appeared in a change file -- it was applied to production
-- out-of-band and only ever landed in Schema/model/mbe_schema.sql, alongside
-- `applier`, `date` and `confirmed`, which were formalised later in mbe-25.08.sql.
--
-- In this repository it is dead weight: `CustomerPayment.Allocated` was the only
-- reader and nothing ever wrote it. The ~60 other sites that sum allocations --
-- SalesOrder.Balance, SalesOrder.Paid, Customer.Debt(), BalanceInCashDrawer(),
-- the accounts-receivable aging query and six report queries -- ignored it
-- entirely, so a flagged row was counted as live everywhere that matters. The
-- docs described an "Unapply" action built on it that was never implemented;
-- that text is corrected in the same commit.
--
-- WARNING -- check before applying. mbe-api DOES write this column: its
-- POST /customer-payments/{id}/applications/{id}/reverse sets cancelled = 1 to
-- reverse an application (FR-045). Any row already reversed that way silently
-- becomes live again once the column is gone, and its amount is counted back
-- into CustomerPayment.Allocated. Run this first and deal with the result:
--
--   SELECT sales_order_payment_id, sales_order, customer_payment, amount
--   FROM sales_order_payment WHERE cancelled = 1;
--
-- If that returns rows, do not apply this until mictlanix/mbe-api#212 is
-- resolved -- those reversals have nowhere to go.
ALTER TABLE `sales_order_payment`
	DROP COLUMN `cancelled`;

-- ATENCIÓN EN CAMPO participation rates backfilled for every commission label.
--
-- Companion to the CommissionsBySalesPerson fix in ReportsController.cs, which
-- corrects who each participation type belongs to on a cross-agent order (one
-- where the customer's assigned agent is not the salesperson on the order):
--
--   CLIENTE VITALICIO (1)  the customer's agent, only when they also sold it
--   CANALIZACIÓN (2)       the salesperson who processed the order
--   ATENCIÓN EN CAMPO (3)  the customer's agent, when someone else processed it
--
-- Participations 2 and 3 were previously joined to the opposite agent, so the
-- customer's agent collected the full CLIENTE VITALICIO rate on orders they did
-- not sell. With that corrected they drop to their ATENCIÓN EN CAMPO share --
-- but only 4 such rows exist, all for CONCRETOS (commission_salesperson_id
-- 392-395), so every other label would resolve through IFNULL(...,0) to a 0%
-- line earning nothing.
--
-- This backfills participation 3 at 0.500000 for every (salesperson, commission)
-- pair that already has a CLIENTE VITALICIO row, which is what "every label the
-- agent handles" means in this table. 60 rows: 12 each for salespersons 17, 33,
-- 54, 77 and 78. The four CONCRETOS rows already present are left alone, and
-- BRIAN (33) gets no CONCRETOS row because he has no CLIENTE VITALICIO row for
-- it -- his CONCRETOS participation is CANALIZACIÓN (id 187), which is correct
-- and unchanged.
--
-- Participation 2 is deliberately NOT backfilled. An agent who processes another
-- agent's order earns a canalización share only where one is configured, which
-- today is BRIAN on CONCRETOS alone. Salespersons with no commission_salesperson
-- rows at all -- VICTORIA (76) is one -- continue to collect nothing.
--
-- NOTE -- this changes what the report pays out. Cross-agent lines that paid the
-- customer's agent 100% of the label rate now pay 50%. commissions_history holds
-- 155 such lines already booked at CLIENTE VITALICIO 1.0000; they are historical
-- records of what was paid under the old rule and are not rewritten here.
INSERT INTO `commission_salesperson`
	(`salesperson`, `commission`, `commission_participation`, `participation_rate`)
SELECT cs.`salesperson`, cs.`commission`, 3, 0.500000
FROM `commission_salesperson` cs
WHERE cs.`commission_participation` = 1
	AND NOT EXISTS (
		SELECT 1 FROM `commission_salesperson` x
		WHERE x.`salesperson` = cs.`salesperson`
			AND x.`commission` = cs.`commission`
			AND x.`commission_participation` = 3
	);
