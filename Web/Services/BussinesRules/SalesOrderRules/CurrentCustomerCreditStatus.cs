using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Helpers;
using Mictlanix.BE.Web.Models;

namespace Mictlanix.BE.Web.Services.BussinesRules.SalesOrderRules {
	public class CurrentCustomerCreditStatus : IBusinessRule<SalesOrder> {
		public Result Validate (SalesOrder entity, User user)
		{
			var customer = entity.Customer;
			if (customer.Debt() + entity.TotalEx - customer.CreditLimit > 0) {
				return Result.Failure(string.Format (Resources.CreditLimitExceeded, entity.Customer.Name));
			}
			return Result.Success ();
		}
	}
}