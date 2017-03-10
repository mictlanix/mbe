using System;
using System.Linq;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Mictlanix.BE.Model
{
    [ActiveRecord("employee_schedule", Lazy = true)]
	public class EmployeeSchedule: ActiveRecordLinqBase<EmployeeSchedule>
	{
		[PrimaryKey(PrimaryKeyType.Identity, "id") ]
		public virtual int Id { get; set; }

        [BelongsTo("employee_id", Lazy = FetchWhen.Immediate)]
        [Display(Name = "Employee", ResourceType = typeof(Resources))]
        public virtual Employee Employee { get; set; }
        
        [Property("enroll_number")]
		public virtual int Enroll_Number { get; set; }

        [Property("day")]
        public virtual int Day { get; set; }

        [Property("start")]
        public virtual string Start { get; set; }

        [Property("end")]
        public virtual string End { get; set; }

        [Property("date")]
        [DataType(DataType.DateTime)]
        public virtual DateTime Date { get; set; }

        [Property("recess")]
		public virtual int? Recess { get; set; }

        public virtual TimeSpan? StartTS
        {
            get
            {
                TimeSpan o;
                if (TimeSpan.TryParse(Start, out o))
                {
                    return o;
                };
                return null;
            }
        }

        public virtual TimeSpan? EndTS
        {
            get
            {
                TimeSpan o;
                if (TimeSpan.TryParse(End, out o))
                {
                    return o;
                };
                return null;
            }
        }

        public virtual bool IsValidSchedule {
            get {
                return EndTS.HasValue && StartTS.HasValue;
            }
        }

        public virtual bool HasRecessTime {
            get {
                return Recess.HasValue && Recess.Value > 0;
            }
        }
    }
}

