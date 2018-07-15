using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using Moq;
using Newtonsoft.Json.Linq;
using NHibernate;
using RadialReview;
using RadialReview.Accessors;
using RadialReview.Controllers;
using RadialReview.Hooks;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.UserModels;
using RadialReview.NHibernate;
using RadialReview.Utilities;
using RadialReview.Utilities.Productivity;
using RadialReview.Utilities.Synchronize;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.SessionState;
using TractionTools.Tests.Startup;
using TractionTools.Tests.Utilities;

namespace TractionTools.Tests.TestUtils {
	[TestClass]
	public class BaseTest {

		private static Dictionary<Guid, UserOrganizationModel> AdminUsers = new Dictionary<Guid, UserOrganizationModel>();
		private static Dictionary<Guid, OrganizationModel> AdminOrganizations = new Dictionary<Guid, OrganizationModel>();

		//protected static string AdminEmail = "admin" + Guid.NewGuid().ToString().Replace("-", "").ToLower() + "@admin.com";
		//protected static string AdminPassword = Guid.NewGuid().ToString().Replace("-", "").ToLower();
		protected static NHibernateUserManager UserManager = new NHibernateUserManager(new NHibernateUserStore());
		protected static async Task<UserOrganizationModel> GetAdminUser(Guid id) {

			if (AdminUsers.ContainsKey(id)) {
				return AdminUsers[id];
			}
			var username = "admin" + id.ToString().Replace("-", "").ToLower() + "@admin.com";
			var password = id.ToString().Replace("-", "").ToLower();
			MockHttpContext();
			var user = new UserModel() { UserName = username, FirstName = "Admin_FN", LastName = "Admin_LN" };
			var resultx = await UserManager.CreateAsync(user, password);
			bool genAdmin = false;
			DbCommit(s => {
				AdminOrganizations[id] = new OrganizationModel() {
					Name = new LocalizedStringModel("AdminOrg"),
				};

				AdminOrganizations[id].Settings.EnableL10 = true;
				AdminOrganizations[id].Settings.EnableReview = true;
				s.Save(AdminOrganizations[id]);

				AdminUsers[id] = new UserOrganizationModel() {
					IsRadialAdmin = true,
					User = user,
					Organization = AdminOrganizations[id],
					Cache = new UserLookup()
				};
				s.Save(AdminUsers[id]);
				AdminUsers[id].UpdateCache(s);
				user.UserOrganizationIds = AdminUsers[id].Id.AsList().ToArray();
				user.UserOrganizationCount = 1;
				user.CurrentRole = AdminUsers[id].Id;

				s.Save(new OrganizationTeamModel() { Name = "OrgTeam", Organization = AdminOrganizations[id], Type = TeamType.AllMembers });
				s.Save(new OrganizationTeamModel() { Name = "ManagerTeam", Organization = AdminOrganizations[id], Type = TeamType.Managers });

				user.IsRadialAdmin = true;
				s.Update(user);

				//Add an admin user to the database
				var admins = s.QueryOver<UserModel>().Where(x => x.UserName == "admin@admin.com").List().ToList();
				genAdmin = !admins.Any();



			});

			if (!AdminUsers.Any()) {

				DbCommit(s => {
					//s.Save(AdminOrganizations[id]);

					s.Save(new OrganizationTeamModel() { Name = "OrgTeam", Organization = AdminOrganizations[id], Type = TeamType.AllMembers });
					s.Save(new OrganizationTeamModel() { Name = "ManagerTeam", Organization = AdminOrganizations[id], Type = TeamType.Managers });

				});
			}


			await CreateGeneralAdmin();
			#region Create Admin in DB
			//if (genAdmin) {
			//    var um = new UserModel() { UserName = "admin@admin.com", FirstName = "admin", LastName = "account" };
			//    var resulty = await UserManager.CreateAsync(um, "admin");
			//    DbCommit(s => {
			//        var org = new OrganizationModel() {
			//            Name = new LocalizedStringModel("Admin Account"),
			//        };
			//        s.Save(org);
			//        var uo = new UserOrganizationModel() {
			//            IsRadialAdmin = true,
			//            User = um,
			//            Organization = org,
			//            Cache = new UserLookup()
			//        };
			//        org.Settings.EnableL10 = true;
			//        org.Settings.EnableReview = true;

			//        s.Save(uo);
			//        uo.UpdateCache(s);
			//        um.UserOrganizationIds = uo.Id.AsList().ToArray();
			//        um.UserOrganizationCount = 1;
			//        um.CurrentRole = uo.Id;
			//        um.IsRadialAdmin = true;
			//        s.Update(um);
			//    });
			//}
			#endregion

			return AdminUsers[id];
		}

