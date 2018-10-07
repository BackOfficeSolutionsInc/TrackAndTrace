using ApiDesign.Tests.TestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ApiDesign.Tests.LockedFiles {
	[TestClass]
	public class LockApiVersions {

		public static string PROJECT_FILENAME = "ApiDesign";
		public static Type ANY_PROJECT_TYPE = typeof(ApiDesign.Controllers.HomeController);

		public static Entry V1_FILE_HASHS = new Entry("V1_File_Hashs");
		public static string V1_NAMESPACE = "ApiDesign.Controllers.V1";


		[TestMethod]
		public void EnsureApiV1Unchanged() {
			EnsureNoFilesChanged(V1_FILE_HASHS, V1_NAMESPACE, ANY_PROJECT_TYPE);
		}

		//[TestMethod]
		public void LockApiV1() {
			var hashes = GetFileHashs(V1_NAMESPACE, typeof(ApiDesign.Controllers.HomeController));
			ExpectedResults.InsertOrUpdate(V1_FILE_HASHS, hashes);
			Assert.Inconclusive("FILES WERE SUCCESSFULLY LOCKED. Now, please comment out this test or the file will continue to be overwritten");
		}

		#region Helpers


		private void EnsureNoFilesChanged(Entry key, string namespacePrefix, Type anyTypeInTheAssembly) {
			List<HashFile> foundHashes= GetFileHashs(namespacePrefix, anyTypeInTheAssembly);
			var expectedHashes = ExpectedResults.GetValue<List<HashFile>>(key);
			SetUtility.AssertEqual(expectedHashes, foundHashes);

		}

		public class HashFile {
			public string Name { get; set; }
			public string Hash { get; set; }

			public override bool Equals(object obj) {
				if (obj is HashFile)
					return Hash.Equals(((HashFile)obj).Hash);
				return false;
			}
			public override int GetHashCode() {
				return Hash.GetHashCode();
			}
			public override string ToString() {
				return Name + " (" + Hash + ")";
			}
		}

		private List<HashFile> GetFileHashs(string namespacePrefix, Type anyTypeInTheAssembly) {
			var asm = Assembly.GetAssembly(anyTypeInTheAssembly);
			var types = asm.GetTypes()
				.Where(t => t.IsClass && t.Namespace != null && t.Namespace.StartsWith(namespacePrefix))
				.ToList();

			Assert.IsTrue(types.Any(), "No classes found. Make sure your namespace and assembly are correct.");

			var sDir = FindSourceDir(asm.CodeBase, PROJECT_FILENAME);
			var tDir = FindTestDir(asm.CodeBase);

			var foundHashes = DirDive(sDir)
				.Where(path => path.ToLower().EndsWith(".cs") && FileHasContents(path, "namespace "+namespacePrefix))
				.Select(path => new HashFile {
					Hash = Hash.File(path),
					Name = path
				}).ToList();
			Assert.IsTrue(foundHashes.Any(), "There were no files containing the namespace ("+namespacePrefix+") in the project (" + sDir + ")");
			return foundHashes;
		}

		[TestMethod]
		public void TestHashing() {
			var str1 = "hello world";
			var str2 = "goodbye world";

			var hash1 = Hash.String(str1);
			var hash2 = Hash.String(str2);

			Assert.IsTrue(Hash.HashsAreDifferent(hash1, hash2));
			Assert.IsFalse(Hash.HashsAreDifferent(hash1, hash1));
			Assert.IsFalse(Hash.HashsAreDifferent(hash1, Hash.String(str1)));
			Assert.IsFalse(Hash.HashsAreDifferent(hash2, hash2));
			Assert.IsFalse(Hash.HashsAreDifferent(hash2, Hash.String(str2)));
		}

		private string FindSourceDir(string assemblyLocation, string projectName) {
			return Path.Combine(Path.GetDirectoryName(FindTestDir(assemblyLocation)), projectName);
		}
		private string FindTestDir(string assemblyLocation) {
			if (Path.GetFileName(assemblyLocation) == "bin")
				return Path.GetDirectoryName(assemblyLocation).Substring(6);
			return FindTestDir(Path.GetDirectoryName(assemblyLocation));
		}

		private static IEnumerable<string> DirDive(string sDir) {
			if (sDir.StartsWith("file:\\"))
				sDir = sDir.Substring(6);
			foreach (string d in Directory.GetDirectories(sDir)) {
				foreach (string f in Directory.GetFiles(d)) {
					yield return f;
				}
				foreach (var a in DirDive(d))
					yield return a;
			}			
		}

		private bool FileHasContents(string file, string search) {
			var reader = new StreamReader(file, Encoding.UTF8);
			var contents = reader.ReadToEnd();

			var modSearch = search.Replace(" ", "\\s+").Replace(".", "\\.");
			return Regex.IsMatch(contents, modSearch);




		}
		#endregion
	}
}
