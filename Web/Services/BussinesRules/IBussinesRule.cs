using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mictlanix.BE.Model;
using MySqlX.XDevAPI.Common;

namespace Mictlanix.BE.Web.Services.BussinesRules {
	public interface IBusinessRule<T> { Result Validate (T entity, User user); }
}