		private static async Task CreateGeneralAdmin() {
			bool genAdmin = false;
			DbQuery(s => {

				var admins = s.QueryOver<UserModel>().Where(x => x.UserName == "admin@gmail.com").List().ToList();
				genAdmin = !admins.Any();
			});
			if (genAdmin) {
				var UserManager = new NHibernateUserManager(new NHibernateUserStore());
				var user = new UserModel() { UserName = "admin@gmail.com", FirstName = "Admin_FN", LastName = "Admin_LN" };
				var resultx = await UserManager.CreateAsync(user, "adminpass");
				DbCommit(s => {
					var org = new OrganizationModel() {
						Name = new LocalizedStringModel("AdminOrg"),
					};

					org.Settings.EnableL10 = true;
					org.Settings.EnableReview = true;
					s.Save(org);
					org.Organization = org;
					s.Update(org);
					var u = new UserOrganizationModel() {
						IsRadialAdmin = true,
						User = user,
						Organization = org,
						Cache = new UserLookup()
					};
					s.Save(u);
					u.UpdateCache(s);
					user.UserOrganizationIds = u.Id.AsList().ToArray();
					user.UserOrganizationCount = 1;
					user.CurrentRole = u.Id;

					s.Save(new OrganizationTeamModel() { Name = "OrgTeam", Organization = org, Type = TeamType.AllMembers });
					s.Save(new OrganizationTeamModel() { Name = "ManagerTeam", Organization = org, Type = TeamType.Managers });

					user.IsRadialAdmin = true;
					s.Update(user);
				});
			}
		}

		protected string GetBaseUrl(string after = null) {
			after = (after ?? "").TrimStart('/');

			return "https://localhost:44300/" + after;
		}

		private static bool ApplicationCreated;
		protected void MockApplication() {
			if (!ApplicationCreated)
				ApplicationAccessor.EnsureApplicationExists();
			ApplicationCreated = true;
		}


		public static void RemoveIsTest() {
			if (HttpContext.Current != null && HttpContext.Current.Items != null)
				HttpContext.Current.Items["IsTest"] = null;
		}
		public static void AddIsTest() {
			if (HttpContext.Current != null && HttpContext.Current.Items != null)
				HttpContext.Current.Items["IsTest"] = true;
		}

		public void MockNoSyncException() {
			MockHttpContext(true);
			HttpContext.Current.Items[SyncUtil.NO_SYNC_EXCEPTION] = true;
		}

		public static void MockHttpContext(Controller ctrl, bool isTest = true) {
			MockHttpContext(isTest);
			ctrl.ControllerContext = new ControllerContext() {
				HttpContext = new HttpContextWrapper(HttpContext.Current)
			};
		}

		public static void MockHttpContext(bool isTest = true) {
			if (HttpContext.Current == null) {
				HttpContext.Current = new HttpContext(new HttpRequest("", "http://fake.url", ""), new HttpResponse(HttpWriter.Null));

				var fakeIdentity = new GenericIdentity("TestUser");
				var principal = new GenericPrincipal(fakeIdentity, null);

				HttpContext.Current.User = principal;
				var browser = new HttpBrowserCapabilities() {
					Capabilities = new Dictionary<string, string>{
					{"majorversion", "8"},
					{"browser", "IE"},
					{"isMobileDevice","false"}
				}
				};
				HttpContext.Current.Request.Browser = browser;
				var data = new Dictionary<string, object>() { { "a", "b" } };

				HttpContext.Current.Items["owin.Environment"] = data;
			}
			HttpContext.Current.Items["IsTest"] = isTest ? (bool?)true : null;

		}
		public static async Task ThrowsAsync<T>(Func<Task> func, Action<T> onError = null) where T : Exception {

			var exceptionThrown = false;
			try {
				await func();
			} catch (T e) {
				exceptionThrown = true;
				if (onError != null)
					onError(e);
			}

			if (!exceptionThrown)
				throw new AssertFailedException(String.Format("An exception of type {0} was expected, but not thrown", typeof(T)));
		}

		public static T Throws<T>(Action func) where T : Exception {
			var exceptionThrown = false;
			T exception = null;
			try {
				func.Invoke();
			} catch (T e) {
				exception = e;
				exceptionThrown = true;
			}

			if (!exceptionThrown)
				throw new AssertFailedException(String.Format("An exception of type {0} was expected, but not thrown", typeof(T)));
			return exception;

		}

