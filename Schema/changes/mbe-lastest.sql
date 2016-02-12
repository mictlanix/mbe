
CREATE TABLE `time_clock` (
  `time_clock_id` char(36) COLLATE utf8_unicode_ci NOT NULL,
  `address` varchar(16) COLLATE utf8_unicode_ci NOT NULL,
  `last_sync` datetime NOT NULL,
  `sync` tinyint(1) NOT NULL,
  PRIMARY KEY (`time_clock_id`)
) ENGINE=InnoDB;

CREATE TABLE `time_clock_record` (
  `time_clock_record_id` char(36) COLLATE utf8_unicode_ci NOT NULL,
  `enroll_number` int(11) NOT NULL,
  `datetime` datetime NOT NULL,
  `type` int(11) NOT NULL,
  `employee` int(11) DEFAULT NULL,
  PRIMARY KEY (`time_clock_record_id`),
  KEY `time_clock_record_employee_idx` (`employee`),
  CONSTRAINT `time_clock_record_employee_fk` FOREIGN KEY (`employee`) REFERENCES `employee` (`employee_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB;
