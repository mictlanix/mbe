using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Models;
using Mictlanix.BE.Web.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Mictlanix.BE.Web.Controllers
{
    [Authorize]
    public class HumanResourcesController : CustomController
    {
        // GET: HumanResources

        public ViewResult SynchronizeTimeClocks()
        {

            //getCheckClocks();
            

            var watch = System.Diagnostics.Stopwatch.StartNew();

            //List<EmployeeAttendanceDBRecorder> sheets = new List<EmployeeAttendanceDBRecorder>();

            DateTime StartDate = new DateTime(2015, 12, 01);
            DateTime EndDate = new DateTime(2016, 12, 15);
            

            List<Check> Checks = Check.Queryable.Where(x => x.Time >= StartDate && x.Time <= EndDate).ToList();
            List<int> enroll_numbers = Checks.Select(x => x.Enroll_Number).Distinct().ToList();

            var Employees = Employee.Queryable.Where(e => enroll_numbers.Contains(e.Enroll_Number.Value)).ToList();

            

            foreach (var Employee in Employees)
            {
                EmployeeAttendanceDBRecorder.Recorder(Employee, StartDate, EndDate);
            }

            watch.Stop();
            var elapsedMs = watch.Elapsed;


            return View("SynchronizeTimeclocks");

        }

        private void getCheckClocks()
        {
            throw new NotImplementedException();
        }

        public ViewResult AttendanceEmployees()
        {            
            return View("Attendance", new DateRange());
        }

        [HttpPost]
        public ActionResult AttendanceEmployees(string employee, DateTime StartDate, DateTime EndDate)
        {
            
            var Attendances = Attendance.Queryable.Where(x => x.Date >= StartDate && x.Date <= EndDate && x.Employee.IsActive).Select(x => x.Employee.Id).Distinct().ToList();
            int id_employee;
            var Sheets = new List<AttendanceSheetByEmployee>();

            

            if (int.TryParse(employee, out id_employee))
            {
                Sheets.Add(new AttendanceSheetByEmployee(Employee.Find(id_employee), StartDate, EndDate));
            }
            else {
                foreach (var i in Attendances)
                {
                    Sheets.Add(new AttendanceSheetByEmployee(Employee.Find(i), StartDate, EndDate));
                }
            }


            return PartialView("_Attendance", Sheets);
        }

        public ViewResult EmployeeSchedules() {

            return View("EmployeeSchedules");
        }

        [HttpPost]
        public ActionResult EmployeeSchedules(string employee) {

            int id;

            if (int.TryParse(employee, out id)) {
                return PartialView("_EmployeeSchedules", Employee.Queryable.Where(x => x.Id == id && x.IsActive).ToList());
            }

            return PartialView("_EmployeeSchedules", Employee.Queryable.Where(x => x.IsActive).OrderBy(x => x.Enroll_Number).ToList());
        }

        [HttpPost]
        public ActionResult SetFirstCheckIn(int id, TimeSpan value)
        {

            var Item = Attendance.Find(id);
            //if (value != null) {
                Item.FirstCheckin = Item.Date.Date.Add(value);
                Item.Modified = DateTime.Now;
                Item.UpdateAndFlush();
            //}

            return PartialView("_AttendanceEmployeeByDay", Item);
        }

        [HttpPost]
        public ActionResult SetFirstCheckOut(int id, TimeSpan value)
        {

            var Item = Attendance.Find(id);
            //if (value!=null)
            //{
                Item.FirstCheckout = Item.Date.Date.Add(value);
                if (value < TimeSpan.Parse(Item.ShiftIn))
                {
                    Item.FirstCheckout.Value.AddDays(1);
                }
                Item.Modified = DateTime.Now;
                Item.UpdateAndFlush();
            //}

            return PartialView("_AttendanceEmployeeByDay", Item);

        }

        [HttpPost]
        public ActionResult SetLastCheckIn(int id, TimeSpan value)
        {

            var Item = Attendance.Find(id);
            //if (value != null)
            //{
                Item.LastCheckin = Item.Date.Add(value);

                if (value < TimeSpan.Parse(Item.ShiftIn))
                {
                    Item.LastCheckin.Value.AddDays(1);
                }

                Item.Modified = DateTime.Now;
                Item.UpdateAndFlush();
            //}

            return PartialView("_AttendanceEmployeeByDay", Item);

        }

        [HttpPost]
        public ActionResult SetLastCheckOut(int id, TimeSpan value)
        {

            var Item = Attendance.Find(id);
            //if (value != null)
            //{
                Item.LastCheckout = Item.Date.Date.Add(value);

                if (value < TimeSpan.Parse(Item.ShiftIn)) {
                    Item.LastCheckout.Value.AddDays(1);
                }

                Item.Modified = DateTime.Now;
                Item.UpdateAndFlush();
            //}

            return PartialView("_AttendanceEmployeeByDay", Item);


        }

        public void Excel()
        {
            
            DateTime start = new DateTime(2016, 11, 1);
            DateTime end = new DateTime(2016, 11, 15);
            List<Attendance> Sheets = Attendance.Queryable.Where(x => x.Date >= start && x.Date <= end && x.Employee.IsActive).OrderBy(x => x.Employee.Id).OrderBy(x => x.Date).ToList();

            bool IsIncompleteReviewAttendances = Sheets.Where(x => x.IsMissingAnyCheck).Count() > 0;

            if (IsIncompleteReviewAttendances)
                throw new Exception("Error en asistencias");

            List<int> Employee_ids = Sheets.Select(x => x.Employee.Id).Distinct().ToList();

            var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Asistencia");
            var headers = new string[] { "Día", "Entrada", "Salida (C)", "Entrada (C)", "Salida",
                "(-)", "(+)", "T. Extra (min)", "T. Extra (hrs)", "Retardo", "Falta" };
            int row = 2;


            ws.Cells[1, 1].Value = "Reporte de Asistencia";
            using (var range = ws.Cells[1, 1, 1, 1])
            {
                range.Style.Font.Bold = true;
                range.Style.Font.Size = 22;
            }

            foreach (var Employee_id in Employee_ids)
            {
                row++;
                var Employee_sheet = Employee.Find(Employee_id);
                ws.Cells[row, 1].Value = Employee_sheet.Name;

                using (var range = ws.Cells[row, 1, row, 1])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Font.Size = 18;
                }

                row += 2;

                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cells[row, i + 1].Value = headers[i];
                }

                using (var range = ws.Cells[row, 1, row, headers.Length])
                {
                    range.Style.Font.Bold = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                row++;

                var AttendanceDays = Sheets.Where(x => x.Employee.Id == Employee_sheet.Id).ToList();

                foreach (var attendanceday in AttendanceDays)
                {
                    ws.Cells[row, 1].Value = attendanceday.Date;
                    if (attendanceday.FirstCheckin.HasValue && attendanceday.FirstCheckin.Value.TimeOfDay != TimeSpan.Zero)
                        ws.Cells[row, 2].Value = attendanceday.FirstCheckin.Value.TimeOfDay;
                    if (attendanceday.FirstCheckout.HasValue && attendanceday.FirstCheckout.Value.TimeOfDay != TimeSpan.Zero)
                        ws.Cells[row, 3].Value = attendanceday.FirstCheckout.Value.TimeOfDay;
                    if (attendanceday.LastCheckin.HasValue && attendanceday.LastCheckin.Value.TimeOfDay != TimeSpan.Zero)
                        ws.Cells[row, 4].Value = attendanceday.LastCheckin.Value.TimeOfDay;
                    if (attendanceday.LastCheckout.HasValue && attendanceday.LastCheckout.Value.TimeOfDay != TimeSpan.Zero)
                        ws.Cells[row, 5].Value = attendanceday.LastCheckout.Value.TimeOfDay;

                    ws.Cells[row, 6].Value = Math.Ceiling(attendanceday.DelayedTime.TotalMinutes);
                    ws.Cells[row, 7].Value = Math.Floor(attendanceday.OverTime.TotalMinutes);
                    ws.Cells[row, 8].FormulaR1C1 = "-RC[-2]+RC[-1]";
                    ws.Cells[row, 9].FormulaR1C1 = "ROUND(RC[-1]/60,2)";
                    ws.Cells[row, 10].Value = attendanceday.IsDelayed ? 1 : 0;
                    ws.Cells[row, 11].Value = !attendanceday.IsPresent && attendanceday.hasSchedule ? 1 : 0;

                    row++;
                }

                ws.Cells[row, 5].Value = "Total";
                ws.Cells[row, 6].FormulaR1C1 = "SUM(R[-" + AttendanceDays.Count + "]C:R[-1]C)";
                ws.Cells[row, 7].FormulaR1C1 = "SUM(R[-" + AttendanceDays.Count + "]C:R[-1]C)";
                ws.Cells[row, 8].FormulaR1C1 = "SUM(R[-" + AttendanceDays.Count + "]C:R[-1]C)";
                ws.Cells[row, 9].FormulaR1C1 = "ROUND(RC[-1]/60,2)";
                ws.Cells[row, 10].FormulaR1C1 = "SUM(R[-" + AttendanceDays.Count + "]C:R[-1]C)";
                ws.Cells[row, 11].FormulaR1C1 = "SUM(R[-" + AttendanceDays.Count + "]C:R[-1]C)";

                using (var range = ws.Cells[row, 1, row, headers.Length])
                {
                    range.Style.Font.Bold = true;
                }

                row++;

                //if (AttendanceDays.Sum(x => x.Checks.Count) == 0)
                //{
                //    continue;
                //}
            }

            using (var range = ws.Cells[1, 1, ws.Cells.Rows, 1])
            {
                range.Style.Numberformat.Format = "dddd dd MMM";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            using (var range = ws.Cells[1, 2, ws.Cells.Rows, 5])
            {
                range.Style.Numberformat.Format = "HH:mm";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            ws.Column(1).Width = 25;
            ws.Column(2).Width = 12;
            ws.Column(3).Width = 12;
            ws.Column(4).Width = 12;
            ws.Column(5).Width = 12;
            ws.Column(6).Width = 12;
            ws.Column(7).Width = 12;
            ws.Column(8).Width = 12;
            ws.Column(9).Width = 12;
            ws.Column(10).Width = 12;

            package.Workbook.Properties.Title = "Attempts";
            this.Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            this.Response.AddHeader(
                      "content-disposition",
                      string.Format("attachment;  filename={0}", "Asistencia " + start.Date + " - " + end.Date + ".xlsx"));
            this.Response.BinaryWrite(package.GetAsByteArray());


        }

        [HttpPost]
        public ActionResult SetStart(int id, TimeSpan value) {

            var item = EmployeeSchedule.Find(id);
            item.Start = value.ToString();

            using (var scope = new TransactionScope()) {
                item.UpdateAndFlush();
            }

            return Json(new { id = id, value = value.ToString() });
        }

        [HttpPost]
        public ActionResult SetEnd(int id, TimeSpan value)
        {

            var item = EmployeeSchedule.Find(id);
            item.End = value.ToString();

            using (var scope = new TransactionScope())
            {
                item.UpdateAndFlush();
            }

            return Json( new { id = id, value = value.ToString()});
            
        }

        [HttpPost]
        public ActionResult SetRecess(int id, int value) {

            var item = EmployeeSchedule.Find(id);
            item.Recess = value;

            using (var scope = new TransactionScope())
            {
                item.UpdateAndFlush();
            }

            return Json(new { id = id, value = value.ToString() });
        }

        public ActionResult New() {

            return PartialView();
        }

        [HttpPost]
        public ActionResult SetEnrollNumber(int id, int value) {

            //var emp = Employee.Find(id);
            bool AlreadyExistEnrollNumber = Employee.Queryable.Where(x => x.Enroll_Number == value).Count() > 0;

            if (AlreadyExistEnrollNumber) {
                Response.StatusCode = 400;
                return Content(Resources.Duplicate);
            }

            return Json(new { });
        }

    }
}