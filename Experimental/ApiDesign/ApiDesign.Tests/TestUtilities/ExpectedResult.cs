using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ApiDesign.Tests.TestUtilities {

	public class Entry {
		public Entry(string name) {
			Name = name;
		}
		public string Name { get; set; }

		public override string ToString() {
			return Name;
		}
	}

	public class ExpectedResults {

		public static bool AreEqual<T>(string key, T value) {
			var expectedXml = ExpectedResults.Get(new Entry(key));
			if (expectedXml == null)
				throw new Exception("Key does not exist in the expected results file.");
			return expectedXml.ExpectedJson == JsonConvert.SerializeObject(value);
		}


		public class XmlExpectedResult {
			[XmlAttribute]
			public string Name { get; set; }
			public string ExpectedJson { get; set; }
			public DateTime GeneratedDate { get; set; }
			public XmlExpectedResult() {
				GeneratedDate = DateTime.UtcNow;
			}
		}

		public static string GetFile() {
			var testDir = GetDirectory();
			var p = Path.Combine(testDir, "_data");
			if (!Directory.Exists(p))
				Directory.CreateDirectory(p);

			return Path.Combine(p, "ExpectedResults.xml");
		}

		public static string GetDirectory() {
			var asm = Assembly.GetCallingAssembly();
			return FindDir(asm.CodeBase);
		}

		private static string FindDir(string assemblyLocation) {
			if (Path.GetFileName(assemblyLocation) == "bin")
				return Path.GetDirectoryName(assemblyLocation).Substring(6);
			return FindDir(Path.GetDirectoryName(assemblyLocation));
		}

		public static void InsertOrUpdate<T>(Entry key, T obj) {
			InsertOrUpdate(key,JsonConvert.SerializeObject(obj));
		}

		private static void InsertOrUpdate(Entry key, string json) {
			var all = Deserialize();
			var match = all.Where(x => x.Name != key.Name).ToList();
			match.Add(new XmlExpectedResult() { Name = key.Name, ExpectedJson = json, GeneratedDate = DateTime.UtcNow });
			SerializeFile(match);
		}

		private static void SerializeFile(List<XmlExpectedResult> dataToSerialize) {
			SerializeFile(dataToSerialize, GetFile());
		}


		private static void SerializeFile(List<XmlExpectedResult> dataToSerialize, string file) {
			var text = SerializeText(dataToSerialize);
			File.WriteAllText(file, text);
		}

		private static string SerializeText(List<XmlExpectedResult> dataToSerialize) {
			try {
				var stringwriter = new System.IO.StringWriter();
				var serializer = new XmlSerializer(typeof(List<XmlExpectedResult>));
				serializer.Serialize(stringwriter, dataToSerialize);
				return stringwriter.ToString();
			} catch {
				throw;
			}
		}

		public static List<XmlExpectedResult> Deserialize() {
			return DeserializeFile(GetFile());
		}

		private static List<XmlExpectedResult> DeserializeFile(string path) {

			if (!File.Exists(path))
				SerializeFile(new List<XmlExpectedResult>(), path);

			var text = File.ReadAllText(path);
			return DeserializeText(text);
		}

		private static List<XmlExpectedResult> DeserializeText(string xmlText) {
			try {
				var stringReader = new System.IO.StringReader(xmlText);
				var serializer = new XmlSerializer(typeof(List<XmlExpectedResult>));
				return (List<XmlExpectedResult>)serializer.Deserialize(stringReader);
			} catch {
				throw;
			}
		}

		public static XmlExpectedResult Get(Entry key) {
			if (key == null)
				return null;
			var results = Deserialize();
			var r = results.Where(x => x.Name == key.Name).FirstOrDefault();
			//r.Verison = version;
			return r;
		}

		public static T GetValue<T>(Entry key) {
			var found = Get(key);
			if (found == null)
				throw new Exception("Entry ("+key+") does not exist in the file");
			return JsonConvert.DeserializeObject<T>(found.ExpectedJson);
		}
	}
}
