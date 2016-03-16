// 
// CashCount.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
//   Eduardo Nieto <enieto@mictlanix.com>
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
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using System.ComponentModel.DataAnnotations;


namespace Mictlanix.BE.Model {
	[ActiveRecord ("cash_count")]
	public class CashCount : ActiveRecordLinqBase<CashCount> {
		[PrimaryKey (PrimaryKeyType.Identity, "cash_count_id")]
		public int Id { get; set; }

		[BelongsTo ("session")]
		[Display (Name = "CashSession", ResourceType = typeof (Resources))]
		//[Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(Resources))]
		public virtual CashSession Session { get; set; }

		[Property]
		[DataType (DataType.Currency)]
		[Display (Name = "Denomination", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof (Resources))]
		public decimal Denomination { get; set; }

		[Property]
		[Display (Name = "Quantity", ResourceType = typeof (Resources))]
		[Required (ErrorMessageResourceName = "Validation_RequiredNumber", ErrorMessageResourceType = typeof (Resources))]
		public int Quantity { get; set; }

		[Property]
		[Display (Name = "Type", ResourceType = typeof (Resources))]
		public CashCountType Type { get; set; }

		[DataType (DataType.Currency)]
		[Display (Name = "Total", ResourceType = typeof (Resources))]
		public decimal Total {
			get { return Denomination * Quantity; }
		}

		#region Override Base Methods

		public override string ToString ()
		{
			return string.Format ("{0:c} × {1} = {2:c}", Denomination, Quantity, Total);
		}

		public override bool Equals (object obj)
		{
			CashCount other = obj as CashCount;

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
