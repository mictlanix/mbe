// 
// FiscalReport.cs
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
using System.Globalization;
using System.Web.Mvc;
using System.Linq;
using System.Web.Security;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Models
{
    public class FiscalReport
    {
        [Display(Name = "Year", ResourceType = typeof(Resources))]
        public virtual int Year { get; set; }

        [Display(Name = "Month", ResourceType = typeof(Resources))]
        [DisplayFormat(DataFormatString = "{0:00}")]
        public virtual int Month { get; set; }

        [Display(Name = "TaxpayerId", ResourceType = typeof(Resources))]
        public virtual string TaxpayerId { get; set; }
		
        [Display(Name = "TaxpayerName", ResourceType = typeof(Resources))]
		public virtual string TaxpayerName { get; set; }
		
        #region Override Base Methods

        public override string ToString ()
		{
			return string.Format (Resources.Format_FiscalReportName, TaxpayerId, Year, Month);
        }

        public override bool Equals (object obj)
		{
			FiscalReport other = obj as FiscalReport;

			if (other == null)
				return false;

			if (TaxpayerId == string.Empty && other.TaxpayerId == string.Empty &&
				Year == 0 && other.Year == 0 && Month == 0 && other.Month == 0) {
				return (object)this == other;
			}
			
			return Year == other.Year &&
				   Month == other.Month &&
				   TaxpayerId == other.TaxpayerId;
        }

        public override int GetHashCode ()
		{
			if (TaxpayerId == string.Empty && Year == 0 && Month == 0)
				return base.GetHashCode ();

			return string.Format ("{0}#{1}{2}{3}", GetType ().FullName, TaxpayerId, Year, Month).GetHashCode();
        }

        #endregion
    }
    
}