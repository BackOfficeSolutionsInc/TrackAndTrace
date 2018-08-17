using ParserUtilities.Utilities.LogFile;
using PipelineAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipelineAnalyzer {
	public class HappyFoxLeadParser {
		
		private HappyFoxLeadParser() {
		}
		
		public static LogFile<Client> CreateFromLeadsFile(string path) {
			var file = LogFileReader.Read<Client>(path,3);

			return file;
		}

	}
}
