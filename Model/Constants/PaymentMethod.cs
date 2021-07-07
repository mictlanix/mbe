// 
// PaymentMethod.cs
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
using System.ComponentModel.DataAnnotations;

namespace Mictlanix.BE.Model {
	public enum PaymentMethod : int {
		[Display (Name = "NA", ResourceType = typeof (Resources))]
		NA,
		[Display (Name = "Cash", ResourceType = typeof (Resources))]
		Cash = 1,
		[Display (Name = "Check", ResourceType = typeof (Resources))]
		Check = 2,
		[Display (Name = "ElectronicFundsTransfer", ResourceType = typeof (Resources))]
		EFT = 3,
		[Display (Name = "CreditCard", ResourceType = typeof (Resources))]
		CreditCard = 4,
		[Display (Name = "ElectronicPurse", ResourceType = typeof (Resources))]
		ElectronicPurse = 5,
		[Display (Name = "ElectronicMoney", ResourceType = typeof (Resources))]
		ElectronicMoney = 6,
		[Display (Name = "FoodVouchers", ResourceType = typeof (Resources))]
		FoodVouchers = 8,
		[Display (Name = "DebitCard", ResourceType = typeof (Resources))]
		DebitCard = 28,
		[Display (Name = "ServiceCard", ResourceType = typeof (Resources))]
		ServiceCard = 29,
		[Display (Name = "AdvancePaymentsApplied", ResourceType = typeof (Resources))]
		AdvancePayments = 30,
		[Display(Name = "ToBeDefined", ResourceType = typeof(Resources))]
		ToBeDefined = 99,
		[Display (Name = "GovernmentFunding", ResourceType = typeof (Resources))]
		GovernmentFunding = 1001
	}
}
