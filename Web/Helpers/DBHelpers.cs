using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using NHibernate;

namespace Mictlanix.BE.Web.Helpers {
	public static class DBHelpers {
		public static void Merge (string table, string pk, int old_id, int new_id) {
			string call = @"CALL replace_id_reference(:table, :pk, :old, :new);";
			

			ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				int ret;

				using (var tx = session.BeginTransaction ()) {
					var call_session = session.CreateSQLQuery (call);
					call_session.SetString ("table", table);
					call_session.SetString ("pk", pk);
					call_session.SetInt32 ("old", old_id);
					call_session.SetInt32 ("new", new_id);
					ret = call_session.ExecuteUpdate ();

					tx.Commit ();
				}

				return ret;
			}, null);

		}
	}
}