﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Mictlanix.BE.Model.Validation {
	public sealed class DistinctAttribute : ValidationAttribute {
		const string default_error_message = "'{0}' must be distinct than '{1}'";
		readonly string property_name;

		public DistinctAttribute (string basePropertyName)
		    : base (default_error_message)
		{
			property_name = basePropertyName;
		}

		//Override default FormatErrorMessage Method
		public override string FormatErrorMessage (string name)
		{
			return string.Format (ErrorMessageString, name, property_name);
		}

		//Override IsValid
		protected override ValidationResult IsValid (object value, ValidationContext validationContext)
		{
			//Get PropertyInfo Object
			var basePropertyInfo = validationContext.ObjectType.GetProperty (property_name);

			//Get Value of the property
			var other = basePropertyInfo.GetValue (validationContext.ObjectInstance, null);
			var attrs = basePropertyInfo.GetCustomAttributes (typeof (DisplayAttribute), false);
			string property = attrs.Length == 0 ? property_name : ((DisplayAttribute) attrs [0]).GetName ();

			//Actual comparision
			if (object.Equals (value, other)) {
				var message = string.Format (ErrorMessageString, validationContext.DisplayName, property);
				return new ValidationResult (message);
			}

			//Default return - This means there were no validation error
			return null;
		}

	}


}
