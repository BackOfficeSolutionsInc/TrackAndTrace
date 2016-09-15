using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview {
	public static class SearchUtil {
		public static IEnumerable<T> Search<T>(this IEnumerable<T> all, string terms, Func<T, string> lookAtStrings) {
			return Search(all, terms, x => new string[] { lookAtStrings(x) });
		}

		public static IEnumerable<T> Search<T>(this IEnumerable<T> all, string terms, Func<T, IEnumerable<string>> lookAtStrings) {
			var results = new List<T>();
			var ts = terms.Split(' ').Select(x => x.ToLower()).ToList();
			return all.Where(a => {
				var allStrings = lookAtStrings(a).Select(x => x.ToLower()).ToList();
				return (ts.All(t => allStrings.Any(y => y.Contains(t))));
			});
		}
	}
}