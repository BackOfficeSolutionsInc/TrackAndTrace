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
using MigraDoc.DocumentObjectModel.Internals;
using System.Threading.Tasks;
using static RadialReview.Utilities.LayoutOptimizer;

namespace RadialReview.Utilities {


	public class Container : Table {

		public Cell Contents { get; private set; }

		public Container(Unit width) {
			Rows.LeftIndent = 0;
			LeftPadding = 0;
			RightPadding = 0;

			if (PdfAccessor.DEBUGGER) {
				Borders.Width = .1;
				Borders.Color = Colors.Red;
				Borders.Style = BorderStyle.Dot;
			}

			AddColumn(width);
			var r = AddRow();
			Contents = r.Cells[0];
		}

		public void Add(DocumentObject a) {
			Contents.Elements.Add(a);
		}

		// User-defined conversion from Digit to double
		public static implicit operator Cell(Container d) {
			return d.Contents;
		}

		public Paragraph AddParagraph(string v) {
			return Contents.Elements.AddParagraph(v);
		}
		public Paragraph AddParagraph() {
			return Contents.Elements.AddParagraph();
		}
	}

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

		public StaticElement(Action<ResizeContext> draw, PageSetup setup = null, Unit? widthOverride = null) : base(draw, setup, widthOverride) {
			CalculatedHeights = new Dictionary<Unit, Unit>();
		}

