using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserUtilities {
	public static class ObjectExtensions {
		//NotNull
		public static R NotNull<T, R>(this T obj, Func<T, R> f) {
			if (obj != null) {
				try {
					return f(obj);
				} catch (NullReferenceException) {
					return default(R);
				} catch (ArgumentNullException) {
					return default(R);
				}
			} else {
				return default(R);
			}
		}
	}
}
