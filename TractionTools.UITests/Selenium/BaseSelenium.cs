using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.IO;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.IE;
using TractionTools.Tests.TestUtils;
using OpenQA.Selenium;
using System.Web.Mvc;
using System.Linq.Expressions;
using RadialReview.Models;
using RadialReview;
using System.Linq;
using System.Collections.Generic;
using RadialReview.Utilities;
using System.Threading.Tasks;
using TractionTools.Tests.Utilities;
using System.Web.Configuration;
using System.Configuration;
using System.Threading;
using System.Management;
using RadialReview.Utilities.DataTypes;
using System.Runtime.Serialization;
using System.Security.Policy;
using System.Web;
using TractionTools.UITests.Utilities;
using TractionTools.UITests.Utilities.Extensions;
using System.Drawing;
using OpenQA.Selenium.Remote;
using System.Reflection;

namespace TractionTools.UITests.Selenium {
	//http://stephenwalther.com/archive/2011/12/22/asp-net-mvc-selenium-iisexpress


	[Flags]
	public enum WithBrowsers {
		Chrome = 1,
		Firefox = 2,
		IE = 4,
		All = 7
	}

	[TestClass]
	public class BaseSelenium : BaseTest {
		const int iisPort = 2020;
		public static List<WithBrowsers> AllBrowsers = new List<WithBrowsers> { WithBrowsers.Chrome, /*WithBrowsers.Firefox, WithBrowsers.IE*/ };

		//private static string _applicationName;
		//private static string _testName;
		private static Process _iisProcess;
		private WithBrowsers? _RequiredBrowsers = null;
		private Dictionary<IWebDriver, Credentials> currentDriverUser = new Dictionary<IWebDriver, Credentials>();
		private static Dictionary<Guid, Credentials> _AdminCredentials = new Dictionary<Guid, Credentials>();
		private static object lck = new object();

		public static FirefoxDriver _FirefoxDriver;
		public static ChromeDriver _ChromeDriver;
		public static InternetExplorerDriver _InternetExplorerDriver;

		public WithBrowsers RequiredBrowsers {
			get {
				_RequiredBrowsers = _RequiredBrowsers ?? (WithBrowsers)Config.GetAppSetting("RequiredBrowsers", "0").ToInt();
				return _RequiredBrowsers.Value;
			}
		}


		private static List<ImageId> ImagesNeedingGeneration = new List<ImageId>();
		private static List<ImageId> ImagesDoNotMatch = new List<ImageId>();
		private static List<ImageId> ExistingIds = new List<ImageId>();
		private static List<Exception> Deferred = new List<Exception>();

		protected BaseSelenium() { }

		[TestInitialize]
		public void TestInitialize() {
			Parallel.ForEach(AllBrowsers, b => {
				var driver = GetDriver(b);
				driver.Navigate().GoToUrl(GetAbsoluteUrl("/Account/login"));
				driver.WaitForAlert(.5);
			});
			Deferred = new List<Exception>();
		}

		[TestCleanup]
		[DebuggerHidden]
		public void TestCleanup() {
			if (Deferred.Any()) {
				if (Deferred.All(x => x is AssertInconclusiveException)) {
					var builder = "";
					var noMatch = Deferred.Where(x => x.Message.StartsWith("Images do not match: "));
					if (noMatch.Any()) {
						builder += "IMAGES DO NOT MATCH:\n===========================\n";
						builder += String.Join("\n", noMatch.Select(x => x.Message.SubstringAfter("Images do not match: "))) + "\n\n";
					}
					var noCompare = Deferred.Where(x => x.Message.StartsWith("No comparison image: "));

					if (noCompare.Any()) {
						builder += "NO COMPARISON IMAGE:\n===========================\n";
						builder += String.Join("\n", noCompare.Select(x => x.Message.SubstringAfter("No comparison image: "))) + "\n\n";
					}
					var others = Deferred.Where(x => !x.Message.StartsWith("No comparison image: ") && !x.Message.StartsWith("Images do not match: "));

					if (others.Any()) {
						builder += "DEFERRED EXCEPTIONS:\n===========================\n";
						builder += String.Join("\n", others.Select(x => x.Message)) + "\n\n";
					}

					Assert.Inconclusive("The following exceptions occurred:\n\n" + builder);
				}
				if (Deferred.Count() == 1)
					throw Deferred[0];
				throw new AggregateException(Deferred);
			}
		}

