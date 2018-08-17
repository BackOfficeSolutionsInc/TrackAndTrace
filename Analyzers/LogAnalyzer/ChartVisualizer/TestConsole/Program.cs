using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChartVisualizer;

namespace TestConsole {
	class Program {
		static void Main(string[] args) {
			XYSeries myString = "Hello, World";
			ChartDebugger.TestShowVisualizer(myString);

		}
	}
}
