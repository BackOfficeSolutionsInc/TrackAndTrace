using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using RadialReview;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TractionTools.Tests.Utilities;

namespace TractionTools.Tests.TestUtils {
	public class BaseApiTest : BaseTest{

		public static Version VERSION_0 = new Version(0, 0);
		public static Version VERSION_1 = new Version(1, 0);


		public TestContext TestContext { get; set; }
		public Version VERSION { get; private set; }
		public BaseApiTest(Version version) {
			VERSION = version;
			TestName = "TractionTools.Tests";
		}

		public void CompareModelProperties(bool? _nil, object actual,bool lastComparison=true) {
			_compareModelProperties(null, actual,lastComparison);
		}


		public void CompareModelProperties(object actual,bool lastComparison=true) {
			var expected = new ApiExpected(TestContext, VERSION);
			_compareModelProperties(expected, actual, lastComparison);
		}
		private static void _compareModelProperties(ApiExpected expected, object actual, bool lastComparison) {
			string actualJson = Newtonsoft.Json.JsonConvert.SerializeObject(actual);



			var expectedXml = ExpectedApiResults.Get(expected);

			var regenSetting = Config.GetAppSetting("RegenerateAPI", "").Split(',').Any(x=>x.ToUpper() == "V" + expected.NotNull(y=>y.Verison));

			//-- Just print the result -- 
			if ((expectedXml == null || expected == null) && !regenSetting) {
				Console.WriteLine("====" + actual.NotNull(x => x.GetType().Name) + "====");
				actualJson = Newtonsoft.Json.JsonConvert.SerializeObject(actual, Newtonsoft.Json.Formatting.Indented);
				Console.WriteLine(actualJson);
				if (lastComparison)
					Assert.Inconclusive("Expected JSON does not exist. See console output for actual.");
				return;
			}

			//-- Regenerate --
			if (regenSetting || (expected!=null && expected.Regen)) {
				Console.WriteLine("====" + actual.NotNull(x => x.GetType().Name) + "====");
				actualJson = Newtonsoft.Json.JsonConvert.SerializeObject(actual, Newtonsoft.Json.Formatting.Indented);
				Console.WriteLine(actualJson);
				ExpectedApiResults.InsertOrUpdate(expected, actualJson);
				if (lastComparison)
					Assert.Inconclusive("JSON was regenerated. See console output for actual.");
				return;
			}
			var expectedJson = expectedXml.ExpectedJson;


			try {

				var resurceDic = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(expectedJson);
				var actualDic = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(actualJson);
				CompareModelProperties(resurceDic, actualDic, new List<string>() { "Object" });

			} catch (Newtonsoft.Json.JsonSerializationException ex) {
				var resurceDic = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(expectedJson);
				var actualDic = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(actualJson);

				Assert.AreEqual(resurceDic.Count, actualDic.Count);

				for (int i = 0; i < resurceDic.Count; i++) {
					CompareModelProperties(resurceDic[i], actualDic[i], new List<string>() { "Array" });
				}
			}
		}

		private static void CompareModelProperties(Dictionary<string, object> resurceDic, Dictionary<string, object> actualDic, List<string> path) {
			var result = SetUtility.AddRemove(resurceDic.Keys, actualDic.Keys);
			if (!result.AreSame()) {
				result.PrintDifference();
			}
			if (result.RemovedValues.Any()) {
				Console.WriteLine();
				Console.WriteLine("At: " + string.Join(" + ", path));
				Assert.Fail("Value was removed to model.");
			}

			if (result.AddedValues.Any()) {
				Console.WriteLine();
				Console.WriteLine("At: " + string.Join(" + ", path));
				Assert.Inconclusive("Value was added to model.");
			}

			Assert.IsTrue(result.AreSame());

			foreach (var item in resurceDic) {
				if (item.Value is JObject) {
					var newVal = ((JObject)item.Value).ToObject<Dictionary<string, object>>();
					var actVal = ((JObject)actualDic[item.Key]).ToObject<Dictionary<string, object>>();

					var newPath = path.ToList();
					newPath.Add(item.Key);

					CompareModelProperties(newVal, actVal, newPath);

				} else if (item.Value is JArray) { // check is json array
					var newVal = ((JArray)item.Value).ToObject<List<Dictionary<string, object>>>();
					var actVal = ((JArray)actualDic[item.Key]).ToObject<List<Dictionary<string, object>>>();

					var newPath = path.ToList();
					newPath.Add(item.Key);

					Assert.AreEqual(newVal.Count, actVal.Count);

					for (int i = 0; i < newVal.Count; i++) {
						CompareModelProperties(newVal[i], actVal[i], newPath);
					}
				} else if (item.Value is string || item.Value == null || (item.Value.GetType() == typeof(DateTime)) || item.Value is long || item.Value is bool || (item.Value.GetType() == typeof(double))) {
					// we did this intentionally
					// not required for test this case
				} else {
					throw new NotImplementedException();
				}
			}
		}
	}
}
