using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mictlanix.BE.Model
{
    [ActiveRecord("time_clock", Lazy = true)]
    public class TimeClock:ActiveRecordLinqBase<TimeClock>
    {
        [PrimaryKey(PrimaryKeyType.Identity, "time_clock_id")]
        public virtual int Id { get; set; }

        //[Property("address")]
        //public virtual 
    }
}
