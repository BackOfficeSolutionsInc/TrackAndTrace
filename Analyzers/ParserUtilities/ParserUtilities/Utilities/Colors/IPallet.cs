using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserUtilities.Utilities.Colors {
	public interface IPallet {
		string GetColor(double percent);
		string NextColor();
	}
}
