//
// IncidenceSearch.cs
//
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
//
// Copyright (C) 2026 Eddy Zavaleta, Mictlanix, and contributors.
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
using System.ComponentModel.DataAnnotations;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Models {
	// Every filter is optional: with none set the list is the whole trail, newest first.
	public class IncidenceSearch : Search<Incidence> {
		[Display (Name = "Source", ResourceType = typeof (Resources))]
		public SourceType? Source { get; set; }

		[Display (Name = "Reference", ResourceType = typeof (Resources))]
		public int? Reference { get; set; }

		[DataType (DataType.Date)]
		[Display (Name = "StartDate", ResourceType = typeof (Resources))]
		public DateTime? StartDate { get; set; }

		// Inclusive: the whole of this day is in range.
		[DataType (DataType.Date)]
		[Display (Name = "EndDate", ResourceType = typeof (Resources))]
		public DateTime? EndDate { get; set; }
	}
}
