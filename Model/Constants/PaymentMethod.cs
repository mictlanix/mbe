// 
// PaymentMethod.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.org>
//   Eduardo Nieto <enieto@mictlanix.org>
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

namespace Mictlanix.BE.Model
{
    public enum PaymentMethod : int
    {
        [Display(Name = "Unidentified", ResourceType = typeof(Resources))]
        Unidentified,
        [Display(Name = "Cash", ResourceType = typeof(Resources))]
        Cash,
        [Display(Name = "CreditCard", ResourceType = typeof(Resources))]
        CreditCard,
        [Display(Name = "DebitCard", ResourceType = typeof(Resources))]
        DebitCard,
        [Display(Name = "Check", ResourceType = typeof(Resources))]
        Check,
        [Display(Name = "WireTransfer", ResourceType = typeof(Resources))]
        WireTransfer,
        [Display(Name = "GovernmentFunding", ResourceType = typeof(Resources))]
        GovernmentFunding,
        [Display(Name = "BankDeposit", ResourceType = typeof(Resources))]
		BankDeposit
    }
}
