using Microsoft.SolverFoundation.Services;
using Microsoft.SolverFoundation.Solvers;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Drawing;
using RadialReview.Utilities.Pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities {
	public class ResizableElement {


		public Action<Cell, ResizeVariables> Draw { get; set; }
		public PageSetup Setup { get; set; }
		public Unit Width { get; set; }

		public ResizableElement(Unit width, Action<Cell, ResizeVariables> draw, PageSetup setup = null) {
			Draw = draw;
			Setup = setup;
			Width = width;
		}

		public void AddToDocument(Action<Table> adder, ResizeVariables vars) {
			var t = new Table();
			t.AddColumn(Width);// Unit.FromInch(1.5));
			var r = t.AddRow();
			var cell = r.Cells[0];
			Draw(cell, vars);
			adder(t);
		}

		public Unit CalcHeight(ResizeVariables vars) {
			var doc = new Document();
			var sec = doc.AddSection();
			if (Setup != null) {
				sec.PageSetup = Setup;
			}

			sec.PageSetup.PageWidth = Unit.FromInch(1000);
			sec.PageSetup.PageHeight = Unit.FromInch(1000);

			sec.PageSetup.TopMargin = Unit.FromInch(0);
			sec.PageSetup.BottomMargin = Unit.FromInch(0);
			sec.PageSetup.LeftMargin = Unit.FromInch(0);
			sec.PageSetup.RightMargin = Unit.FromInch(0);

			var t = sec.AddTable();

			t.AddColumn(Width);// Unit.FromInch(1.5));
			var r = t.AddRow();

			var cell = r.Cells[0];

			Draw(cell, vars);

			var render = new DocumentRenderer(doc);
			render.PrepareDocument();

			var d = render.GetDocumentObjectsFromPage(1);
			var p = render.FormattedDocument.GetRenderInfos(1);

			var h = p[0].LayoutInfo.ContentArea.Height;
			var w = p[0].LayoutInfo.ContentArea.Width;
			return Unit.FromInch(h.Inch);
		}
	}

	public class ResizeVariables {

		public class Variable {
			public Variable(Unit value, Unit lower, Unit upper, Unit? initial = null) {
				Value = value;
				Lower = lower;
				Upper = upper;
				Initial = initial ?? Value;
			}

			public Unit Value { get; private set; }
			public Unit Upper { get; private set; }
			public Unit Lower { get; private set; }

			public Unit Initial { get; private set; }
		}

		private List<KeyValuePair<string, Variable>> variables { get; set; }


		public double ArgSqrError() {
			var sum = 0.0;
			foreach (var v in variables) {
				var x = v.Value.Value;
				var i = v.Value.Initial;
				var low = v.Value.Lower;
				var high = v.Value.Upper;
				var e = (x - i);
				//var e = ((x - i) / ((x - low) * (x - high)));
				var scaler = Math.Max(0.01, v.Value.Upper - v.Value.Lower);
				sum += (e * e) / (scaler * scaler);
			}
			return sum/Math.Max(1,variables.Count());
		}

		public ResizeVariables() {
			variables = new List<KeyValuePair<string, Variable>>();
		}

		public void Add(string key, Unit value, Unit lower, Unit upper) {
			variables.Add(new KeyValuePair<string, Variable>(key, new Variable(value, lower, upper)));
		}

		protected void Add(string key, Unit value, Unit lower, Unit upper, Unit initial) {
			variables.Add(new KeyValuePair<string, Variable>(key, new Variable(value, lower, upper, initial)));
		}

		public Unit Get(string key) {
			return variables.FirstOrDefault(x => x.Key == key).NotNull(x => x.Value.Value);
		}

		public double[] GetValues() {
			return variables.Select(x => x.Value.Value.Point).ToArray();
		}

		public double[] GetLowers() {
			return variables.Select(x => x.Value.Lower.Point).ToArray();
		}

		public double[] GetUppers() {
			return variables.Select(x => x.Value.Upper.Point).ToArray();
		}

		public ResizeVariables CloneWith(double[] values) {
			var o = new ResizeVariables();

			for (var i = 0; i < variables.Count; i++) {
				var v = variables[i];
				var value = values[i];
				o.Add(v.Key, Unit.FromPoint(value), v.Value.Lower, v.Value.Upper, v.Value.Value);
			}

			return o;

		}

		//public void ApplyToSolver(UnconstrainedNonlinearModel solver) {
		//	foreach (var variable in variables) {
		//		solver.AddVariable(variable.Key,
		//		}
		//}
	}

	public class PdfOptimzer {


		public class FitHeightsResult {

			public Unit TotalHeight { get; set; }
			public bool Fits { get; set; }
			public Dictionary<ResizableElement, Unit> Heights { get; set; }
			public FitHeightsResult() {
				Heights = new Dictionary<ResizableElement, Unit>();
			}

			public Unit SqrError { get; set; }
		}

		public static FitHeightsResult FitHeights(Unit setToHeight, IEnumerable<ResizableElement> elements, ResizeVariables v) {
			var elementsList = elements.ToList();
			var heights = new List<double>();

			Unit sum = 0.0;
			foreach (var e in elementsList) {
				var h = e.CalcHeight(v);
				sum += h;
				heights.Add(h);
			}

			double remaining = setToHeight - sum;
			var additionalEach = remaining / Math.Max(1.0, elementsList.Count());

			var res = new FitHeightsResult();
			res.TotalHeight = sum;
			if (remaining >= 0) {
				for (var i = 0; i < heights.Count; i++) {
					heights[i] += additionalEach;
				}
				res.Fits = true;
			}

			var err = remaining / Math.Max(.001, setToHeight);

			var adj = 0.0;
			if (sum > setToHeight) {
				adj = 1;//-remaining;
			}

			res.SqrError = err*err+ adj;
			

			for (var i = 0; i < elementsList.Count; i++) {
				var e = elementsList[i];
				var h = heights[i];
				res.Heights[e] = h;
			}
			return res;
		}


		public class OptimizedHeightsResult {
			public ResizeVariables Variables { get; set; }
			public double Error { get; set; }
			public bool SolverCompleted { get; set; }
			public NonlinearResult Result { get; set; }
			public FitHeightsResult Fit { get; internal set; }
		}

		private static double Sigmoid(double x) {
			return 1.0 / (1.0 + Math.Exp(-x));
		}

		public static OptimizedHeightsResult OptimizeHeights(Unit setToHeight, IEnumerable<ResizableElement> elements, ResizeVariables v) {


			var initial = v.GetValues();
			var lowers = v.GetLowers();
			var uppers = v.GetUppers();

			var solver = new NelderMeadSolver();


			var solution = NelderMeadSolver.Solve(x => {
				var clone = v.CloneWith(x);
				var fit = FitHeights(setToHeight, elements, clone);
				var err = fit.SqrError;

				return err;

				//if (stopOnFit && fit.Fits)
				//	return -1;
				//var thresh = .1;
				////if (err < thresh) {
				//	var argError = clone.ArgSqrError();
				//	return err*.9 + .1*err*(Sigmoid(argError)+1);
				////}
				//return err;//				+ argError/(err*err);// +1) * (1+argError);//Sigmoid(argError) + argError/Sigmoid(err);
			}, initial.ToArray(), lowers, uppers);

			var valid = new[] {
				NonlinearResult.Feasible,
				NonlinearResult.LocalOptimal,
				NonlinearResult.Optimal
			};
			

			var optimal = new List<double>();
			
			//If is valid...
			for (var i = 0; i < initial.Count(); i++) {
				optimal.Add(solution.GetValue(i + 1));
			}

			var solverCompleted = valid.Any(x => x == solution.Result);


			var res = new OptimizedHeightsResult();

			if (solverCompleted) {
				res.SolverCompleted = true;
			}
			for(var i=0;i<2;i++) {
				var optimalVariables = v.CloneWith(optimal.ToArray());
				res.Error = solution.GetSolutionValue(0);
				res.Result = solution.Result;
				res.Variables = optimalVariables;
				res.Fit = FitHeights(setToHeight, elements, optimalVariables);

				if (!res.Fit.Fits || !solverCompleted) {
					//Run again if suboptimal..
					optimal = initial.ToList();
					continue;
				}
				//otherwise just return.
				break;
			}
			return res;
		}





	}
}