		public static void DbCommit(Action<ISession> sFunc, bool singleSession = false) {
			BaseTest.DbQuery(sFunc, true, singleSession);
		}
		//protected void DbCommit(Action<ISession> sFunc)
		//{
		//    BaseTest.DbExecute(sFunc, true);
		//}
		//protected void DbExecute(Action<ISession> sFunc, bool commit = false){
		//    BaseTest.DbExecute(sFunc, commit);
		//}

		public static void DbQuery(Action<ISession> sFunc, bool commit = false, bool singleSession = false) {
			//RemoveIsTest();
			using (var s = HibernateSession.GetCurrentSession(singleSession)) {
				using (var tx = s.BeginTransaction()) {
					if (s.Connection.ConnectionString != "Data Source=|DataDirectory|\\_testdb.db" && s.Connection.ConnectionString != "FullUri=file:memorydb.db?mode=memory&cache=shared")
						throw new Exception("ConnectionString must be 'Data Source=|DataDirectory|\\_testdb.db' or 'FullUri=file:memorydb.db?mode=memory&cache=shared'");

					sFunc(s);
					if (commit) {
						tx.Commit();
						s.Flush();
					}
				}

			}
		}

		public UserOrganizationModel GetCaller() {
			return new UserOrganizationModel() {
				IsRadialAdmin = true
			};
		}

		public static string GetTestName() {
			var stackTrace = new StackTrace();
			foreach (var stackFrame in stackTrace.GetFrames()) {
				MethodBase methodBase = stackFrame.GetMethod();
				Object[] attributes = methodBase.GetCustomAttributes(typeof(TestMethodAttribute), false);
				if (attributes.Length >= 1) {
					return methodBase.Name;
				}
			}
			Console.WriteLine(string.Join("\n", stackTrace.GetFrames().Select(x => x.GetMethod().Name)));
			throw new Exception("Not called from a test method");
			//return "Not called from a test method";
		}

		public static void MockController<T>(UserOrganizationModel caller, Action<T> f) where T : BaseController, new() {

			MockHttpContext();
			var ctrl = new T();
			ctrl.MockUser(caller);

			ctrl.ControllerContext = new ControllerContext(new HttpContextWrapper(HttpContext.Current), new RouteData(), ctrl);

			f(ctrl);


		}

		#region Directory Lookup
		public static long Timestamp = DateTime.UtcNow.ToJavascriptMilliseconds() / (1000 * 60);
		public static string ApplicationName { get; protected set; }
		public static string TestName { get; protected set; }
		protected static string TempFolder;

		public static string GetTempFile() {
			return Path.Combine("c:\\TractionTools-Tests\\", "");
		}

		public static string GetTestSolutionPath() {
			//var solutionFolder = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)));
			//var testPath = Path.Combine(solutionFolder, TestName);

			var solutionFolder = "c:\\TractionTools\\";// Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)));
			var testPath = Path.Combine(solutionFolder, TestName);
			return testPath;
		}

		public static string GetTractionToolsSolutionPath() {
			var solutionFolder = "c:\\TractionTools\\";// Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)));
			var solutionPath = Path.Combine(solutionFolder, ApplicationName);
			return solutionPath;
		}

		public static string GetShortTestId() {
			var n = ("" + Timestamp);
			return n.Substring(0, 4) + "-" + n.Substring(4);
		}
		public static string GetPdfFolder(string subdir = null) {
			var folder = GetBasePdfFolder();
			folder = Path.Combine(folder, GetShortTestId());

			if (subdir != null) {
				folder = Path.Combine(folder, subdir);
			}
			Directory.CreateDirectory(folder);

			return folder;
		}
		public static string GetBasePdfFolder() {
			var folder = Path.Combine(GetTempFile(), "pdfs");
			Directory.CreateDirectory(folder);
			return folder;
		}
		public static string GetCurrentPdfFolder() {
			var folder = GetBasePdfFolder();
			folder = Path.Combine(folder, "current");
			Directory.CreateDirectory(folder);
			return folder;
		}


		public static string GetScreenshotFolder(string subdir = null) {
			var folder = Path.Combine(GetTempFile(), "screenshots");

			folder = Path.Combine(folder, GetShortTestId());

			if (subdir != null) {
				folder = Path.Combine(folder, subdir);
			}
			Directory.CreateDirectory(folder);

			return folder;
		}

