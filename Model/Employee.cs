// 
// Employee.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.org>
//   Eduardo Nieto <enieto@mictlanix.org>
// 
// Copyright (C) 2011 Eddy Zavaleta, Mictlanix, and contributors.
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

namespace Mictlanix.BE.Model
{
    [ActiveRecord("employee", Lazy = true)]
    public class Employee : ActiveRecordLinqBase<Employee>
    {
        public Employee()
        {
        }

        [PrimaryKey(PrimaryKeyType.Identity, "employee_id")]
		public virtual int Id { get; set; }

        [Property("first_name")]
		[Display(Name = "FirstName", ResourceType = typeof(Resources))]
		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[StringLength(100, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public virtual string FirstName { get; set; }

        [Property("last_name")]
		[Display(Name = "LastName", ResourceType = typeof(Resources))]
		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		[StringLength(100, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public virtual string LastName { get; set; }

        [Display(Name = "Name", ResourceType = typeof(Resources))]
		public virtual string Name { get { return string.Format("{0} {1}", FirstName, LastName).Trim(); } }

        [Property]
		[DataType(DataType.Date)]
		[Display(Name = "Birthday", ResourceType = typeof(Resources))]
		public virtual DateTime? Birthday { get; set; }

        [Property("taxpayer_id")]
		[Display(Name = "TaxpayerId", ResourceType = typeof(Resources))]
		[StringLength(13, MinimumLength = 12, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public virtual string TaxpayerId { get; set; }

        [Property]
		[Display(Name = "Gender", ResourceType = typeof(Resources))]
		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		public virtual GenderEnum Gender { get; set; }

        [Property("personal_id")]
		[Display(Name = "PersonalId", ResourceType = typeof(Resources))]
		[StringLength(18, MinimumLength = 18, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
		public virtual string PersonalId { get; set; }

        [Property("start_job_date")]
		[DataType(DataType.Date)]
		[Display(Name = "StartJobDate", ResourceType = typeof(Resources))]
		[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		public virtual DateTime StartJobDate { get; set; }
		
        #region Override Base Methods

        public override string ToString()
        {
            return string.Format("{0}", Name);
        }

        public override bool Equals(object obj)
        {
            Employee other = obj as Employee;

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