		public override Unit CalcHeight(Unit width, RangedVariables vars) {
			if (!CalculatedHeights.ContainsKey(width)) {
				CalculatedHeights[width] = base.CalcHeight(width, vars);
			}
			return CalculatedHeights[width];
		}
	}

	public class ResizeContext {
		public Container Container { get; set; }
		public RangedVariables Variables { get; set; }
		public LayoutOptimizer.PageLayoutResults Page { get; internal set; }
		public IHint Hint { get; set; }

		public Paragraph AddParagraph() {
			return Container.AddParagraph();
		}

		//public IElement This { get; set; }
	}

	public class ResizableElement : IElement, IElementOverrideWidth {

		public Action<ResizeContext> Draw { get; set; }
		public PageSetup Setup { get; set; }
		public Unit? WidthOverride { get; set; }

		public IHint Hint { get; set; }

		public ResizableElement(Action<ResizeContext> draw, PageSetup setup = null, Unit? widthOverride = null) {
			if (draw == null)
				throw new ArgumentNullException(nameof(draw));
			Draw = draw;
			Setup = setup;
			WidthOverride = widthOverride;
		}

		public void AddToDocument(Action<Container> adder, Unit width, RangedVariables vars) {
			var c = new Container(WidthOverride ?? width);
			var ctx = new ResizeContext() {
				Container = c,
				Variables = vars,
				Hint = Hint,
				//This = this,
			};
			Draw(ctx);
			adder(c);
		}

		public virtual Unit CalcHeight(Unit width, RangedVariables vars) {
			var doc = new Document();
			var sec = doc.AddSection();
			if (Setup != null) {
				sec.PageSetup = Setup;
			}

			sec.PageSetup.PageWidth = Unit.FromInch(500);
			sec.PageSetup.PageHeight = Unit.FromInch(500);

			sec.PageSetup.TopMargin = Unit.FromInch(0);
			sec.PageSetup.BottomMargin = Unit.FromInch(0);
			sec.PageSetup.LeftMargin = Unit.FromInch(0);
			sec.PageSetup.RightMargin = Unit.FromInch(0);

			var t = new Container(width);
			sec.Add(t);

			//t.AddColumn(width);// Unit.FromInch(1.5));
			//var r = t.AddRow();
			//var cell = r.Cells[0];

			var ctx = new ResizeContext() {
				Container = t,
				Variables = vars,
				Hint = Hint,
				//This = this
			};
			try {
				Draw(ctx);
			} catch (Exception e) {
				throw;
			}

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
		Task Draw(Section section, IEnumerable<string> requiredViewBoxes, IEnumerable<IDrawInstruction> instructions);
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
		Container Contents { get; }

	}

	public interface IHint {
		string ForViewBox();
		IEnumerable<IElement> GetElements();
		void Draw(Container viewBoxContainer, INamedViewBox viewBox, RangedVariables pageVariables, int page);
		void SetHasError();
	}

	public class Hint : IHint {
		public bool HasError { get; set; }
		public string ViewBoxName { get; set; }
		public IEnumerable<IElement> Elements { get; set; }
		public Hint(string viewBox, IEnumerable<IElement> elements) {
			ViewBoxName = viewBox;
			Elements = elements;
		}
		public Hint(string viewBox, params IElement[] elements) {
			ViewBoxName = viewBox;
			Elements = elements;

			foreach (var e in elements) {
				e.Hint = this;
			}
		}

		public string ForViewBox() { return ViewBoxName; }
		public IEnumerable<IElement> GetElements() { return Elements.ToList(); }

		public virtual void DrawElement(Container contents, Cell viewBoxContainer, int page) {
			viewBoxContainer.Elements.Add(contents);
		}

		public virtual void Draw(Container viewBoxContainer, INamedViewBox viewBox, RangedVariables pageVariables, int page) {
			foreach (var element in GetElements()) {
				element.AddToDocument(new Action<Container>(x => {
					DrawElement(x, viewBoxContainer,page);
				}), viewBox.GetWidth(), pageVariables);
			}
		}

		public void SetHasError() {
			HasError = true;
		}
	}

	public interface IElementOverrideWidth {
		Unit? OverrideWidth(Unit width);
	}

	public interface IElement {
		IHint Hint { get; set; }
		Unit CalcHeight(Unit width, RangedVariables v);
		void AddToDocument(Action<Container> adder, Unit width, RangedVariables vars);
	}

	public class PageSplitter {

		public class SplitCharaters {
			public SplitCharaters(string splitStr) {
				SplitStr = splitStr;
				JoinStr = splitStr;
			}
			public SplitCharaters(string splitStr, string joinStr) {
				SplitStr = splitStr;
				JoinStr = joinStr;
			}

			public string SplitStr { get; set; }
			public string JoinStr { get; set; }
		}

		protected List<string> Splits;

		public PageSplitter(string str, int maxCharacters, SplitCharaters[] splitOn) {
			Splits = SplitStringIntoPages(str, maxCharacters, splitOn).ToList();
		}
		public PageSplitter(string str, int maxCharacters) {
			Splits = SplitStringIntoPages(str, maxCharacters).ToList();
		}

		public IEnumerable<Hint> GenerateHints(string viewName, Func<IElement> before, Func<string, IElement> contents, Func<IElement> after) {
			return GenerateHints(viewName, Splits, before, contents, after);
		}

		public IEnumerable<Hint> GenerateStaticHints(string viewName, Action<ResizeContext> before, Action<ResizeContext, string> contents, Action<ResizeContext> after = null, PageSetup pageSetup = null, Unit? widthOverride = null) {
			if (contents == null)
				throw new ArgumentNullException(nameof(contents));

			return GenerateHints(viewName, Splits, () => before.NotNull(x => new StaticElement(x, pageSetup, widthOverride)), s => new StaticElement(ctx => contents(ctx, s), pageSetup, widthOverride), () => after.NotNull(x => new StaticElement(x, pageSetup, widthOverride)));
		}



		public static IEnumerable<Hint> GenerateHints(string viewName, IEnumerable<string> splits, Func<IElement> before, Func<string, IElement> contents, Func<IElement> after) {
			if (splits != null && splits.Any()) {
				var i = 0;
				var last = splits.Count() - 1;

				foreach (var s in splits) {
					var elements = new List<IElement>();
					//first
					if (i == 0 && before != null)
						elements.Add(before());
					//middle
					if (contents != null)
						elements.Add(contents(s ?? ""));
					//last
					if (i == last && after != null)
						elements.Add(after());


					elements = elements.Where(x => x != null).ToList();
					if (elements.Any()) {
						yield return new Hint(viewName, elements);
					}

					i++;
				}
			}
		}

		public static IEnumerable<string> SplitStringIntoPages(string str, int maxCharaters) {
			return SplitStringIntoPages(str, maxCharaters, DefaultSplitCharacters);
		}
		public static IEnumerable<string> SplitStringIntoPages(string str, int maxCharaters, SplitCharaters[] splits) {
			if (str == null || splits == null)
				return new List<string>();
			return SplitAndGroup(str, DefaultSplitCharacters, maxCharaters);
		}

		public static SplitCharaters[] DefaultSplitCharacters = new[] {
			new SplitCharaters("\r\n"),
			new SplitCharaters("\n"),
			new SplitCharaters(" "),
			new SplitCharaters("-"),
			new SplitCharaters("","-"),
		};

		private static string[] SplitOnChar(string str, SplitCharaters character) {
			return str.Split(new[] { character.SplitStr }, StringSplitOptions.None);
		}


		private static IEnumerable<string> SplitAndGroup(string str, SplitCharaters[] splitOn, int maxCharacters) {
			//Nothing else to split on...
			if (splitOn == null || splitOn.Length == 0)
				return new[] { str };

			var lowerSplitting = splitOn.Skip(1).ToArray();
			var splits = SplitOnChar(str, splitOn[0]);

			var grouping = new List<string>();
			var prevStr = "";
			foreach (var s in splits) {
				if (s.Length + prevStr.Length <= maxCharacters) {
					if (!string.IsNullOrWhiteSpace(prevStr))
						prevStr = prevStr + splitOn[0].JoinStr;
					grouping.Add(prevStr + s);
					prevStr = "";
				} else {
					grouping.Add(prevStr);
					if (s.Length < maxCharacters) {
						//see if we can merge it later
						prevStr = s;
					} else {
						//By itself it's too long...
						if (splitOn.Length > 1) {
							grouping.AddRange(SplitAndGroup(s, lowerSplitting, maxCharacters));
						} else {
							grouping.Add(s);
						}
						prevStr = "";
					}
				}
			}
			if (!string.IsNullOrWhiteSpace(prevStr)) {
				grouping.Add(prevStr);
			}
			//if (splitOn[0].SplitStr == "" && splitOn.Length == 1) {
			//	if (splits.Count() >= 2) {
			//		var secondToLast = grouping[splits.Count() - 2];
			//		var last = grouping[splits.Count() - 1];
			//		secondToLast + last
			//	}
			//}
			return grouping;
		}

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

		public static IEnumerable<string> SplitString(string str, int approxCharaters) {
			var currentIndex = approxCharaters;
			while (true) {
				if (str.Length < approxCharaters)
					yield return str;



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
			public Container Contents { get; set; }
			public INamedViewBox ViewBox { get; set; }
		}


		public static void Draw(Document doc, LayoutOptimizerResults results) {
			int pageNum = 0;
			foreach (var page in results.Pages) {
				var docPage = doc.AddSection();
				var docLayout = page.Layout;

				var pageViewBoxContainer = new Dictionary<string, Container>();

				var allViewBoxes = docLayout.GetViewBoxes(page.RequiredViewBoxes).ToList();
				var viewBoxLookup = allViewBoxes.ToDictionary(x => x.GetName(), x => x);

				//Create content containers
				foreach (var viewBox in allViewBoxes) {
					pageViewBoxContainer[viewBox.GetName()] = new Container(viewBox.GetWidth());
				}

				//Draw hints on content container
				foreach (var pageHint in page.HintsOnPage) {
					var viewBoxName = pageHint.ForViewBox();
					var viewBoxContents = pageViewBoxContainer[viewBoxName];
					var pageVariables = page.GetVariables(viewBoxName);
					var viewBox = viewBoxLookup[viewBoxName];

					//var ctx = new ResizeContext() {
					//	Container = viewBoxContents,
					//	Hint=pageHint,
					//	Variables = pageVariables,
					//	Page = page,
					//};

					//Draw elements on content container
					pageHint.Draw(viewBoxContents, viewBox, pageVariables, pageNum);
				}

				//Add content containers to the page
				var instructions = new List<Instruction>();
				foreach (var viewBox in allViewBoxes) {
					var viewBoxContent = pageViewBoxContainer[viewBox.GetName()];
					instructions.Add(new Instruction() {
						Contents = viewBoxContent,
						ViewBox = viewBox,
					});
				}

				page.Layout.Draw(docPage, page.RequiredViewBoxes, instructions);
				pageNum++;
			}
		}

		public static LayoutOptimizerResults Optimize(IDocumentGenerator layoutGenerator, IEnumerable<IHint> hints, RangedVariables vars, TimeoutCheck timeout) {

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

			var iteration = 0;
			var maxIterations = 8; //Maximum of 8 pages...

			var previousIterationBoxes = new List<string>();
			bool firstTry = true;

			//Each page...
			while (true) {

                timeout.ShouldTimeout();

				var unfitViewBoxes = new List<string>();

				var pageLayout = layoutGenerator.GetPageLayout(pageNumber);
				var pageViewBoxes = pageLayout.GetViewBoxes(remainingViewBoxes);

				var latestFitResults = new Dictionary<string, ViewBoxOptimzerResults>();

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
							latestFitResults[viewBoxName] = results;

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

				//Max iterations reached, or deadlocked?
				//	if (!firstTry) {
				var diff = SetUtility.AddRemove(previousIterationBoxes, unfitViewBoxes);
				if (iteration > 0 && maxIterations < iteration || (unfitViewBoxes.Any() && diff.AreSame())) {
					if ((unfitViewBoxes.Any() && diff.AreSame())) {
						anyFailures = true;
						//if (!currentPage.HintsOnPage.Any()) {
						//	pageResults.RemoveAt(pageResults.Count() - 1);
						//	currentPage = pageResults.LastOrDefault();
						//}
						Console.WriteLine("Optimize failed:" + string.Join(",", unfitViewBoxes));
						if (currentPage != null && unfitViewBoxes.Any()) {
							AppendError(currentPage, unfitViewBoxes.First(), "---Error sizing contents---");
							try {
								foreach (var v in remainingViewBoxes) {
									currentPage.AddHints(remainingHintsByViewBox[v]);
									remainingHintsByViewBox[v].ForEach(hint=>hint.SetHasError());									
								}
							} catch (Exception e) {
							}
						}
						break;
					}
				}
				previousIterationBoxes = unfitViewBoxes.Select(x => x).ToList();
				//} else {
				//	previousIterationBoxes = new List<string>();
				//}
				//firstTry = false;

				iteration += 1;


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

		private static void AppendError(PageLayoutResults page, string viewBox, string message) {
			try {
				var errorParagraph = new Paragraph();
				errorParagraph.AddText(message ?? "error");
				errorParagraph.Format.Font.Color = Colors.DarkRed;
				errorParagraph.Format.Font.Size = 6;
				errorParagraph.Format.Font.Italic = true;
				page.AddHints(new[] { new Hint(viewBox, new StaticElement(x => x.Container.Add(errorParagraph))) });
			} catch (Exception e) {
				int a = 0;
			}
		}
	}
}