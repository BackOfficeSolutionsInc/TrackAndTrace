using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RadialReview.Properties;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TractionTools.Tests.Utilities {

    public class CompareBetween<U> : IDisposable {
        protected U Start { get; set; }
        protected U Expected { get; set; }
        protected Func<U> Comparer { get; set; }
        public CompareBetween(Func<U> comparer, Func<U, U> expected) {
            Start = comparer();
            Expected = expected(Start);
            Comparer = comparer;
        }

        public CompareBetween(Func<U> comparer,  U expected) {
            Start = comparer();
            Expected = expected;
            Comparer = comparer;
        }

        public void Assert(Func<U,U> expected) {
            var nowVal= Comparer();
            var expecting = expected(Start);
            if (!(nowVal == null && expecting == null) && !nowVal.Equals(expecting)) {
                throw new Exception("Expected: " + expecting + ", Found: " + nowVal);
            }
        }

        public void Dispose() {
            if (!ExceptionUtility.IsInException()) {
                Assert(x => Expected);
            }
        }
    }
    public class CompareUtil {
        private static bool DEBUG = true;
        public static void AssertObjectJsonEqualsString(string expectedJson,object obj) {

            var json = JsonConvert.SerializeObject(obj, Formatting.None,new JsonSerializerSettings() {ReferenceLoopHandling = ReferenceLoopHandling.Ignore});
                        
            if (DEBUG) {
                Console.WriteLine("==================");
                Console.WriteLine(json);
            }
            if (json != expectedJson) {
                throw new Exception("Json does not match string");
            }

        }

        public static CompareBetween<T> StaticComparer<CLASS,T>(string propName, T expectedEnd) {
            return new CompareBetween<T>(new Func<T>(() => (T)Reflections.ReflectionExtensions.GetStaticField<CLASS>(propName)), expectedEnd);
        }
        public static CompareBetween<T> StaticComparer<CLASS,T>(string propName, Func<T,T> expectedEnd) {
            return new CompareBetween<T>(new Func<T>(() => (T)Reflections.ReflectionExtensions.GetStaticField<CLASS>(propName)), expectedEnd);
        }

    }
}
