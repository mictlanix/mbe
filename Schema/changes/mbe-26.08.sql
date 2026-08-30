-- Abandoned modules dropped (see mictlanix/mbe#37).
--
-- Technical service: the five tech_service_* tables were created in
-- mbe-14.08.sql. The module was never finished and nothing reads it. Its 36
-- rows of history are not retained.
--
-- Vehicle service orders: vehicle_service_order and service_order_detail were
-- created in mbe-25.08.sql and never used once -- both tables are empty in
-- mbe_dev and mbe_demo. vehicle and vehicle_operator are NOT part of this;
-- delivery itineraries use them and they stay.

-- SystemObjects 58 (TechnicalServiceReports), 64 (TechnicalServiceRequests),
-- 65 (TechnicalServiceReceipts) and 90 (VehicleServiceOrders) are retired in
-- Model/Constants/SystemObjects.cs. Nothing enumerates them any more, so these
-- rows would otherwise stay behind forever. Objects 88 (Vehicle) and
-- 89 (VehicleOperators) are live and untouched.
DELETE FROM `access_privilege` WHERE `object` IN (58, 64, 65, 90);
