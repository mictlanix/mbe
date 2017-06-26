using System;
using System.ComponentModel.DataAnnotations;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Mictlanix.BE.Model
{
	[ActiveRecord("checks", Lazy = true)]
    public class Check: ActiveRecordLinqBase<Check>
    {
        [PrimaryKey(PrimaryKeyType.Identity, "check_id")]
        public virtual int Id { get; set; }

        [Property("time") ]
        [DataType(DataType.DateTime)]
        [Display(Name ="Check")]
        public virtual DateTime Time { get; set; }

        [Property( "enroll_number")]
        public virtual int Enroll_Number { get; set; }
        
        [Property("status")]
        public virtual string Status { get; set; }

        #region Override Base Methods


        public override bool Equals(object obj)
        {
            Check other = obj as Check;

            if (other == null)
                return false;

            if (Id == 0 && other.Id == 0)
                return (object)this == other;
            else
                return Id == other.Id;
        }

        public override int GetHashCode()
        {
            if (Id == 0)
                return base.GetHashCode();

            return string.Format("{0}#{1}", GetType().FullName, Id).GetHashCode();
        }

        #endregion
    }
}
