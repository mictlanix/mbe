// 
// DateRange.cs
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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Models {
	public enum c_Reason {

		[Description ("01")]
		ComprobanteEmitidoConErroresConRelacion = 1,
		[Description ("02")]
		ComprobanteEmitidoConErroresSinRelacion = 2,
		[Description ("03")]
		OperacionNoRealizada = 3,
		[Description ("04")]
		OperacionNominativaRelacionadaEnFacturaGlobal = 4
	}

	public static class c_ReasonExtensions {
		public static string ToCodigo (this c_Reason reason)
		{
			var attribute = reason.GetType ()
			    .GetMember (reason.ToString ())
			    .FirstOrDefault ()
			    ?.GetCustomAttribute<DescriptionAttribute> ();

			return attribute?.Description ?? string.Empty;
		}
	}

	public class CancelFiscalDocument {
		public CancelFiscalDocument ()
		{
		}

		public int Id { get; set; }

		[Display (Name = "StampId", ResourceType = typeof (Resources))]
		public  string StampId { get; set; }

		
		[Display(Name = "Reason", ResourceType = typeof(Resources))]
		public c_Reason Reason { get; set; }

		[Display (Name = "Substitution", ResourceType = typeof (Resources))]
		public  string Substitution { get; set; }

	}
}