		[AssemblyInitialize]
		public static void AssemblyInitialize(TestContext ctx) {
			// Start IISExpress
			ApplicationName = "RadialReview";
			TestName = "TractionTools.UITests";
			StartIIS();
			// Start Selenium drivers

			ChromeOptions options = new ChromeOptions();
			//options.AddArgument("--headless");

			_ChromeDriver = new ChromeDriver(options);
			_ChromeDriver.Navigate().GoToUrl(GetAbsoluteUrl("/Account/login"));
			//_FirefoxDriver.Navigate().GoToUrl(GetAbsoluteUrl("/Account/login"));
			//_InternetExplorerDriver.Navigate().GoToUrl(GetAbsoluteUrl("/Account/login"));
		}
		[AssemblyCleanup]
		public static void AssemblyCleanup() {
			// Ensure IISExpress is stopped
			KillAllISSExpress();
			// Stop all Selenium drivers
			if (_FirefoxDriver != null)
				_FirefoxDriver.Quit();
			if (_ChromeDriver != null)
				_ChromeDriver.Quit();
			if (_InternetExplorerDriver != null)
				_InternetExplorerDriver.Quit();

			//if (ImagesDoNotMatch.Any()) {
			//var p = Path.Combine(GetTestSolutionPath(), "_Log", GetShortTestId());
			//Directory.CreateDirectory(p);
			var lines = ImagesDoNotMatch.Select(x => x.GetName()).OrderBy(x => x);
			var p = Path.Combine(GetTestSolutionPath(), "_Log", "current");
			Directory.CreateDirectory(p);
			File.WriteAllLines(Path.Combine(p, "NoMatch.txt"), lines);
			// }
			//if (ImagesNeedingGeneration.Any()) {
			//var p = Path.Combine(GetTestSolutionPath(), "_Log", GetShortTestId());
			//Directory.CreateDirectory(p);
			lines = ImagesNeedingGeneration.Select(x => x.GetName()).OrderBy(x => x);
			//File.WriteAllLines(Path.Combine(p, "NeedGeneration.txt"), lines);
			p = Path.Combine(GetTestSolutionPath(), "_Log", "current");
			Directory.CreateDirectory(p);
			File.WriteAllLines(Path.Combine(p, "NeedGeneration.txt"), lines);
			//}
		}

		private static RemoteWebDriver GetDriver(WithBrowsers flag) {
			switch (flag) {
				case WithBrowsers.Chrome: {
						if (_ChromeDriver == null) {
							_ChromeDriver = new ChromeDriver();
						}
						_ChromeDriver.Manage().Window.Size = new Size(1022, 806/*767*/);
						return _ChromeDriver;
					}
				case WithBrowsers.Firefox: {
						if (_FirefoxDriver == null) {
							FirefoxProfile Prof = new FirefoxProfile();
							FirefoxBinary Bin = new FirefoxBinary(Config.GetAppSetting("FirefoxLocation"));
							_FirefoxDriver = new FirefoxDriver(Bin, Prof);
							// _FirefoxDriver = new FirefoxDriver();
						}
						return _FirefoxDriver;
					}
				case WithBrowsers.IE: {
						if (_InternetExplorerDriver == null)
							_InternetExplorerDriver = new InternetExplorerDriver();
						return _InternetExplorerDriver;
					}
			}
			throw new ArgumentOutOfRangeException("Unhandled Browser: " + flag);
		}

		protected string AddScreenshot(IWebDriver driver, string name) {
			var folder = GetScreenshotFolder();
			var file = Path.Combine(folder, name);
			driver.TakeScreenshot(file);
			return file;
		}

		public void TestForConsoleErrors(TestCtx driver) {
			var errorContainer = driver.FindElement(By.ClassName("JSError"));
			if (errorContainer != null) {
				var any = errorContainer.FindElements(By.TagName("li"));
				if (any != null && any.Any()) {
					Console.WriteLine("CONSOLE ERRORS:");
					foreach (var a in any) {
						Console.WriteLine(a);
					}
					driver.DeferException(new Exception("CONSOLE ERRORS", new AggregateException(any.Select(x => new Exception(x.Text)).ToArray())));
				}
			}
		}

