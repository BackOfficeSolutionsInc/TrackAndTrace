using Microsoft.SolverFoundation.Services;
using Microsoft.SolverFoundation.Solvers;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Drawing;
using RadialReview.Accessors;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static RadialReview.Utilities.ViewBoxOptimzer;

namespace RadialReview.Utilities {
	public class RangedVariables {
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
			return sum / Math.Max(1, variables.Count());
		}

		public RangedVariables() {
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
		public double[] GetInitials() {
			return variables.Select(x => x.Value.Initial.Point).ToArray();
		}
		public double[] GetLowers() {
			return variables.Select(x => x.Value.Lower.Point).ToArray();
		}

		public double[] GetUppers() {
			return variables.Select(x => x.Value.Upper.Point).ToArray();
		}

		public RangedVariables CloneWith(double[] values) {
			var o = new RangedVariables();

			for (var i = 0; i < variables.Count; i++) {
				var v = variables[i];
				var value = values[i];
				o.Add(v.Key, Unit.FromPoint(value), v.Value.Lower, v.Value.Upper, v.Value.Value);
			}

			return o;

		}

		public RangedVariables CloneReset() {
			return CloneWith(GetInitials());
		}

		//public void ApplyToSolver(UnconstrainedNonlinearModel solver) {
		//	foreach (var variable in variables) {
		//		solver.AddVariable(variable.Key,
		//		}
		//}
	}

	public class StaticElement : ResizableElement {
		public Dictionary<Unit, Unit> CalculatedHeights { get; private set; }

		public StaticElement(Action<Cell, RangedVariables> draw, PageSetup setup = null) : base(draw, setup) {
			CalculatedHeights = new Dictionary<Unit, Unit>();
		}

		public override Unit CalcHeight(Unit width, RangedVariables vars) {
			if (!CalculatedHeights.ContainsKey(width)) {
				CalculatedHeights[width] = base.CalcHeight(width, vars);
			}
			return CalculatedHeights[width];
		}
	}

	public class ResizableElement : IElement, IElementOverrideWidth {

		public Action<Cell, RangedVariables> Draw { get; set; }
		public PageSetup Setup { get; set; }
		public Unit? WidthOverride { get; set; }

		public ResizableElement(Action<Cell, RangedVariables> draw, PageSetup setup = null,Unit? widthOverride=null) {
			Draw = draw;
			Setup = setup;
			WidthOverride = widthOverride;
		}

		public void AddToDocument(Action<Table> adder, Unit width, RangedVariables vars) {
			var t = new Table();

			t.Rows.LeftIndent = 0;
			t.LeftPadding = 0;
			t.RightPadding = 0;

			if (PdfAccessor.DEBUGGER) {
				t.Borders.Width = 1;
				t.Borders.Color = Colors.Red;
			}

			t.AddColumn(WidthOverride??width);// Unit.FromInch(1.5));
			var r = t.AddRow();
			var cell = r.Cells[0];
			Draw(cell, vars);
			adder(t);
		}

		public virtual Unit CalcHeight(Unit width, RangedVariables vars) {
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

			t.AddColumn(width);// Unit.FromInch(1.5));
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

		public Unit? OverrideWidth(Unit width) {
			return WidthOverride;
		}
	}

	public class ViewBoxOptimzer {

		public class FitHeightsResults {
			public Unit TotalHeight { get; set; }
			public bool Fits { get; set; }
			public Dictionary<IElement, Unit> Heights { get; set; }
			public FitHeightsResults() {
				Heights = new Dictionary<IElement, Unit>();
			}
			public Unit SqrError { get; set; }
		}

		public static FitHeightsResults FitHeights(Unit setToHeight, Unit width, IEnumerable<IElement> elements, RangedVariables v) {
			var elementsList = elements.ToList();
			var heights = new List<double>();

			Unit sum = 0.0;
			foreach (var e in elementsList) {
				var w = width;

				if (e is IElementOverrideWidth) {
					w = ((IElementOverrideWidth)e).OverrideWidth(width) ?? width;
				}

				var h = e.CalcHeight(w, v);
				sum += h;
				heights.Add(h);
			}

			double remaining = setToHeight - sum;
			var additionalEach = remaining / Math.Max(1.0, elementsList.Count());

			var res = new FitHeightsResults();
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

			res.SqrError = err * err + adj;


			for (var i = 0; i < elementsList.Count; i++) {
				var e = elementsList[i];
				var h = heights[i];
				res.Heights[e] = h;
			}
			return res;
		}

