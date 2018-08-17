using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TractionTools.Tests.TestUtils;

namespace TractionTools.Tests.Utilities {
	public class ApiExpected {

		private static DefaultDictionary<string, int> callCount = new DefaultDictionary<string, int>(x => 0);

		public ApiExpected(TestContext context, Version verison) {
			Name = context.FullyQualifiedTestClassName.Split('.').Last()+"."+context.TestName;
			if (callCount[Name] > 0)
				Name += "-" + callCount[Name];
			Verison = verison;
			callCount[Name] += 1;
		}

		public string Name { get; set; }
		public Version Verison { get; set; }
		public bool Regen { get; set; }

		public static ApiExpected Regenerate(Version version) {
			return new ApiExpected(null, version) {
				Regen = true
			};
		}
	}

	public class ExpectedApiResults {
				
		public class XmlApiResult {
			[XmlAttribute]
			public string Name { get; set; }
			[XmlAttribute("GeneratedVersion")]
			public string _GeneratedVersion { get { return "" + GeneratedVerison; } set { GeneratedVerison = Version.Parse(value); } }


			public string ExpectedJson { get; set; }
			public DateTime GeneratedDate { get; set; }

			[XmlIgnore]
			public Version GeneratedVerison { get; set; }


			//public int Verison { get; set; }
		}

		public static string GetFile(Version version) {
			var p = Path.Combine(BaseTest.GetTestSolutionPath(), "_data");
			return Path.Combine(p,"ApiResults_" + version + ".xml");
		}


		public static void InsertOrUpdate(ApiExpected result, string json) {
			var all = Deserialize(result.Verison);
			var match = all.Where(x => x.Name != result.Name).ToList();
			match.Add(new XmlApiResult() { Name = result.Name, ExpectedJson = json, GeneratedVerison = result.Verison, GeneratedDate = DateTime.UtcNow });
			SerializeFile(match, result.Verison);
		}

		private static void SerializeFile(List<XmlApiResult> dataToSerialize, Version version) {
			SerializeFile(dataToSerialize, GetFile(version));
		}


		private static void SerializeFile(List<XmlApiResult> dataToSerialize, string file) {
			var text = SerializeText(dataToSerialize);
			File.WriteAllText(file,text);
		}

		private static string SerializeText(List<XmlApiResult> dataToSerialize) {
			try {
				var stringwriter = new System.IO.StringWriter();
				var serializer = new XmlSerializer(typeof(List<XmlApiResult>));
				serializer.Serialize(stringwriter, dataToSerialize);
				return stringwriter.ToString();
			} catch {
				throw;
			}
		}

		public static List<XmlApiResult> Deserialize(Version version) {
			return DeserializeFile(GetFile(version));
		}

		private static List<XmlApiResult> DeserializeFile(string path) {

			if (!File.Exists(path))
				SerializeFile(new List<XmlApiResult>(), path);

			var text = File.ReadAllText(path);
			return DeserializeText(text);
		}

		private static List<XmlApiResult> DeserializeText(string xmlText) {
			try {
				var stringReader = new System.IO.StringReader(xmlText);
				var serializer = new XmlSerializer(typeof(List<XmlApiResult>));
				return (List<XmlApiResult>)serializer.Deserialize(stringReader);
			} catch {
				throw;
			}
		}

		public static XmlApiResult Get(ApiExpected key) {
			if (key == null)
				return null;	
			var results = Deserialize(key.Verison);
			var r= results.Where(x => x.Name == key.Name).FirstOrDefault();
			//r.Verison = version;
			return r;
		}
	}
}