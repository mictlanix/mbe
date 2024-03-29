// 
// User.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2011-2013 Eddy Zavaleta, Mictlanix, and contributors.
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
using System.ComponentModel.DataAnnotations;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Mictlanix.BE.Model {
	[ActiveRecord ("user")]
	public class User : ActiveRecordLinqBase<User> {
		IList<AccessPrivilege> privileges = new List<AccessPrivilege> ();

		[PrimaryKey ("user_id")]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[RegularExpression (@"^[0-9a-zA-Z]+$", ErrorMessageResourceName = "Validation_UserName", ErrorMessageResourceType = typeof (Resources))]
		[StringLength (20, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "UserName", ResourceType = typeof (Resources))]
		public string UserName { get; set; }

		[Property ()]
		[DataType (DataType.Password)]
		[Display (Name = "Password", ResourceType = typeof (Resources))]
		[StringLength (40, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string Password { get; set; }

		[Property ("email")]
		[DataType (DataType.EmailAddress)]
		[EmailAddress (ErrorMessageResourceName = "Validation_Email", ErrorMessageResourceType = typeof (Resources), ErrorMessage = null)]
		[Display (Name = "Email", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[StringLength (250, MinimumLength = 6, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof (Resources))]
		public string Email { get; set; }

		[Required (ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "Employee", ResourceType = typeof (Resources))]
		[UIHint ("EmployeeSelector")]
		public int EmployeeId { get; set; }

		[BelongsTo ("employee", Fetch = FetchEnum.Join)]
		[Display (Name = "Employee", ResourceType = typeof (Resources))]
		public virtual Employee Employee { get; set; }

		[BelongsTo ("user_id", Fetch = FetchEnum.Join, Insert = false, Update = true, NotFoundBehaviour = NotFoundBehaviour.Ignore)]
		[Display (Name = "UserSettings", ResourceType = typeof (Resources))]
		public virtual UserSettings UserSettings { get; set; }

		[Property ("administrator")]
		[Display (Name = "Administrator", ResourceType = typeof (Resources))]
		public bool IsAdministrator { get; set; }

		[HasMany (typeof (AccessPrivilege), Table = "access_privilege", ColumnKey = "user")]
		public IList<AccessPrivilege> Privileges {
			get { return privileges; }
			set { privileges = value; }
		}
	}
}