		public class ViewBoxOptimzerResults {
			public RangedVariables Variables { get; set; }
			public double Error { get; set; }
			public bool SolverCompleted { get; set; }
			public NonlinearResult Result { get; set; }
			public FitHeightsResults FitResults { get; internal set; }
		}

		private static double Sigmoid(double x) {
			return 1.0 / (1.0 + Math.Exp(-x));
		}

		public static ViewBoxOptimzerResults Optimize(Unit setToHeight, Unit width, IEnumerable<IElement> elements, RangedVariables v) {


			var initial = v.GetValues();
			var lowers = v.GetLowers();
			var uppers = v.GetUppers();

			var solver = new NelderMeadSolver();


			var solution = NelderMeadSolver.Solve(x => {
				var clone = v.CloneWith(x);
				var fit = FitHeights(setToHeight, width, elements, clone);
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


			var res = new ViewBoxOptimzerResults();

			if (solverCompleted) {
				res.SolverCompleted = true;
			}
			for (var i = 0; i < 2; i++) {
				var optimalVariables = v.CloneWith(optimal.ToArray());
				res.Error = solution.GetSolutionValue(0);
				res.Result = solution.Result;
				res.Variables = optimalVariables;
				res.FitResults = FitHeights(setToHeight, width, elements, optimalVariables);

				if (!res.FitResults.Fits || !solverCompleted) {
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

	public interface IDocumentGenerator {
		IPageGenerator GetPageLayout(int page);
	}

	public interface IPageGenerator {
		IEnumerable<INamedViewBox> GetViewBoxes(IEnumerable<string> requiredViewBoxs);
		Action<Section> GetDrawer(IEnumerable<string> requiredViewBoxes, IEnumerable<IDrawInstruction> instructions);
	}

	public interface INamedViewBox {
		string GetName();
		Unit GetWidth();
		Unit GetHeight();
	}

	public class NamedViewBox : INamedViewBox {
		public string Name { get; set; }
		public Unit Width { get; set; }
		public Unit Height { get; set; }

		public NamedViewBox(string name, Unit width, Unit height) {
			Name = name;
			Width = width;
			Height = height;
		}

		public string GetName() { return Name; }
		public Unit GetHeight() { return Height; }
		public Unit GetWidth() { return Width; }
	}

	public interface IDrawInstruction {
		INamedViewBox ViewBox { get; }
		Table Contents { get; }

	}

	public interface IHint {
		string ForViewBox();
		IEnumerable<IElement> GetElements();
		void Draw(Table viewBoxContainer, INamedViewBox viewBox, RangedVariables pageVariables);
	}

	public class Hint : IHint {
		public string ViewBoxName { get; set; }
		public IEnumerable<IElement> Elements { get; set; }
		public Hint(string viewBox, IEnumerable<IElement> elements) {
			ViewBoxName = viewBox;
			Elements = elements;
		}
		public Hint(string viewBox, params IElement[] elements) {
			ViewBoxName = viewBox;
			Elements = elements;
		}
		public string ForViewBox() { return ViewBoxName; }
		public IEnumerable<IElement> GetElements() { return Elements.ToList(); }

		public virtual void DrawElement(Table elementContents, Cell viewBoxContainer) {
			viewBoxContainer.Elements.Add(elementContents);
		}

		public virtual void Draw(Table viewBoxContainer, INamedViewBox viewBox, RangedVariables pageVariables) {
			foreach (var element in GetElements()) {
				element.AddToDocument(new Action<Table>(x => {
					var row = viewBoxContainer.AddRow();
					var cell = row.Cells[0];
					DrawElement(x, cell);
				}), viewBox.GetWidth(), pageVariables);
			}
		}
	}

	public interface IElementOverrideWidth {
		Unit? OverrideWidth(Unit width);
	}

	public interface IElement {
		Unit CalcHeight(Unit width, RangedVariables v);
		void AddToDocument(Action<Table> adder, Unit width, RangedVariables vars);
	}



	public class LayoutOptimizer {

		public class PageLayoutResults {

			public PageLayoutResults() {
				HintsOnPage = new List<IHint>();
				ViewBoxResults = new Dictionary<string, ViewBoxOptimzerResults>();
			}

			public IEnumerable<string> RequiredViewBoxes { get; set; }
			public IPageGenerator Layout { get; set; }
			public List<IHint> HintsOnPage { get; set; }
			public Dictionary<string, ViewBoxOptimzerResults> ViewBoxResults { get; set; }

			public RangedVariables GetVariables(string viewBox) {
				return ViewBoxResults[viewBox].Variables;
			}

			public void AddViewBoxResults(string viewBoxName, ViewBoxOptimzerResults results) {
				if (ViewBoxResults.ContainsKey(viewBoxName))
					throw new Exception("PageLayoutResults already contains ViewBox result for " + viewBoxName);
				ViewBoxResults[viewBoxName] = results;
			}

			public void AddHints(IEnumerable<IHint> pageHints) {
				HintsOnPage.AddRange(pageHints);
			}
		}

		public class LayoutOptimizerResults {
			public LayoutOptimizerResults() {
				Pages = new List<PageLayoutResults>();
			}
			public List<PageLayoutResults> Pages { get; set; }
			public bool Success { get; set; }
		}

		public class Instruction : IDrawInstruction {
			public Table Contents { get; set; }
			public INamedViewBox ViewBox { get; set; }
		}


		public static void Draw(Document doc, LayoutOptimizerResults results) {

			foreach (var page in results.Pages) {
				var docPage = doc.AddSection();
				var docLayout = page.Layout;

				var pageViewBoxTable = new Dictionary<string, Table>();

				var allViewBoxes = docLayout.GetViewBoxes(page.RequiredViewBoxes).ToList();
				var viewBoxLookup = allViewBoxes.ToDictionary(x => x.GetName(), x => x);
				
				//Create content containers
				foreach (var viewBox in allViewBoxes) {
					var table = new Table();
					table.Rows.LeftIndent = 0;
					table.LeftPadding = 0;
					table.RightPadding = 0;

					table.AddColumn(viewBox.GetWidth());
					pageViewBoxTable[viewBox.GetName()] = table;
				}

				//Draw hints on content container
				foreach (var pageHint in page.HintsOnPage) {
					var viewBoxName = pageHint.ForViewBox();
					var viewBoxContents = pageViewBoxTable[viewBoxName];
					var pageVariables = page.GetVariables(viewBoxName);
					var viewBox = viewBoxLookup[viewBoxName];
					//Draw elements on content container
					pageHint.Draw(viewBoxContents, viewBox, pageVariables);
				}

				//Add content containers to the page
				var instructions = new List<Instruction>();
				foreach (var viewBox in allViewBoxes) {
					var viewBoxContent = pageViewBoxTable[viewBox.GetName()];
					instructions.Add(new Instruction() {
						Contents = viewBoxContent,
						ViewBox = viewBox,
					});
				}

				page.Layout.GetDrawer(page.RequiredViewBoxes, instructions)?.Invoke(docPage);
			}
		}


		public static LayoutOptimizerResults Optimize(IDocumentGenerator layoutGenerator, IEnumerable<IHint> hints, RangedVariables vars) {

			var fitResults = new LayoutOptimizerResults();

			var hintsForViewBox = new DefaultDictionary<string, List<IHint>>(k => new List<IHint>());

			var allHints = new List<IHint>();
			var allRequiredViewBoxNames = new List<string>();

			foreach (var hint in hints) {
				var name = hint.ForViewBox();
				allRequiredViewBoxNames.Add(name);

				hintsForViewBox[name].Add(hint);
				allHints.Add(hint);
			}

			allRequiredViewBoxNames = allRequiredViewBoxNames.Distinct().ToList();
			var layoutViewBoxNames = new List<string>();


			//Confirm the layout is valid
			{
				var layout = layoutGenerator.GetPageLayout(0);
				var layoutViewBoxes = layout.GetViewBoxes(allRequiredViewBoxNames);
				//One ViewBox per name
				foreach (var b in layoutViewBoxes) {
					var viewBoxName = b.GetName();
					if (layoutViewBoxNames.Contains(viewBoxName)) {
						throw new Exception("Layout already contains a viewBox named " + viewBoxName);
					}
					layoutViewBoxNames.Add(viewBoxName);
				}
				/*
				//Confirm that all elements have a section that they belong to.
				var set = SetUtility.AddRemove(allHintNames, layoutSectionNames);
				if (set.RemovedValues.Any()) {
					throw new Exception("The following named sections are required for the layout: " + string.Join(", ", set.RemovedValues)+". All layout sections must be present on page 1 of the layout");
				}*/

			}


			var remainingHintsByViewBox = new DefaultDictionary<string, List<IHint>>(x => new List<IHint>());
			foreach (var viewBoxName in layoutViewBoxNames) {
				remainingHintsByViewBox[viewBoxName].AddRange(hintsForViewBox[viewBoxName]);
			}

			int pageNumber = 0;
			var pageResults = new List<PageLayoutResults>();

			var currentPage = new PageLayoutResults();
			var nextPage = new PageLayoutResults();

			var failedPreviousPage = new DefaultDictionary<string, bool>(x => false);

			pageResults.Add(currentPage);

			var remainingViewBoxes = allRequiredViewBoxNames.ToList();
			var anyFailures = false;

			//Each page...
			while (true) {

				var unfitViewBoxes = new List<string>();

				var pageLayout = layoutGenerator.GetPageLayout(pageNumber);
				var pageViewBoxes = pageLayout.GetViewBoxes(remainingViewBoxes);



				//Each section of the page
				foreach (var viewBox in pageViewBoxes) {
					var viewBoxName = viewBox.GetName();
					var remainingViewBoxHints = remainingHintsByViewBox[viewBoxName];

					var h = viewBox.GetHeight();
					var w = viewBox.GetWidth();
					if (remainingViewBoxHints.Any()) {

						for (var i = remainingViewBoxHints.Count(); i >= 0; i--) {
							var pageHints = remainingViewBoxHints.Take(i);
							var hintElements = pageHints.SelectMany(x => x.GetElements());
							var varClone = vars.CloneReset();
							var results = ViewBoxOptimzer.Optimize(h, w, hintElements, varClone);

							//Doesnt fit..
							var failure = false;
							if (!results.FitResults.Fits) {
								if (!unfitViewBoxes.Contains(viewBoxName)) {
									unfitViewBoxes.Add(viewBoxName);
								}
								failure = (i == 0 && failedPreviousPage[viewBoxName]);
							}

							//Fits
							if (results.FitResults.Fits || failure) {
								anyFailures = anyFailures || failure;
								remainingHintsByViewBox[viewBoxName] = remainingHintsByViewBox[viewBoxName].Skip(i).ToList();
								currentPage.AddHints(pageHints);
								currentPage.AddViewBoxResults(viewBoxName, results);
								break;
							}

							if (i == 0) {
								failedPreviousPage[viewBoxName] = true;
							}
						}
					}
				}


				//fix remaining viewboxes
				//remainingSections.Where(x=>x.).ToList();

				unfitViewBoxes = unfitViewBoxes.Distinct().ToList();
				currentPage.Layout = pageLayout;
				currentPage.RequiredViewBoxes = remainingViewBoxes.ToList();

				remainingViewBoxes = remainingHintsByViewBox.Where(x => x.Value.Any()).Select(x => x.Key).ToList();

				//Need another page?
				if (unfitViewBoxes.Any()) {
					pageResults.Add(nextPage);
					currentPage = nextPage;
					nextPage = new PageLayoutResults();
					pageNumber += 1;
				} else {
					break;
				}
			}

			fitResults.Pages = pageResults;
			fitResults.Success = !anyFailures;
			return fitResults;
		}
	}
}