		//protected static string GetTestName()
		//{
		//    var stackTrace = new StackTrace();
		//    foreach (var stackFrame in stackTrace.GetFrames()) {
		//        MethodBase methodBase = stackFrame.GetMethod();
		//        Object[] attributes = methodBase.GetCustomAttributes(typeof(TestMethodAttribute), false);
		//        if (attributes.Length >= 1) {
		//            return methodBase.Name;
		//        }
		//    }
		//    Console.WriteLine(string.Join("\n", stackTrace.GetFrames().Select(x => x.GetMethod().Name)));
		//    throw new Exception("Not called from a test method");
		//    //return "Not called from a test method";
		//}

		protected Dictionary<string, int> TestCount = new Dictionary<string, int>();

		public void TestView(Credentials mockUser, string url, Action<TestCtx> test, WithBrowsers browsers = WithBrowsers.All) {
			var exceptions = new List<Exception>();
			var flagDrivers = AllBrowsers;
			var testName = GetTestName();

			var count = TestCount.GetOrAddDefault(testName, x => 0);
			TestCount[testName] += 1;
			if (TestCount[testName] > 1)
				testName += "_" + TestCount[testName];

			var ctx = new TestCtx(url, testName, ExistingIds, ImagesNeedingGeneration, ImagesDoNotMatch);



			ctx._Deferred = Deferred;
			Parallel.ForEach(flagDrivers, b => {
				if (browsers.HasFlag(b)) {
					var driver = GetDriver(b);
					ctx.CurrentBrowser = b;
					ctx.CurrentDriver = driver;
					try {
						RunTestOnWebDriver(mockUser, url, ctx, test);
						TestForConsoleErrors(ctx);
						ctx.TestScreenshot("Complete");
					} catch (Exception e) {
						PreserveStackTrace(e);
						exceptions.Add(e);
						try {
							Console.WriteLine("Screenshot:\n" + AddScreenshot(driver, Guid.NewGuid() + ".png"));
						} catch (Exception e2) {
							Console.WriteLine(e2.Message);
							Console.WriteLine(e2.StackTrace);

						}
					}
				}
			});
			//exceptions.AddRange(testInfo.Defer);
			exceptions = exceptions.Where(x => x != null).ToList();

			if (exceptions.Any()) {
				if (exceptions.All(x => x is AssertInconclusiveException)) {
					Assert.Inconclusive("The following exceptions occurred:\n\n" + string.Join(",", exceptions.Select(x => x.Message)));
				}
				if (exceptions.Count() == 1)
					throw exceptions[0];
				throw new AggregateException(exceptions);
			}


			var untested = new List<string>();
			foreach (var r in RequiredBrowsers.GetFlags()) {
				if (!browsers.HasFlag(r))
					untested.Add(Enum.GetName(typeof(WithBrowsers), r));
			}

			if (untested.Any())
				Assert.Inconclusive("Not all browsers were tested. Still requires " + string.Join(",", untested) + ".");

		}

		public static string GetAbsoluteUrl(string relativeUrl) {
			if (!relativeUrl.StartsWith("/")) {
				relativeUrl = "/" + relativeUrl;
			}
			return String.Format("http://localhost:{0}{1}", iisPort, relativeUrl);
		}

