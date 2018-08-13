using PipelineAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipelineAnalyzer {
	public class HappyFoxLeadParser {

		private List<String> Headings { get; set; }

		private HappyFoxLeadParser() {



		}
		
		public static HappyFoxLeadParser CreateFromLeadsFile(string file) {

			var lineNum = 0;
			using (StreamReader sr = new StreamReader(file)) {
				while (sr.Peek() >= 0) {
					var line = sr.ReadLine();
					switch (lineNum) {
						case 0:
							break;
						case 1:
							break;
						case 2:
							break;
					}

					lineNum += 1;
				}
			}
		}

	}
}
