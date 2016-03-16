// 
// Store.cs
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
using Mictlanix.BE.Model.Validation;

namespace Mictlanix.BE.Model {
	[ActiveRecord ("exchange_rate", Lazy = true)]
	public class ExchangeRate : ActiveRecordLinqBase<ExchangeRate> {
		[PrimaryKey (PrimaryKeyType.Identity, "exchange_rate_id")]
		public virtual int Id { get; set; }

		[Property]
		[DataType (DataType.Date)]
		[Display (Name = "Date", ResourceType = typeof (Resources))]
		public virtual DateTime Date { get; set; }

		[Property]
		[Range (0.0001, double.MaxValue, ErrorMessageResourceName = "Validation_CannotBeZeroOrNegative", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "Rate", ResourceType = typeof (Resources))]
		public virtual decimal Rate { get; set; }

		[Property]
		[Distinct ("Target", ErrorMessageResourceName = "Validation_ShouldNotBeEquals", ErrorMessageResourceType = typeof (Resources))]
		[Display (Name = "Base", ResourceType = typeof (Resources))]
		public virtual CurrencyCode Base { get; set; }

		[Property]
		[Display (Name = "Target", ResourceType = typeof (Resources))]
		public virtual CurrencyCode Target { get; set; }

		#region Override Base Methods

		public override string ToString ()
		{
			return string.Format ("1 {2} = {1:0.00##} {3} ({0:d})", Date, Rate, Base, Target);
		}

		public override bool Equals (object obj)
		{
			Store other = obj as Store;

			if (other == null)
				return false;

			if (Id == 0 && other.Id == 0)
				return (object) this == other;
			else
				return Id == other.Id;
		}

		public override int GetHashCode ()
		{
			if (Id == 0)
				return base.GetHashCode ();

			return string.Format ("{0}#{1}", GetType ().FullName, Id).GetHashCode ();
		}

		#endregion
	}
}