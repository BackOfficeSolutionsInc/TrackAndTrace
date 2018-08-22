using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserUtilities.Utilities.LogFile {

	[Flags]
	public enum FlagType {
		None=0,
		UserFlag=1,
		UnusuallyLongRequest=2,
		PotentialCauses=4,
		LikelyCause = 8,
		ByGuid = 16,
		HasError = 32,
        Fixed = 64,
    }

	public interface ILogLine {

		string Guid { get; set; }
		DateTime EndTime { get; }
		DateTime StartTime { get;  }
		FlagType Flag { get; set; }
		int GroupNumber { get; set; }

		string[] GetHeaders();
		string[] GetLine(DateTime firstLogStartTime);
		ILogLine ConstructFromLine(string line);
	}
}
