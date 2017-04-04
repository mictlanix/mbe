using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Mictlanix.BE.Model
{
    [ActiveRecord("expenses", Lazy = true)]
    public class Expense:ActiveRecordLinqBase<Expense>
    {
        [PrimaryKey(PrimaryKeyType.Identity, "expense_id")]
        public virtual int Id { get; set; }

        [Property("expense")]
        [Display(Name = "Name", ResourceType = typeof(Resources))]
        public virtual string Name { get; set; }

        [Property("comment")]
        public virtual string Comment { get; set; }
    }
}
