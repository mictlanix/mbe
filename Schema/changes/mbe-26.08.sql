-- Technical service module dropped (see mictlanix/mbe#37).
-- The five tech_service_* tables were created in mbe-14.08.sql; the module was
-- never finished and nothing reads it. The 36 rows of history are not retained.

-- SystemObjects 58 (Reports), 64 (Requests), 65 (Receipts) are retired in
-- Model/Constants/SystemObjects.cs. Nothing enumerates them any more, so these
-- rows would otherwise stay behind forever.
DELETE FROM `access_privilege` WHERE `object` IN (58, 64, 65);
