using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Mictlanix.BE.Model{
    [ActiveRecord("employee_schedule")]
    public class EmployeeSchedule : ActiveRecordLinqBase<EmployeeSchedule> 
    {
        [PrimaryKey(PrimaryKeyType.Identity, "id")]
        public int Id { get; set; }



        /*
    CREATE TABLE `employee_schedule` (
	`id` INT(11) UNSIGNED NOT NULL AUTO_INCREMENT,
	`employee` INT(11) NOT NULL,
	`day` TINYINT(1) NULL DEFAULT NULL,
	`start` TIME NULL DEFAULT NULL,
	`end` TIME NULL DEFAULT NULL,
	`date` DATETIME NULL DEFAULT CURRENT_TIMESTAMP,
	PRIMARY KEY (`id`),
	INDEX `FK_schedule_employee` (`employee`),
	CONSTRAINT `FK_schedule_employee` FOREIGN KEY (`employee`) REFERENCES `employee` (`employee_id`)
)
         */
    }
}
