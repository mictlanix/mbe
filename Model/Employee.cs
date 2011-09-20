using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Business.Essentials.Model
{
    [ActiveRecord("employee")]
    public class Employee : ActiveRecordLinqBase<Employee>
    {
        [PrimaryKey(PrimaryKeyType.Identity, "employee_id")]
        public int Id { get; set; }

        [Property("first_name")]
        [Display(Name = "FirstName", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(100, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string FirstName { get; set; }

        [Property("last_name")]
        [Display(Name = "LastName", ResourceType = typeof(Resources))]
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [StringLength(100, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string LastName { get; set; }

        [Property]
        [DataType(DataType.Date)]
        [Display(Name = "Birthday", ResourceType = typeof(Resources))]
        public DateTime? Birthday { get; set; }

        [Property("taxpayer_id")]
        [Display(Name = "TaxpayerId", ResourceType = typeof(Resources))]
        [StringLength(13, MinimumLength = 12, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string TaxpayerId { get; set; }

        [Property]
        [Display(Name = "Gender", ResourceType = typeof(Resources))]
        public int Gender { get; set; }

        [Property("personal_id")]
        [Display(Name = "PersonalId", ResourceType = typeof(Resources))]
        [StringLength(18, MinimumLength = 18, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string PersonalId { get; set; }

        [Property("start_job_date")]
        [DataType(DataType.Date)]
        [Display(Name = "StartJobDate", ResourceType = typeof(Resources))]
        public DateTime? StartJobDate { get; set; }
    }
}
