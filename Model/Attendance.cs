using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace Mictlanix.BE.Model
{
    [ActiveRecord("attendance", Lazy = true)]
    public class Attendance : ActiveRecordLinqBase<Attendance>
    {
        [PrimaryKey(PrimaryKeyType.Identity, "id")]
        public virtual int Id { get; set; }

        [BelongsTo("employee", Lazy = FetchWhen.OnInvoke)]
        [Display(Name = "Employee", ResourceType = typeof(Resources))]
        public virtual Employee Employee { get; set; }

        [Property("date")]
        public virtual DateTime Date { get; set; }

        [Property("shift_in")]
        public virtual string ShiftIn { get; set; }

        [Property("shift_out")]
        public virtual string ShiftOut { get; set; }

        [Property("first_checkin")]
        public virtual DateTime? FirstCheckin { get; set; }

        [Property("first_checkout")]
        public virtual DateTime? FirstCheckout { get; set; }

        [Property("last_checkin")]
        public virtual DateTime? LastCheckin { get; set; }

        [Property("last_checkout")]
        public virtual DateTime? LastCheckout { get; set; }

        [Property("modified")]
        public virtual DateTime? Modified { get; set; }

        [Property("comment")]
        public virtual string Comment { get; set; }

        public virtual bool IsPresent
        {
            get
            {
                return (FirstCheckin.HasValue
                       || FirstCheckout.HasValue
                       || LastCheckin.HasValue
                       || LastCheckout.HasValue) && hasSchedule;
            }
        }

        public virtual bool hasSchedule
        {
            get
            {
                return (ShiftInTS.HasValue && ShiftOutTS.HasValue && ShiftInTS.Value != ShiftOutTS.Value);
            }
        }

        public virtual TimeSpan? ShiftOutTS
        {
            get
            {
                TimeSpan o;
                if (TimeSpan.TryParse(ShiftOut, out o))
                {
                    return o;
                };
                return null;
            }
        }

        public virtual TimeSpan? ShiftInTS
        {
            get
            {
                TimeSpan i;
                if (TimeSpan.TryParse(ShiftIn, out i))
                {
                    return i;
                };
                return null;
            }
        }

        public virtual bool IsCompleteWorkDay
        {
            get
            {
                if (!hasSchedule)
                {
                    return false;
                }

                if (ShiftOutTS > ShiftInTS)
                {
                    return ShiftOutTS.Value.Add(-ShiftInTS.Value).TotalHours > 6;
                }
                else
                {
                    return new TimeSpan(24, 0, 0).Subtract(ShiftInTS.Value).Add(ShiftOutTS.Value).Hours > 6;
                }

            }
        }

        public virtual TimeSpan DelayedTime
        {
            get
            {
                TimeSpan TimeDelayed = TimeSpan.Zero;
                if (hasSchedule)
                {
                    if (FirstCheckin.HasValue)
                    {
                        var TimeDelayedInFirstCheckIn = FirstCheckin.Value.TimeOfDay.Subtract(ShiftInTS.Value);
                        if (TimeDelayedInFirstCheckIn.TotalMinutes > int.Parse(Resources.DelayToleranceMinutes))
                        {
                            TimeDelayed = TimeDelayed.Add(TimeDelayedInFirstCheckIn > TimeSpan.Zero ? TimeDelayedInFirstCheckIn : TimeSpan.Zero);
                        }
                    }

                    if (FirstCheckout.HasValue && LastCheckin.HasValue)
                    {
                        var Schedule = EmployeeSchedule.Queryable.Where(x => x.Employee == Employee && x.Day == (int)Date.DayOfWeek).FirstOrDefault();

                        var RecessTime = TimeSpan.FromMinutes(Schedule.Recess.Value);
                        //Obtener los minutos de comida del empleado para el día actual
                        var TimeDelayedInFirstCheckOut = LastCheckin.Value.TimeOfDay.Add(-FirstCheckout.Value.TimeOfDay);
                        if (TimeDelayedInFirstCheckOut > TimeSpan.Zero)
                        {
                            TimeDelayed = TimeDelayed.Add(TimeDelayedInFirstCheckOut);
                        }

                    }
                }

                return TimeDelayed;
            }
        }

        public virtual TimeSpan OverTime
        {

            get
            {
                if (hasSchedule && LastCheckout.HasValue)
                {
                    var ExpectedEndWorkTime = Date.Date.Add(ShiftOutTS.Value);
                    if (ShiftInTS > ShiftOutTS)
                    {
                        ExpectedEndWorkTime = ExpectedEndWorkTime.AddDays(1);
                    }
                    return LastCheckout.Value.Subtract(ExpectedEndWorkTime);
                }
                return TimeSpan.Zero;
            }
        }

        public virtual bool IsMissingAnyCheck
        {
            get
            {
                if (!hasSchedule)
                {
                    return false;
                }

                if (!IsCompleteWorkDay)
                {
                    return (FirstCheckin.HasValue && !LastCheckout.HasValue)
                            || (!FirstCheckin.HasValue && LastCheckout.HasValue);

                }

                return (FirstCheckin.HasValue && !LastCheckout.HasValue)
                        || (!FirstCheckin.HasValue && LastCheckout.HasValue)
                        || (FirstCheckout.HasValue && !LastCheckin.HasValue)
                        || (!FirstCheckout.HasValue && LastCheckin.HasValue);
            }
        }

        public virtual bool IsDelayed
        {
            get
            {
                return DelayedTime.TotalMinutes > (int.Parse(Resources.DelayToleranceMinutes));
            }
        }

        public virtual bool IsLeavingBeforeEndWorkTime
        {
            get
            {
                if (!IsPresent)
                {
                    return false;
                }

                if (LastCheckout.HasValue && hasSchedule)
                {
                    return LastCheckout.Value.TimeOfDay < ShiftOutTS;
                }
                return false;
            }
        }

    }
}

