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

namespace TractionTools.UITests.Selenium {
    //http://stephenwalther.com/archive/2011/12/22/asp-net-mvc-selenium-iisexpress

    public class Credentials {
        public string Username { get; private set; }
        public string Password { get; private set; }
        public UserOrganizationModel User { get; private set; }

        public Credentials(String username, string password, UserOrganizationModel user = null)
        {
            Username = username;
            Password = password;
            User = user;
        }

        public override bool Equals(object obj)
        {
            if (obj is Credentials) {
                var o = (Credentials)obj;
                return o.Username == Username && o.Password == Password;
            }
            return false;
        }
    }

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
        //private static string _applicationName;
        //private static string _testName;
        private static Process _iisProcess;
        private static string TempFolder;
        private WithBrowsers? _RequiredBrowsers = null;
        private Dictionary<IWebDriver, Credentials> currentDriverUser = new Dictionary<IWebDriver, Credentials>();
        private Dictionary<Guid, Credentials> _AdminCredentials = new Dictionary<Guid, Credentials>();
        private static object lck = new object();

        public static FirefoxDriver _FirefoxDriver;
        public static ChromeDriver _ChromeDriver;
        public static InternetExplorerDriver _InternetExplorerDriver;
        public static long Timestamp = DateTime.UtcNow.ToJavascriptMilliseconds() / (1000*60);

        protected BaseSelenium() { }

        [AssemblyInitialize]
        public static void TestInitialize(TestContext ctx)
        {
            // Start IISExpress
            StartIIS("RadialReview", "TractionTools.UITests");
            // Start Selenium drivers
            _ChromeDriver = new ChromeDriver();
            _ChromeDriver.Navigate().GoToUrl(GetAbsoluteUrl("/Account/login"));
            //_FirefoxDriver.Navigate().GoToUrl(GetAbsoluteUrl("/Account/login"));
            //_InternetExplorerDriver.Navigate().GoToUrl(GetAbsoluteUrl("/Account/login"));
        }
        [AssemblyCleanup]
        public static void TestCleanup()
        {
            // Ensure IISExpress is stopped
            KillAllISSExpress();
            // Stop all Selenium drivers
            if (_FirefoxDriver != null)
                _FirefoxDriver.Quit();
            if (_ChromeDriver != null)
                _ChromeDriver.Quit();
            if (_InternetExplorerDriver != null)
                _InternetExplorerDriver.Quit();
        }

        private IWebDriver GetDriver(WithBrowsers flag)
        {
            switch (flag) {
                case WithBrowsers.Chrome: {
                        if (_ChromeDriver == null) _ChromeDriver = new ChromeDriver();
                        return _ChromeDriver;
                    }
                case WithBrowsers.Firefox: {
                        if (_FirefoxDriver == null) _FirefoxDriver = new FirefoxDriver();
                        return _FirefoxDriver;
                    }
                case WithBrowsers.IE: {
                        if (_InternetExplorerDriver == null) _InternetExplorerDriver = new InternetExplorerDriver();
                        return _InternetExplorerDriver;
                    }
            }
            throw new ArgumentOutOfRangeException("Unhandled Browser: " + flag);
        }

        protected string AddScreenshot(IWebDriver driver, string name)
        {
            var folder = Path.Combine(GetTempFile(), "screenshots");
            var n = (""+Timestamp);
            folder = Path.Combine(folder, n.Substring(0,4)+"-"+n.Substring(4));
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            var file = Path.Combine(folder, name);
            driver.TakeScreenshot(file);
            return file;
        }

