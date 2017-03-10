using Mictlanix.BE.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Mictlanix.BE.Web.Models
{
    public class AttendanceSheetByEmployee
    {
        public Employee Employee { get; }

        public List<Attendance> AttendanceDays { get; }

        public AttendanceSheetByEmployee(Employee Employee, DateTime Start, DateTime End) {

            AttendanceDays = Attendance.Queryable.Where(x => x.Employee == Employee && x.Date >= Start && x.Date <= End).ToList();
            this.Employee = Employee;
        }

        

    }
}