using Castle.ActiveRecord;
using Mictlanix.BE.Web.Models;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using Mictlanix.BE.Model;


namespace Mictlanix.BE.Web.Models
{
    public class EmployeeAttendanceDBRecorder
    {

        public static void Recorder(Employee Employee, DateTime FirstDay, DateTime LastDay)
        {

            List<EmployeeSchedule> Schedules = EmployeeSchedule.Queryable.Where(x => x.Employee.Id == Employee.Id).ToList();

            //Esta consulta trae la fecha más antigua y la más nueva en los chequeos de un empleado dentro del intervalo de tiempo en los parámetros del método 
            //para crear entradas en la tabla attendance. No tiene caso crear entradas de un empleado cuando éste aún no checaba

            List<Check> Checks = Check.Queryable.Where(x => x.Enroll_Number == Employee.Enroll_Number && x.Time >= FirstDay && x.Time < LastDay.AddDays(1).AddMilliseconds(-1)).ToList();
            List<Attendance> Attendances = Attendance.Queryable.Where(x => x.Employee == Employee && x.Date >=FirstDay && x.Date <=LastDay).ToList();
            

            int Days = LastDay.Subtract(FirstDay).Days;

            for (int i = 0; i < Days; i++) {
                var Schedule = Schedules.Where(x => x.Day == (int)FirstDay.AddDays(i).DayOfWeek).FirstOrDefault();

                AttendanceDayRecorder.Recorder(Employee, FirstDay.AddDays(i), Schedule, Checks, Attendances.Where(x => x.Date == FirstDay.AddDays(i)).FirstOrDefault());

            }
        }

    }
}