        public void TestView(Credentials mockUser, string url, Action<IWebDriver> test, WithBrowsers browsers = WithBrowsers.All)
        {
            var exceptions = new List<Exception>();
            var flagDrivers = new List<WithBrowsers> { WithBrowsers.Chrome, WithBrowsers.Firefox, WithBrowsers.IE };
            Parallel.ForEach(flagDrivers, b => {
                if (browsers.HasFlag(b)) {
                    var driver = GetDriver(b);
                    try {
                        RunTestOnWebDriver(mockUser, url, driver, test);
                    } catch (Exception e) {
                        PreserveStackTrace(e);
                        Console.WriteLine("Screenshot:" + AddScreenshot(driver, Guid.NewGuid() + ".png"));
                        exceptions.Add(e);
                    }
                }
            });
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

        protected static void PreserveStackTrace(Exception e)
        {
            var ctx = new StreamingContext(StreamingContextStates.CrossAppDomain);
            var mgr = new ObjectManager(null, ctx);
            var si = new SerializationInfo(e.GetType(), new FormatterConverter());

            e.GetObjectData(si, ctx);
            mgr.RegisterObject(e, 1, si); // prepare for SetObjectData
            mgr.DoFixups(); // ObjectManager calls SetObjectData

            // voila, e is unmodified save for _remoteStackTraceString
        }


        public WithBrowsers RequiredBrowsers
        {
            get
            {
                _RequiredBrowsers = _RequiredBrowsers ?? (WithBrowsers)Config.GetAppSetting("RequiredBrowsers", "0").ToInt();
                return _RequiredBrowsers.Value;
            }
        }
        public static string GetAbsoluteUrl(string relativeUrl)
        {
            if (!relativeUrl.StartsWith("/")) {
                relativeUrl = "/" + relativeUrl;
            }
            return String.Format("http://localhost:{0}{1}", iisPort, relativeUrl);
        }

        #region Private
        private void RunTestOnWebDriver(Credentials mockUser, string url, IWebDriver driver, Action<IWebDriver> test)
        {
            try {

                if (!currentDriverUser.ContainsKey(driver) || !currentDriverUser[driver].Equals(mockUser)) {
                    driver.Navigate().GoToUrl(GetAbsoluteUrl("/Account/login?ReturnUrl=" + HttpUtility.UrlEncode(url)));
                    driver.FindElement(By.Name("UserName")).SendKeys(mockUser.Username);
                    driver.FindElement(By.Name("Password")).SendKeys(mockUser.Password);
                    driver.FindElement(By.Id("loginForm")).FindElement(By.TagName("form")).Submit();
                    //driver.WaitForAlert();
                    currentDriverUser[driver] = mockUser;
                } else {
                    driver.Navigate().GoToUrl(GetAbsoluteUrl(url));
                }
                
            } catch (NoSuchElementException e) {
                throw new AssertFailedException("Could not login. (" + driver.GetType().Name + ")");
            }
            test(driver);
        }
        protected async Task<Credentials> GetAdminCredentials(Guid id)
        {
            if (!_AdminCredentials.ContainsKey(id)) {
                var user = await GetAdminUser(id);
                var username = "admin" + id.ToString().Replace("-", "").ToLower() + "@admin.com";
                var password = id.ToString().Replace("-", "").ToLower();
                _AdminCredentials[id] = new Credentials(username, password, user);
            }
            return _AdminCredentials[id];
        }
        private static string PidFile()
        {
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
        private static void KillAllProcessesSpawnedBy(UInt32 parentProcessId)
        {
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
        private static void KillAllISSExpress()
        {

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
                } catch (ArgumentException e) {
                }
            }
            lock (lck) {
                File.Delete(pidFile);
            }
        }
        private static void StartIIS(string appProjName, string testProjName)
        {
            var applicationPath = GetApplicationPath(appProjName, testProjName);
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

        protected static string GetTempFile()
        {
            return Path.Combine(Path.GetTempPath(), "TractionTools");
        }

        protected static string GetApplicationPath(string applicationName, string testName)
        {
            var solutionFolder = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)));
            var solutionPath = Path.Combine(solutionFolder, applicationName);
            var testPath = Path.Combine(solutionFolder, testName);

            var temp = Path.Combine(Path.Combine(GetTempFile(), "ServerFiles"), Guid.NewGuid().ToString());
            Directory.CreateDirectory(temp);

            FileUtility.DirectoryCopy(solutionPath, temp, true, new string[] { "\\obj\\" });

            var testConfigFileMap = new ExeConfigurationFileMap() { ExeConfigFilename = Path.Combine(testPath, "app.config") };
            var testConfig = ConfigurationManager.OpenMappedExeConfiguration(testConfigFileMap, ConfigurationUserLevel.None);
            var testSettings = (AppSettingsSection)testConfig.GetSection("appSettings");

            var webConfigPaths = new string[] { Path.Combine(Path.Combine(temp, "bin"), "web.config"), Path.Combine(temp, "web.config") };

            foreach (var p in webConfigPaths) {
                var tempConfigFileMap = new ExeConfigurationFileMap() { ExeConfigFilename = p };
                var tempConfig = ConfigurationManager.OpenMappedExeConfiguration(tempConfigFileMap, ConfigurationUserLevel.None);
                var tempSettings = (AppSettingsSection)tempConfig.GetSection("appSettings");

                foreach (var k in testSettings.Settings.AllKeys) {
                    if (tempSettings.Settings.AllKeys.Any(x => x == k)) {
                        tempSettings.Settings.Remove(k);
                    }

                    tempSettings.Settings.Add(k, testSettings.Settings[k].Value);
                }
                tempConfig.Save();
            }

            TempFolder = temp;
            return temp;
        }
        #endregion

    }
}
