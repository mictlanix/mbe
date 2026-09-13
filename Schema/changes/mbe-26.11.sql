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
