// 
// Employee.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.org>
//   Eduardo Nieto <enieto@mictlanix.org>
// 
// Copyright (C) 2011 Eddy Zavaleta, Mictlanix (http://www.mictlanix.org)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;

namespace Business.Essentials.Model
{
    public enum GenderEnum : int
    {
        [Display(Name = "Male", ResourceType = typeof(Resources))]
        Male,
        [Display(Name = "Female", ResourceType = typeof(Resources))]
        Female
    }

    [ActiveRecord("employee")]
    public class Employee : ActiveRecordLinqBase<Employee>
    {
        public Employee()
        {
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", FirstName, LastName).Trim();
        }

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

        [Display(Name = "Name", ResourceType = typeof(Resources))]
        public string Name { get { return string.Format("{0} {1}", FirstName, LastName).Trim(); } }

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
        public GenderEnum Gender { get; set; }

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
