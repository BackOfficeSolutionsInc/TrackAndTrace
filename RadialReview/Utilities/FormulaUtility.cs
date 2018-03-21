using Dangl.Calculator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace RadialReview.Utilities {
	public class FormulaUtility {

		public class ParsedFormula {
			protected List<string> Parts { get; set; }

			public ParsedFormula(string formula) {
				formula = formula ?? "";
				formula = formula.Trim();
				this.Parts = new List<string>();
				var open = false;
				var current = "";
				foreach (var f in formula) {
					if (open == true && f == '[') {
						throw new FormatException("Unexpected '['");
					}
					if (open == false && f == ']') {
						throw new FormatException("Unexpected ']'");
					}

					if (open == false && f == '[') {
						open = true;
						this.Parts.Add(current);
						current = "";
					}
					current += f;
					if (open == true && f == ']') {
						open = false;
						this.Parts.Add(current);
						current = "";
					}
				}
				this.Parts.Add(current);
				Parts.RemoveAll(x => string.IsNullOrWhiteSpace(x));
			}

			public List<object> Tokenize<T>(Func<string, T> replacements) {
				if (replacements == null)
					throw new ArgumentNullException(nameof(replacements));
				var sb = new List<object>();
				foreach (var p in Parts) {
					if (p.StartsWith("[")) {
						sb.Add(replacements(p.Substring(1, p.Length - 2)));
					} else {
						sb.Add(p);
					}
				}
				return sb;
			}

			public string Replace(Func<string, string> replacements) {
				return string.Join("", Tokenize(replacements));
			}

			public double? Evaluate(Func<string, double?> replacements, bool nullOnDivideByZero = true) {
				//StringBuilder sb = new StringBuilder();
				//foreach (var p in Parts) {
				//    if (p.StartsWith("[")) {
				//        sb.Append(replacements(p.Substring(1, p.Length - 2)) ?? 0.0);
				//    } else {
				//        sb.Append(p);
				//    }
				//}
				var sb = Replace(x => "" + (replacements(x) ?? 0.0));
				var result = Calculator.Calculate(sb);
				if (result.IsValid) {
					return (double)result.Result;
				} else {
					if (nullOnDivideByZero && result.ErrorPosition == -1 && Double.IsInfinity(result.Result)) {
						return (double?)null;
					}

					var positionError = "Divide by zero";

					if (result.ErrorPosition >= 0)
						positionError = "Error at " + result.ErrorPosition;

					throw new InvalidOperationException(result.ErrorMessage, new Exception(positionError));
				}

			}

			public List<string> GetVariables() {
				return Parts.Where(x => x.StartsWith("[")).Select(x => x.Substring(1, x.Length - 2)).ToList();
			}
		}

		public static ParsedFormula Parse(string formula) {
			return new ParsedFormula(formula);
		}

		public static double? ParseFormula(string formula, Func<string, double?> replacements) {
			return Parse(formula).Evaluate(replacements);
		}
	}
}