		protected static string GetApplicationPath() {
			//var solutionFolder = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)));
			var solutionPath = GetTractionToolsSolutionPath();// Path.Combine(solutionFolder, applicationName);
			var testPath = GetTestSolutionPath();// Path.Combine(solutionFolder, testName);

			var temp = Path.Combine(Path.Combine(GetTempFile(), "ServerFiles"), Guid.NewGuid().ToString());
			Directory.CreateDirectory(temp);

			FileUtility.DirectoryCopy(solutionPath, temp, true, new string[] { "\\obj\\" });

			var testConfigFileMap = new ExeConfigurationFileMap() { ExeConfigFilename = Path.Combine(testPath, "app.config") };
			var testConfig = ConfigurationManager.OpenMappedExeConfiguration(testConfigFileMap, ConfigurationUserLevel.None);
			var testSettings = (AppSettingsSection)testConfig.GetSection("appSettings");

			if (!testSettings.Settings.AllKeys.Any())
				throw new Exception("App.config was empty. Cannot correctly remap Web.config.");

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

		[TestInitialize]
		public void BaseTestInitialize() {
			SetupAssemblyInitializer.ReconfigureSqlite();
			HooksRegistry.Deregister();

		}
		#endregion

		[Obsolete("dont use", true)]
		public void CompareModelProperties(string expected, object actual) {
			throw new Exception("Use BaseApiTest's CompareModelProperties.");
		}


		protected void Save(Document doc, string name) {
			PdfDocumentRenderer renderer = new PdfDocumentRenderer(true);
			renderer.Document = doc;
			renderer.RenderDocument();
			renderer.PdfDocument.Save(Path.Combine(GetCurrentPdfFolder(), name));
			renderer.PdfDocument.Save(Path.Combine(GetPdfFolder(), name));
		}

	}
	public static class TestObjectExtensions {
		public class Getter<T> {

			private PropertyInfo prop;
			private FieldInfo field;
			private T obj;
			public Getter(T obj, PropertyInfo prop) {
				if (prop == null)
					throw new ArgumentNullException("prop", "PropertyInfo was null");
				this.prop = prop;
				this.obj = obj;
			}
			public Getter(T obj, FieldInfo field) {
				if (field == null)
					throw new ArgumentNullException("field", "FieldInfo was null");
				this.field = field;
				this.obj = obj;
			}
			public TRef Get<TRef>() {
				if (prop != null)
					return (TRef)prop.GetValue(obj);
				else if (field != null)
					return (TRef)field.GetValue(obj);
				throw new Exception("Both null?");
			}
		}
		public class Setter<T> {

			private PropertyInfo prop;
			private FieldInfo field;
			private T obj;
			public Setter(T obj, PropertyInfo prop) {
				if (prop == null)
					throw new ArgumentNullException("prop", "PropertyInfo was null");
				this.prop = prop;
				this.obj = obj;
			}
			public Setter(T obj, FieldInfo field) {
				if (field == null)
					throw new ArgumentNullException("field", "FieldInfo was null");
				this.field = field;
				this.obj = obj;
			}
			public void Set<TRef>(TRef value) {
				if (prop != null)
					prop.SetValue(obj, value);
				else if (field != null)
					field.SetValue(obj, value);
			}
		}
		private static Setter<T> GetSetter<T>(this T obj, string propertyName) {
			Setter<T> propInfo = null;
			var type = obj.GetType();
			do {
				var prop = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (prop != null)
					propInfo = new Setter<T>(obj, prop);
				else {
					var field = type.GetField(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (field != null)
						propInfo = new Setter<T>(obj, field);
				}
				//propInfo = type.getfie
				type = type.BaseType;
			}
			while (propInfo == null && type != null);
			return propInfo;
		}
		private static Getter<T> GetGetter<T>(this T obj, string propertyName) {
			Getter<T> propInfo = null;
			var type = obj.GetType();
			do {
				var prop = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (prop != null)
					propInfo = new Getter<T>(obj, prop);
				else {
					var field = type.GetField(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (field != null)
						propInfo = new Getter<T>(obj, field);
				}
				//propInfo = type.getfie
				type = type.BaseType;
			}
			while (propInfo == null && type != null);
			return propInfo;
		}
		public static void SetValue<T, TRef>(this T obj, string field, TRef value) {
			var setter = GetSetter(obj, field);
			setter.Set(value);
		}
		public static TRef GetValue<T, TRef>(this T obj, string field) {
			var getter = GetGetter(obj, field);
			return getter.Get<TRef>();
		}

	}
}
