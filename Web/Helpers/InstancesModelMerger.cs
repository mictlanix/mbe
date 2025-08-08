using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Mictlanix.BE.Model;
using NHibernate;

namespace Mictlanix.BE.Web.Helpers {
	public static class InstancesModelMerger {
		public static void Merge (string table, string column_key, int item, int duplicated) {
			string sql = @"
					DELIMITER //
						CREATE PROCEDURE GetLast10Products()
						BEGIN
						    SELECT * FROM product
						    ORDER BY product_id DESC
						    LIMIT 10;
						END //

						DELIMITER ;
						GetLast10Products();
				";

			ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				int ret;

				using (var tx = session.BeginTransaction ()) {
					var query = session.CreateSQLQuery (sql);

					query.AddScalar ("item", NHibernateUtil.Int32);
					query.AddScalar ("duplicated", NHibernateUtil.Int32);
					query.AddScalar ("db", NHibernateUtil.String);
					query.AddScalar ("table_name", NHibernateUtil.String);
					query.AddScalar ("column_key", NHibernateUtil.String);

					query.SetInt32 ("item", item);
					query.SetInt32 ("duplicated", duplicated);
					query.SetString ("db", "mbe_db");
					query.SetString ("table_name", table);
					query.SetString ("column_key", column_key);

					ret = query.ExecuteUpdate ();

					tx.Commit ();
				}

				return ret;
			}, null);
		}

		public static void Test ()
		{
			string sql = @"
						DROP PROCEDURE if EXISTS actualizarReferencias;
						CREATE PROCEDURE actualizarReferencias(
						    IN nombre_db VARCHAR(255),
						    IN nombre_tabla VARCHAR(255),
						    IN nombre_columna VARCHAR(255),
							IN id_old INT,
						    IN id_new INT
						)
						BEGIN
						    SELECT * FROM product
						    ORDER BY product_id DESC
						    LIMIT 10;
						END;
						CALL GetLast10Products();
				";



			ActiveRecordMediator<Product>.Execute (delegate (ISession session, object instance) {
				int ret;

				using (var tx = session.BeginTransaction ()) {
					var query = session.CreateSQLQuery (sql);



					ret = query.ExecuteUpdate ();

					tx.Commit ();
				}

				return ret;
			}, null);
		}
	}
}