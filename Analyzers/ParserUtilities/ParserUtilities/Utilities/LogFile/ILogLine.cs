using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserUtilities.Utilities.LogFile {
	public interface ILogLine {
		DateTime EndTime { get; }
		DateTime StartTime { get;  }
		bool IsFlagged { get; set; }
		int GroupNumber { get; set; }

		string[] GetHeaders();
		string[] GetLine(DateTime firstLogStartTime);
		ILogLine ConstructFromLine(string line);
	}
}