		#region Private
		public static void PreserveStackTrace(Exception e) {
			var ctx = new StreamingContext(StreamingContextStates.CrossAppDomain);
			var mgr = new ObjectManager(null, ctx);
			var si = new SerializationInfo(e.GetType(), new FormatterConverter());

			e.GetObjectData(si, ctx);
			mgr.RegisterObject(e, 1, si); // prepare for SetObjectData
			mgr.DoFixups(); // ObjectManager calls SetObjectData

			// voila, e is unmodified save for _remoteStackTraceString
		}
		private void RunTestOnWebDriver(Credentials mockUser, string url, TestCtx ctx, Action<TestCtx> test) {
			try {

				if (!currentDriverUser.ContainsKey(ctx) || !currentDriverUser[ctx].Equals(mockUser)) {
					ctx.Navigate().GoToUrl(GetAbsoluteUrl("/Account/login?ReturnUrl=" + HttpUtility.UrlEncode(url)));
					ctx.WaitForAlert(2);
					ctx.FindElement(By.Name("UserName")).SendKeys(mockUser.Username);
					ctx.FindElement(By.Name("Password")).SendKeys(mockUser.Password);
					ctx.FindElement(By.Id("loginForm")).FindElement(By.TagName("form")).Submit();
					//driver.WaitForAlert();
					currentDriverUser[ctx] = mockUser;
				} else {
					ctx.Navigate().GoToUrl(GetAbsoluteUrl(url));
				}

			} catch (NoSuchElementException) {
				throw new AssertFailedException("Could not login. (" + ctx.GetType().Name + ")");
			}
			test(ctx);
		}
		protected static async Task<Credentials> GetAdminCredentials(Guid id) {
			if (!_AdminCredentials.ContainsKey(id)) {
				var user = await GetAdminUser(id);
				var username = "admin" + id.ToString().Replace("-", "").ToLower() + "@admin.com";
				var password = id.ToString().Replace("-", "").ToLower();
				_AdminCredentials[id] = new Credentials(username, password, user);
			}
			return _AdminCredentials[id];
		}
		private static string PidFile() {
			lock (lck) {
				var proc = Path.Combine(Path.GetTempPath(), "IISExpressProc");
				var pidFile = Path.Combine(proc, "pid.txt");
				if (!Directory.Exists(proc))
					Directory.CreateDirectory(proc);
				if (!File.Exists(pidFile))
					File.Create(pidFile).Close();

				while (!File.Exists(pidFile))
					Thread.Sleep(10);

				return pidFile;
			}
		}
		private static void KillAllProcessesSpawnedBy(UInt32 parentProcessId) {
			// logger.Debug("Finding processes spawned by process with Id [" + parentProcessId + "]");

			// NOTE: Process Ids are reused!
			ManagementObjectSearcher searcher = new ManagementObjectSearcher(
				"SELECT * " +
				"FROM Win32_Process " +
				"WHERE ParentProcessId=" + parentProcessId);
			ManagementObjectCollection collection = searcher.Get();
			if (collection.Count > 0) {
				//logger.Debug("Killing [" + collection.Count + "] processes spawned by process with Id [" + parentProcessId + "]");
				foreach (var item in collection) {
					UInt32 childProcessId = (UInt32)item["ProcessId"];
					if ((int)childProcessId != Process.GetCurrentProcess().Id) {
						KillAllProcessesSpawnedBy(childProcessId);

						Process childProcess = Process.GetProcessById((int)childProcessId);
						// logger.Debug("Killing child process [" + childProcess.ProcessName + "] with Id [" + childProcessId + "]");
						childProcess.Kill();
					}
				}
			}
		}
		private static void KillAllISSExpress() {

			var pidFile = PidFile();
			var lines = File.ReadAllLines(pidFile);
			foreach (var p in lines) {
				var pid = p.ToInt();
				try {
					KillAllProcessesSpawnedBy((UInt32)pid);

					using (var proc = Process.GetProcessById(pid)) {
						proc.Kill();
						proc.WaitForExit();
					}
				} catch (ArgumentException) {
				}
			}
			lock (lck) {
				File.Delete(pidFile);
			}
		}
		private static void StartIIS() {
			var applicationPath = GetApplicationPath();
			var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

			//Kill existing proc
			KillAllISSExpress();

			_iisProcess = new Process();
			_iisProcess.StartInfo.FileName = programFiles + @"\IIS Express\iisexpress.exe";
			_iisProcess.StartInfo.Arguments = string.Format(@"/path:""{0}"" /port:{1}", applicationPath, iisPort);
			_iisProcess.Start();

			var pidFile = PidFile();
			lock (lck) {
				File.AppendAllLines(pidFile, (_iisProcess.Id + "").AsList());
			}
		}
		#endregion

		

		protected static void ConcludeMeeting(TestCtx d) {
			d.TestScreenshot("BeforeConclude");

			d.FindElement(By.PartialLinkText("Conclude"), 10).Click();
			d.FindElement(By.Id("SendEmail"), 10).Uncheck();
			d.FindElement(By.Id("form0"), 10).Submit();
			d.FindElement(By.ClassName("meeting-stats"), 15);
		}
	}
}
