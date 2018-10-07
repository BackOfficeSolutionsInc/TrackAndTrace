using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserUtilities {
	public class Log {

		public static int Tabs = 0;

		private static string Format(string type, string message) {
			return string.Format("{1,6}{0,-" + (Tabs * 3) + "} - {2}", "", type , message);
		}
		
		public static void Message(string message) {
			Console.WriteLine(Format("",message));
		}
		public static void Info(string message) {
			Console.WriteLine(Format("INFO", message));
		}
		public static void Warn(string message,bool confirmation) {
			Console.WriteLine("-------------------------");
			Console.WriteLine(Format("WARN", message));
			Console.WriteLine("\n\n(press enter to continue)");
			PlayAlert(3);
			if (confirmation) {
				Console.ReadKey();
			}
			Console.WriteLine("-------------------------");
		}

		public static void PlayAlert(int times) {
			for (var i = 0; i < times; i++) {
				Console.Beep(600, 150);
				if (i == times)
					break;
				Console.Beep(37, 1);
			}
		}

		public static void Error(string message, bool confirmation) {
			Console.WriteLine("-------------------------");
			Console.WriteLine(Format("ERROR", message));
			Console.WriteLine("\n\n(press enter to continue)");
			PlayAlert(4);
			if (confirmation) {
				Console.ReadKey();
			}
			Console.WriteLine("-------------------------");


		}

	}
}
