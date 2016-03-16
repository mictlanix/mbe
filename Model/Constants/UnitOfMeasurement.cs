// 
// GenderEnum.cs
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
using System.ComponentModel.DataAnnotations;

namespace Mictlanix.BE.Model {
	public enum UnitOfMeasurement : int {
		[Display (Name = "NA", ResourceType = typeof (Resources))]
		NA,
		[Display (Name = "Piece", ResourceType = typeof (Resources))]
		Piece,
		[Display (Name = "Tens", ResourceType = typeof (Resources))]
		Tens,
		[Display (Name = "Hundreds", ResourceType = typeof (Resources))]
		Hundreds,
		[Display (Name = "Thousands", ResourceType = typeof (Resources))]
		Thousands,
		[Display (Name = "Box", ResourceType = typeof (Resources))]
		Box,
		[Display (Name = "Set", ResourceType = typeof (Resources))]
		Set,
		[Display (Name = "Gram", ResourceType = typeof (Resources))]
		Gram,
		[Display (Name = "Kilogram", ResourceType = typeof (Resources))]
		Kilogram,
		[Display (Name = "Tonne", ResourceType = typeof (Resources))]
		Tonne,
		[Display (Name = "LinearMeter", ResourceType = typeof (Resources))]
		LinearMeter,
		[Display (Name = "SquareMeter", ResourceType = typeof (Resources))]
		SquareMeter,
		[Display (Name = "CubicMeter", ResourceType = typeof (Resources))]
		CubicMeter,
		[Display (Name = "Liter", ResourceType = typeof (Resources))]
		Liter,
		[Display (Name = "Bottle", ResourceType = typeof (Resources))]
		Bottle,
		[Display (Name = "Barrel", ResourceType = typeof (Resources))]
		Barrel,
		[Display (Name = "Second", ResourceType = typeof (Resources))]
		Second,
		[Display (Name = "Minute", ResourceType = typeof (Resources))]
		Minute,
		[Display (Name = "Hour", ResourceType = typeof (Resources))]
		Hour
	}
}

