// 
// AccountModels.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.org>
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
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Web.Mvc;
using System.Web.Security;

namespace Business.Essentials.WebApp.Models
{
    public class ChangePasswordModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "CurrentPassword", ResourceType = typeof(Resources))]
        [StringLength(40, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string OldPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "NewPassword", ResourceType = typeof(Resources))]
        [StringLength(40, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessageResourceName = "Validation_PasswordDoNotMatch", ErrorMessageResourceType = typeof(Resources))]
        [Display(Name = "ConfirmPassword", ResourceType = typeof(Resources))]
        [StringLength(40, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string ConfirmPassword { get; set; }
    }

    public class LogOnModel
    {
        [Required]
        [Display(Name = "UserName", ResourceType = typeof(Resources))]
        [StringLength(20, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password", ResourceType = typeof(Resources))]
        [StringLength(40, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Password { get; set; }

        [Display(Name = "RememberMe", ResourceType = typeof(Resources))]
        public bool RememberMe { get; set; }
    }

    public class RegisterModel
    {
        [Required]
        [StringLength(20, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        [Display(Name = "UserName", ResourceType = typeof(Resources))]
        public string UserName { get; set; }

        [Required]
        [StringLength(40, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        [DataType(DataType.Password)]
        [Display(Name = "Password", ResourceType = typeof(Resources))]
        public string Password { get; set; }

        [Compare("Password", ErrorMessageResourceName = "Validation_PasswordDoNotMatch", ErrorMessageResourceType = typeof(Resources))]
        [DataType(DataType.Password)]
        [StringLength(40, MinimumLength = 4, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        [Display(Name = "ConfirmPassword", ResourceType = typeof(Resources))]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
        [Display(Name = "Employee", ResourceType = typeof(Resources))]
        [UIHint("EmployeeSelector")]
        public int EmployeeId { get; set; }

        [Required]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "Email", ResourceType = typeof(Resources))]
        [StringLength(250, MinimumLength = 6, ErrorMessageResourceName = "Validation_StringLength", ErrorMessageResourceType = typeof(Resources))]
        public string Email { get; set; }
    }
}
