using ParserUtilities.Utilities.OutputFile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipelineAnalyzer {
	public class Program {
		public static void Main(string[] args) {

			var basePath = @"S:\repos\Radial\RadialReview\Analyzers\PipelineAnalyzer\";
			var inputPath = basePath + "HappyFox.csv";
			var file = HappyFoxLeadParser.CreateFromLeadsFile(inputPath);

			file.Filter(x => x.GetWeeksAgo() < 6);
			var ordering = new List<string> { "1 Demo", "2 Trial", "3 Walk", "4 Paid", "5 Won", "6 Lost", "Closed", "7 Remarket" };

			/*var pt = file.ToPivotTable(x => x.Ticket_Status, x => "" + (int)x.GetWeeksAgo(), x => "" + x.Count);
			pt.XOrder = a => ordering.IndexOf( a.Ticket_Status );
			pt.YOrder = x => x.GetWeeksAgo();*/

			//var pt = file.ToMatrix(
			//	x => x.Ticket_Status,
			//	x => (int)x.GetWeeksAgo(),
			//	a => a.File.Filter(x => x.GetWeeksAgo() < a.X).Count(),
			//	(prev, curr) => {
			//		prev.Result += curr.Result;
			//		return prev;
			//	});

			var pt = file.ToMatrixBuilder(x => x.Ticket_Status, x => (int)x.GetWeeksAgo())
				.XOrder(a => ordering.IndexOf(a.Ticket_Status))
				//.YOrder(a => a.GetWeeksAgo())
				.IncrementIf(x=> (int)x.Line.GetWeeksAgo() <= x.Y && x.X == x.Line.Ticket_Status);
			//,(x,(int)y)=>(); 
			//);// ordering, x => ""+(int)x.GetWeeksAgo());

			var outputPath = basePath + "pt.csv";
			pt.Save(outputPath);
		}
	}
}
