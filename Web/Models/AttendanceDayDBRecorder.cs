using System;
using System.Linq;
using System.Collections.Generic;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Models
{
    public class AttendanceDayRecorder
    {

        public static void Recorder(Employee Employee, DateTime date, EmployeeSchedule Schedule, List<Check> ChecksByEmployee, Attendance Attendance)
        {
            DateTime DateAttendance = date.Date;
            List<Check> Checks = new List<Check>();
  
            TimeSpan? ShiftIn = null;
            TimeSpan? ShiftOut = null;
            List<Check> ChecksOnDay = new List<Check>();


            //Validación hecha en caso de que haya alguna asistencia con su schedule para dicho día
            if (Schedule.IsValidSchedule)
            {
                ShiftIn = TimeSpan.Parse(Schedule.Start);
                ShiftOut = TimeSpan.Parse(Schedule.End);

                DateTime ExpectedScheduleDayStart = DateAttendance.Date.Add(ShiftIn.Value);
                DateTime ExpectedScheduleDayEnd = DateAttendance.Date.Add(ShiftOut.Value);

                //Si la hora de entrada es superior a la hora de salida significa un horario nocturno y debemos buscar
                //el chequeo de salida un día después del de entrada
                if (ShiftOut < ShiftIn)
                {
                    ExpectedScheduleDayEnd = ExpectedScheduleDayEnd.AddDays(1);
                }

                //Hora esperada de final de labores con hasta 3.5 hrs después por posibles horas extras
                //Hora esperada de inicio a labores hasta con 1.5 hrs de anticipo
                ChecksOnDay = ChecksByEmployee.Where(x => x.Enroll_Number == Employee.Enroll_Number
                                            && x.Time >= ExpectedScheduleDayStart.AddHours(-1.5)
                                            && x.Time <= ExpectedScheduleDayEnd.AddHours(3.5)
                                                ).OrderBy(x => x.Time).ToList();
            }
            else {
                //Solamente se buscan los chequeos del día
                ChecksOnDay = ChecksByEmployee.Where(x => x.Enroll_Number == Employee.Enroll_Number && x.Time.Date == DateAttendance.Date).ToList();
            }


            //Limpiar chequeos basura
            if (ChecksOnDay.Count > 0)
            {

                DateTime last = ChecksOnDay[0].Time;

                for (int i = 1; i < ChecksOnDay.Count;)
                {
                    if ((ChecksOnDay[i].Time - last).TotalMinutes < 10)
                    {
                        ChecksOnDay.RemoveAt(i);
                    }
                    else
                    {
                        last = ChecksOnDay[i++].Time;
                    }
                }
            }


            Attendance item = new Attendance()
            {
                FirstCheckin = ChecksOnDay.Count > 0 ? ChecksOnDay.First().Time : (DateTime?)null,
                FirstCheckout = ChecksOnDay.Count > 2 ? ChecksOnDay[1].Time : (DateTime?)null,
                LastCheckin = ChecksOnDay.Count > 3 ? ChecksOnDay[2].Time : (DateTime?)null,
                LastCheckout = ChecksOnDay.Count > 1 ? ChecksOnDay.Last().Time : (DateTime?)null,
                Date = DateAttendance,
                Employee = Employee,
                ShiftIn = ShiftIn.HasValue?ShiftIn.ToString():null,
                ShiftOut = ShiftOut.HasValue?ShiftOut.ToString():null,
            };

            if (Attendance == null)
            {
                item.CreateAndFlush();
            }
            else if (Attendance.Modified == null)
            {
                Attendance.FirstCheckin = item.FirstCheckin;
                Attendance.LastCheckin = item.LastCheckin;
                Attendance.FirstCheckout = item.FirstCheckout;
                Attendance.LastCheckout = item.LastCheckout;
                Attendance.Modified = DateTime.Now;

                Attendance.UpdateAndFlush();
            }

        }

    }
}
