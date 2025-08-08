using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace Mictlanix.BE.Web.Models {
	public class GridViewModel {

		//private Type type;
		public GridViewModel(Type type) { }
		public IList<PropertyInfo> Properties { get; set; }

		public List<int> RowsSizes { get; set; }

	}
}