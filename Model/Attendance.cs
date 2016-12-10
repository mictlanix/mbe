using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
namespace Mictlanix.BE.Model
{
    [ActiveRecord("attendance")]
    public class Attendance
    {

        /*
    CREATE TABLE `attendance` (
	`attendace_id` INT(11) NOT NULL AUTO_INCREMENT,
	`enroll_number` INT(11) NOT NULL,
	`time` DATETIME NOT NULL,
	`status` VARCHAR(3) NOT NULL,
	PRIMARY KEY (`attendace_id`),
	INDEX `attendance_employee_idx` (`enroll_number`),
	CONSTRAINT `FK_attendance_employee` FOREIGN KEY (`enroll_number`) REFERENCES `employee` (`enroll_number`)
    ) */


    }
}
