using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserUtilities.Utilities.LogFile {
	public interface ILogLine {
		DateTime EndTime { get; }
		DateTime StartTime { get;  }

		string[] ToTitle();
		string[] ToLine(DateTime date);
		ILogLine ConstructFromLine(string line);
	}
}
