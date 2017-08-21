using System;
using System.Net;
using zkemkeeper;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mictlanix.BE.Clocks
{
    public class ClockSynchronizer
    {
		public zkemkeeper.CZKEM keeper = new zkemkeeper.CZKEM ();
		//public void Sinchronize () {
		//	bool run = keeper.Connect_Net("192.168.100.33", 4370);


		//	foreach (ConnectionStringSettings conn in ConfigurationManager.ConnectionStrings) {
		//		DbManager.AddConnectionString (conn.Name, conn.ConnectionString);
		//	}

		//	int.TryParse (ConfigurationManager.AppSettings ["Port"], out port);

		//	using (var t = new Timer (Tick, null, 500, 5000)) {
		//		Console.WriteLine ("Press Ctl+C to stop...");

		//		while (run) {
		//			var key = Console.ReadKey ();
		//			run = key.Modifiers != ConsoleModifiers.Control || key.Key != ConsoleKey.C;
		//		}
		//	}
		//}
    }
}
