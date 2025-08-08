using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Services.BussinesRules.SalesOrderRules {
	public class CompletedSalesOrderRule : IBusinessRule<SalesOrder> {
		public Result Validate (SalesOrder entity, User user)
		{
			if(!entity.IsCancelled && entity.IsCompleted) {
				return Result.Success ();
			}
				return Result.Failure(Resources.ItemAlreadyCompletedOrCancelled);
		}
	}
}