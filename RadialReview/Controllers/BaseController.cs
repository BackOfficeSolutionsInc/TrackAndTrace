using System.Collections.Specialized;
using System.IO;
using System.Linq.Expressions;
using Microsoft.Ajax.Utilities;
using MigraDoc.Rendering;
using PdfSharp.Pdf;
using RadialReview.Accessors;
using RadialReview.Exceptions.MeetingExceptions;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using RadialReview.Exceptions;
using System.Web.Routing;
using RadialReview.Models.Angular;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.UserModels;
using RadialReview.Properties;
using log4net;
using System.Threading;
using Microsoft.Owin.Security;
using System.Reflection;
using RadialReview.Models.Json;
using RadialReview.Utilities.Attributes;
using NHibernate;
using RadialReview.Engines;
using System.Configuration;
using RadialReview.Utilities;
using System.Web.Security;
using System.Security.Principal;
using RadialReview.Utilities.Extensions;
using RadialReview.Utilities.Serializers;
using System.Text;
using NHibernate.Context;
using MigraDoc.DocumentObjectModel;
using System.IO.Compression;
using RadialReview.Models.Enums;
using RadialReview.Utilities.Productivity;
using PdfSharp.Drawing;
using System.Drawing.Text;
using RadialReview.NHibernate;
using System.Threading.Tasks;
using System.Net.Mime;
using System.Text.RegularExpressions;
using SpreadsheetLight;
using RadialReview.Hooks;

namespace RadialReview.Controllers {
	public class UserManagementController : BaseController {

		public UserManagementController() : this(new NHibernateUserManager(new NHibernateUserStore())) //this(new UserManager<ApplicationUser>(new NHibernateUserStore<UserModel>(new ApplicationDbContext())))
		{
		}

		protected UserManagementController(NHibernateUserManager userManager) {
			UserManager = userManager;
		}

		protected async Task SignInAsync(UserModel user, bool isPersistent = false) {
			AuthenticationManager.SignOut(DefaultAuthenticationTypes.ExternalCookie);
			var identity = await UserManager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie);
			AuthenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = isPersistent }, identity);
		}

		public NHibernateUserManager UserManager { get; protected set; }
	}

	public class BaseController : Controller {
		#region Helpers
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		#region ViewBag

		protected void ShowAlert(string status, AlertType type = AlertType.Info) {
			switch (type) {
				case AlertType.Info:
					ViewBag.InfoAlert = status;
					break;
				case AlertType.Error:
					ViewBag.Alert = status;
					break;
				case AlertType.Success:
					ViewBag.Success = status;
					break;
				default:
					throw new ArgumentOutOfRangeException("AlertType:" + type);
			}
		}
		#endregion
		#region Engines
		protected static UserEngine _UserEngine = new UserEngine();
		protected static ChartsEngine _ChartsEngine = new ChartsEngine();
		protected static ReviewEngine _ReviewEngine = new ReviewEngine();
		#endregion
		#region Accessors
		protected static RockAccessor _RockAccessor = new RockAccessor();
		protected static RoleAccessor _RoleAccessor = new RoleAccessor();
		protected static UserAccessor _UserAccessor = new UserAccessor();
		protected static TaskAccessor _TaskAccessor = new TaskAccessor();
		protected static TeamAccessor _TeamAccessor = new TeamAccessor();
		protected static NexusAccessor _NexusAccessor = new NexusAccessor();
		protected static ImageAccessor _ImageAccessor = new ImageAccessor();
		protected static GroupAccessor _GroupAccessor = new GroupAccessor();
		protected static OriginAccessor _OriginAccessor = new OriginAccessor();
		protected static ReviewAccessor _ReviewAccessor = new ReviewAccessor();
		protected static AskableAccessor _AskableAccessor = new AskableAccessor();
		protected static PaymentAccessor _PaymentAccessor = new PaymentAccessor();
		protected static KeyValueAccessor _KeyValueAccessor = new KeyValueAccessor();
		protected static PositionAccessor _PositionAccessor = new PositionAccessor();
		protected static QuestionAccessor _QuestionAccessor = new QuestionAccessor();
		protected static CategoryAccessor _CategoryAccessor = new CategoryAccessor();
		protected static PrereviewAccessor _PrereviewAccessor = new PrereviewAccessor();
		protected static ScorecardAccessor _ScorecardAccessor = new ScorecardAccessor();
		protected static PermissionsAccessor _PermissionsAccessor = new PermissionsAccessor();
		protected static OrganizationAccessor _OrganizationAccessor = new OrganizationAccessor();
		//protected static DeepSubordianteAccessor _DeepSubordianteAccessor = new DeepSubordianteAccessor();
		protected static ResponsibilitiesAccessor _ResponsibilitiesAccessor = new ResponsibilitiesAccessor();
		#endregion
		#region GetUserModel
		protected UserModel GetUserModel(bool styles = false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return GetUserModel(s, styles);
				}
			}
		}

		protected UserModel GetUserModel(ISession s, bool styles = false) {
			return new Cache().GetOrGenerate(CacheKeys.USER + "~" + styles, x => {
				x.LifeTime = LifeTime.Request/*Session*/;
				var id = User.Identity.GetUserId();

				var user = _UserAccessor.GetUserById(s, id);
				if (styles) {
					user._StylesSettings = s.Get<UserStyleSettings>(id);
				}

				return user;

			});
		}
		#endregion
		#region GetUser
		//private long? _CurrentUserOrganizationId = null;

		private UserOrganizationModel MockUser = null;
		private bool TransformAngular = true;

		protected string[] AllAdmins = new[] {
			""
		};


		private UserOrganizationModel PopulateUserData(UserOrganizationModel user) {
			if (user != null && Request != null) {
				user._IsRadialAdmin = user.IsRadialAdmin;

				user._ClientTimestamp = Request.Params.Get("_clientTimestamp").TryParseLong();
				user._ClientOffset = Request.Params.Get("_tz").TryParseInt();

				HookData.SetData("ConnectionId", Request.Params.Get("connectionId"));
				HookData.SetData("ClientTimestamp", user._ClientTimestamp);
				HookData.SetData("ClientTimezone", user._ClientOffset);

				if (user._ClientTimestamp != null && user._ClientOffset==null) {
					var diff = (int)(Math.Round((user._ClientTimestamp.Value.ToDateTime()-DateTime.UtcNow).TotalMinutes / 30.0) * 30.0);
					user._ClientOffset = diff;// Thread.SetData(Thread.GetNamedDataSlot("timeOffset"), diff);
				}
			}
			return user;
		}
		public UserOrganizationModel GetUser() {
			return PopulateUserData(_GetUser());
		}
		public UserOrganizationModel GetUser(ISession s) {
			return PopulateUserData(_GetUser(s));
		}
		public UserOrganizationModel GetUser(long userOrganizationId) {
			return PopulateUserData(_GetUser(userOrganizationId));
		}
		private UserOrganizationModel GetUserOrganization(ISession s, long userOrganizationId, String redirectUrl)//, Boolean full = false)
		{
			var cache = new Cache();

			return cache.GetOrGenerate(CacheKeys.USERORGANIZATION, x => {
				var id = User.Identity.GetUserId();
				var found = _UserAccessor.GetUserOrganizations(s, id, userOrganizationId, redirectUrl);
				if (found != null && found.User != null && !cache.Contains(CacheKeys.USER)) {
					cache.Push(CacheKeys.USER, found.User, LifeTime.Request/*Session*/);
				}
				x.LifeTime = LifeTime.Request/*Session*/;
				return found;
			}, x => x.Id != userOrganizationId);
		}

		// ReSharper disable once RedundantOverload.Local
		private UserOrganizationModel _GetUser(ISession s) {
			if (MockUser != null)
				return MockUser;

			return _GetUser(s, null);
		}
		private UserOrganizationModel _GetUser() {
			if (MockUser != null)
				return MockUser;
			long? userOrganizationId = null;

			if (userOrganizationId == null) {
				var orgIdParam = Request.Params.Get("setUserOrganizationId");
				if (orgIdParam != null)
					userOrganizationId = long.Parse(orgIdParam);
			}

			var cache = new Cache();

			if (userOrganizationId == null && cache.Get(CacheKeys.USERORGANIZATION_ID) is long) {
				userOrganizationId = (long)cache.Get(CacheKeys.USERORGANIZATION_ID);
			}



			if (cache.Get(CacheKeys.USERORGANIZATION) is UserOrganizationModel && userOrganizationId == ((UserOrganizationModel)cache.Get(CacheKeys.USERORGANIZATION)).Id)
				return (UserOrganizationModel)cache.Get(CacheKeys.USERORGANIZATION);

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return _GetUser(s, null);
				}
			}
		}
		private UserOrganizationModel _GetUser(long userOrganizationId) {
			if (MockUser != null && MockUser.Id == userOrganizationId)
				return MockUser;
			var cache = new Cache();

			if (cache.Get(CacheKeys.USERORGANIZATION) is UserOrganizationModel && userOrganizationId == ((UserOrganizationModel)cache.Get(CacheKeys.USERORGANIZATION)).Id)
				return (UserOrganizationModel)cache.Get(CacheKeys.USERORGANIZATION);

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return _GetUser(s, userOrganizationId);
				}
			}
		}
		private UserOrganizationModel _GetUser(ISession s, long? userOrganizationId = null)//long? organizationId, Boolean full = false)
		{

			/**/
			if (userOrganizationId == null) {
				try {
					var orgIdParam = Request.Params.Get("setUserOrganizationId");
					if (orgIdParam != null)
						userOrganizationId = long.Parse(orgIdParam);
				} catch (Exception) {
					//var o = false;
				}
			}
			var cache = new Cache();

			if (userOrganizationId == null && cache.Get(CacheKeys.USERORGANIZATION_ID) != null) {
				userOrganizationId = (long)cache.Get(CacheKeys.USERORGANIZATION_ID);
			}
			if (userOrganizationId == null) {
				userOrganizationId = GetUserModel(s).GetCurrentRole();
			}


			var user = cache.Get(CacheKeys.USERORGANIZATION);

			if (user is UserOrganizationModel && userOrganizationId == ((UserOrganizationModel)user).Id)
				return (UserOrganizationModel)user;

			if (userOrganizationId == null) {
				var returnPath = Server.HtmlEncode(Request.Path);

				var found = GetUserOrganizations(s, returnPath);
				if (found.Count() == 0)
					throw new NoUserOrganizationException();
				else if (found.Count() == 1) {
					var uo = found.First();
					if (uo.User != null) {
						uo.User.CurrentRole = uo.Id;
						s.Update(uo.User);
					}
					//_CurrentUserOrganizationId = uo.Id;
					cache.Push(CacheKeys.USERORGANIZATION, uo, LifeTime.Request/*Session*/);
					cache.Push(CacheKeys.USERORGANIZATION_ID, uo.Id, LifeTime.Request/*Session*/);
					return uo;
				} else
					throw new OrganizationIdException(Request.Url.PathAndQuery);
			} else {
				var uo = GetUserOrganization(s, userOrganizationId.Value, Request.Url.PathAndQuery);
				//_CurrentUserOrganizationId = uo.Id;
				cache.Push(CacheKeys.USERORGANIZATION, uo, LifeTime.Request/*Session*/);
				cache.Push(CacheKeys.USERORGANIZATION_ID, userOrganizationId.Value, LifeTime.Request/*Session*/);
				return uo;

			}
		}

		protected List<UserOrganizationModel> GetUserOrganizations(String redirectUrl) //Boolean full = false)
		{
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return GetUserOrganizations(s, redirectUrl);
				}
			}
		}
		protected int GetUserOrganizationCounts(ISession s, String redirectUrl)//Boolean full = false)
		{
			var id = User.Identity.GetUserId();
			return _UserAccessor.GetUserOrganizationCounts(s, id, redirectUrl/*, full*/);
		}
		protected List<UserOrganizationModel> GetUserOrganizations(ISession s, String redirectUrl)//Boolean full = false)
		{
			var id = User.Identity.GetUserId();
			return _UserAccessor.GetUserOrganizations(s, id, redirectUrl/*, full*/);
		}
		#endregion
		#region Validation
		private List<String> ToValidate = new List<string>();
		private NameValueCollection ValidationCollection;
		private bool SkipValidation = false;
		protected void ValidateValues<T>(T model, params Expression<Func<T, object>>[] selectors) {
			if (SkipValidation)
				return;
			foreach (var e in selectors) {
				//var meta = ModelMetadata.FromLambdaExpression(e, new ViewDataDictionary<T>());
				var name = e.GetMvcName();//meta.DisplayName;
				if (!ToValidate.Remove(name))
					throw new PermissionsException("Validation item does not exist.");
				SecuredValueValidator.ValidateValue(ValidationCollection, name);
			}
		}
		#endregion

		protected void ManagerAndCanEditOrException(UserOrganizationModel user) {
			if (!user.IsManagerCanEditOrganization())
				throw new PermissionsException();
		}
		protected ActionResult RedirectToLocal(string returnUrl) {
			if (Url.IsLocalUrl(returnUrl))
				return Redirect(returnUrl);
			else
				throw new RedirectException("Return URL is invalid.");
		}

		private static string CleanFileName(string fileName) {
			return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
		}
        protected ActionResult Xls(SLDocument document, string name = null) {

            name = name ?? ("export_" + DateTime.UtcNow.ToJavascriptMilliseconds());
            //name = name;

            Response.Clear();
            Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            Response.AddHeader("Content-Disposition", "attachment; filename="+name+".xlsx");
            document.SaveAs(Response.OutputStream);
            Response.End();
            return new EmptyResult();            
        }

        protected ActionResult Pdf(PdfDocument document, string name = null, bool inline = true) {
			//var stream = new MemoryStream();
			////try {
			////	document.Save("C:\\Users\\Clay\\Desktop\\Stuff\\doc.pdf");
			////} catch (Exception e) {
			////	int a = 0;
			////}
			//document.Save(stream, false);
			//name = name ?? (Guid.NewGuid() + ".pdf");
			////if (inline){
			////    Response.AppendHeader("content-disposition", "inline; filename=\""+name+"\"");
			////}
			////stream.Seek(0, SeekOrigin.Begin);
			////stream.Position = 0;

			////Response.ContentEncoding = Encoding.UTF8;
			////return new FileStreamResult(stream, MediaTypeNames.Application.Pdf);
			//stream.Seek(0, SeekOrigin.Begin);

			//Response.ContentType = "application/pdf";
			//Response.AddHeader("Content-Disposition", "attachment;filename=" + name);
			//Response.Buffer = true;
			//Response.Clear();
			//Response.OutputStream.Write(stream.GetBuffer(), 0, stream.GetBuffer().Length);
			//Response.OutputStream.Flush();
			//Response.End();

			//return new FileStreamResult(Response.OutputStream, "application/pdf");

			//XPdfFontOptions opt = new XPdfFontOptions(PdfFontEmbedding.Default);
			name = name ?? document.Info.Title;

			MemoryStream stream = new MemoryStream();
			document.Save(stream, false);
			Response.Clear();
			Response.ContentType = "application/pdf";
			if (name != null) {
				if (!name.ToLower().EndsWith(".pdf"))
					name += ".pdf";
				name = CleanFileName(name);
				Response.AddHeader("Content-Disposition", "filename=\"" + name + "\"");
			}
			Response.AddHeader("content-length", stream.Length.ToString());
			Response.BinaryWrite(stream.ToArray());
			//Response.Flush();
			stream.Close();
			//Response.End();
			//var pdfRenderer = new PdfDocumentRenderer(true, PdfFontEmbedding.None);
			//pdfRenderer.PdfDocument = document;
			//pdfRenderer.DocumentRenderer = new DocumentRenderer(document) { PrivateFonts = pfc };
			//pdfRenderer.RenderDocument();

			//var stream = new MemoryStream();
			//pdfRenderer.Save(stream, false);
			//name = name ?? (Guid.NewGuid() + ".pdf");
			////var file = File(stream, System.Net.Mime.MediaTypeNames.Application.Pdf, name);
			//if (inline) {
			//	//Response.Headers.Remove("Content-Disposition");
			//	//var f = Response.Filter;
			//	//Response.Filter = null;
			//	//if (Response.Headers.AllKeys.Any(x => x == "Content-Encoding"))
			//	//    Response.Headers.Remove("Content-Encoding");
			//	//Response.AppendHeader("Content-Disposition", "inline; filename=output.pdf");
			//	//Response.AppendHeader("Cache-Control", "private");

			//}
			return new EmptyResult();
			//new ActionResult(stream, System.Net.Mime.MediaTypeNames.Application.Pdf);



			//return File(stream, System.Net.Mime.MediaTypeNames.Application.Pdf, name);
		}
		private static string MappedAppPath() {
			string APP_PATH = System.Web.HttpContext.Current.Request.ApplicationPath.ToLower();
			if (APP_PATH == "/") //a site 
				APP_PATH = "/";
			else if (!APP_PATH.EndsWith(@"/")) //a virtual 
				APP_PATH += @"/";

			string it = System.Web.HttpContext.Current.Server.MapPath(APP_PATH);
			if (!it.EndsWith(@"\"))
				it += @"\";
			return it;
		}

		protected FileResult Pdf(Document document, string name = null, bool inline = true) {
#pragma warning disable CS0618 // Type or member is obsolete
            var pdfRenderer = new PdfDocumentRenderer(true, PdfFontEmbedding.None);
#pragma warning restore CS0618 // Type or member is obsolete

            pdfRenderer.Document = document;

			pdfRenderer.RenderDocument();
			if (pdfRenderer.PageCount == 0) {
				var doc = new Document();
				var s = doc.AddSection();
				s.AddParagraph("No pages.");
				return Pdf(doc, name, inline);
			}


			var stream = new MemoryStream();

			pdfRenderer.Save(stream, false);
			name = name ?? (Guid.NewGuid() + ".pdf");
			if (inline) {
				//Response.Headers.Remove("Content-Disposition");
				//var f = Response.Filter;
				//Response.Filter = null;
				//if (Response.Headers.AllKeys.Any(x => x == "Content-Encoding"))
				//    Response.Headers.Remove("Content-Encoding");
				//Response.AppendHeader("Content-Disposition", "inline; filename=output.pdf");
				//Response.AppendHeader("Cache-Control", "private");

			}
			return new FileStreamResult(stream, System.Net.Mime.MediaTypeNames.Application.Pdf);
			//return file;
		}


		#endregion
		#region User Status
		protected void SignOut() {
			new Cache().Invalidate(CacheKeys.USERORGANIZATION_ID);
			AuthenticationManager.SignOut();
			FormsAuthentication.SignOut();
			HttpContext.User = new GenericPrincipal(new GenericIdentity(string.Empty), null);
			Session.Clear();
		}
		protected bool IsLoggedIn() {
			return User.Identity.GetUserId() != null;
		}
		public IAuthenticationManager AuthenticationManager {
			get {
				return HttpContext.GetOwinContext().Authentication;
			}
		}
		#endregion
		#region Overrides
		private static MethodInfo GetActionMethod(ExceptionContext filterContext) {
			Type controllerType = filterContext.Controller.GetType();// Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(x => x.Name == requestContext.RouteData.Values["controller"].ToString());
			ControllerContext controllerContext = new ControllerContext(filterContext.RequestContext, Activator.CreateInstance(controllerType) as ControllerBase);
			ControllerDescriptor controllerDescriptor = new ReflectedControllerDescriptor(controllerType);
			ActionDescriptor actionDescriptor = controllerDescriptor.FindAction(controllerContext, controllerContext.RouteData.Values["action"].ToString());
			return (actionDescriptor as ReflectedActionDescriptor).MethodInfo;
		}

		protected override void OnException(ExceptionContext filterContext) {
			try {
				ChromeExtensionComms.SendCommand("pageError");
				var f = filterContext.HttpContext.Response.Filter;
				filterContext.HttpContext.Response.Filter = null;
				if (filterContext.HttpContext.Response.Headers.AllKeys.Any(x => x == "Content-Encoding"))
					filterContext.HttpContext.Response.Headers.Remove("Content-Encoding");
				var action = GetActionMethod(filterContext);

				var isJsonResult = typeof(JsonResult).IsAssignableFrom(action.ReturnType);
				isJsonResult = isJsonResult || (typeof(Task<JsonResult>)).IsAssignableFrom(action.ReturnType);

				if (isJsonResult) {
					var exception = new ResultObject(filterContext.Exception);
					if (filterContext.Exception is RedirectException) {
						var re = ((RedirectException)filterContext.Exception);
						if (re.Silent != null)
							exception.Silent = re.Silent.Value;

						exception.NoErrorReport = re.NoErrorReport;

						if (re.ForceReload)
							exception.Refresh = true;
					}

					filterContext.ExceptionHandled = true;
					filterContext.HttpContext.Response.Clear();
					filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
					filterContext.HttpContext.Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
					filterContext.Result = new JsonResult() { Data = exception, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
					return;
				}


				if (filterContext.ExceptionHandled)
					return;

				if (filterContext.Exception is LoginException) {
					SignOut();
					var redirectUrl = ((RedirectException)filterContext.Exception).RedirectUrl;
					if (redirectUrl == null)
						redirectUrl = Request.Url.PathAndQuery;
					log.Info("Login: [" + Request.Url.PathAndQuery + "] --> [" + redirectUrl + "]");
					filterContext.Result = RedirectToAction("Login", "Account", new { area = "", message = filterContext.Exception.Message, returnUrl = redirectUrl });
					filterContext.ExceptionHandled = true;
					filterContext.HttpContext.Response.Clear();
				} else if (filterContext.Exception is OrganizationIdException) {
					var redirectUrl = ((RedirectException)filterContext.Exception).RedirectUrl;
					log.Info("Organization: [" + Request.Url.PathAndQuery + "] --> [" + redirectUrl + "]");
					filterContext.Result = RedirectToAction("Role", "Account", new { area = "", message = filterContext.Exception.Message, returnUrl = redirectUrl });
					filterContext.ExceptionHandled = true;
					filterContext.HttpContext.Response.Clear();
				} else if (filterContext.Exception is PermissionsException) {
					filterContext.HttpContext.Response.Clear();
					var returnUrl = ((RedirectException)filterContext.Exception).RedirectUrl;
					log.Info("Permissions: [" + Request.Url.PathAndQuery + "] --> [" + returnUrl + "]");
					ViewBag.Message = filterContext.Exception.Message;
					if (typeof(PartialViewResult).IsAssignableFrom(action.ReturnType)) {
						filterContext.Result = PartialView("~/Views/Error/Index.cshtml", filterContext.Exception);
					} else {
						filterContext.Result = View("~/Views/Error/Index.cshtml", filterContext.Exception);
					}
					filterContext.ExceptionHandled = true;
				} else if (filterContext.Exception is MeetingException) {
					var type = ((MeetingException)filterContext.Exception).MeetingExceptionType;
					log.Info("MeetingException: [" + Request.Url.PathAndQuery + "] --> [" + type + "]");
					filterContext.Result = RedirectToAction("ErrorMessage", "L10", new { area = "", message = filterContext.Exception.Message, type });
					filterContext.ExceptionHandled = true;
					filterContext.HttpContext.Response.Clear();
				} else if (filterContext.Exception is RedirectException) {
					var returnUrl = ((RedirectException)filterContext.Exception).RedirectUrl;
					log.Info("Redirect: [" + Request.Url.PathAndQuery + "] --> [" + returnUrl + "]");
					filterContext.Result = RedirectToAction("Index", "Error", new { area = "", message = filterContext.Exception.Message, returnUrl = returnUrl });
					filterContext.ExceptionHandled = true;
					filterContext.HttpContext.Response.Clear();
				} else if (filterContext.Exception is HttpAntiForgeryException) {
					log.Info("AntiForgery: [" + Request.Url.PathAndQuery + "] --> []");
					filterContext.Result = RedirectToAction("Login", "Account", new { area = "", message = "Session Timeout. Please try again." /*filterContext.Exception.Message*/ });
					filterContext.ExceptionHandled = true;
					filterContext.HttpContext.Response.Clear();
				} else {
					log.Error("Error: [" + Request.Url.PathAndQuery + "]<<" + filterContext.Exception.Message + ">>", filterContext.Exception);
					base.OnException(filterContext);
				}
			} catch (Exception e) {
				log.Info("OnException(Exception)", e);
				filterContext.Result = Content(e.Message + "  " + e.StackTrace);
			}
		}

		protected void CompressContent(ActionExecutedContext filterContext) {
			var encodingsAccepted = filterContext.HttpContext.Request.Headers["Accept-Encoding"];
			var contentType = filterContext.HttpContext.Request.Headers["Content-Type"];
			if (filterContext.IsChildAction)
				return;

			if (string.IsNullOrEmpty(encodingsAccepted))
				return;
            if (contentType != null && contentType.ToLower().Contains("pdf"))
                return;
            if (contentType != null && contentType.ToLower()=="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                return;

            try {
			    encodingsAccepted = encodingsAccepted.ToLowerInvariant();
			    var response = filterContext.HttpContext.Response;
                if (encodingsAccepted.Contains("deflate")) {
                    Response.Headers.Remove("Content-Encoding");
                    response.AppendHeader("Content-Encoding", "deflate");
                    response.Filter = new DeflateStream(response.Filter, CompressionMode.Compress);
                } else if (encodingsAccepted.Contains("gzip")) {
                    Response.Headers.Remove("Content-Encoding");
                    response.AppendHeader("Content-Encoding", "gzip");
                    response.Filter = new GZipStream(response.Filter, CompressionMode.Compress);
                }
            } catch (Exception) {
                //I guess just eat it..
            }
		}

		protected override void OnActionExecuting(ActionExecutingContext filterContext) {
			try {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						//Secure hidden fields
						ValidationCollection = filterContext.RequestContext.HttpContext.Request.Form;
						foreach (var f in ValidationCollection.AllKeys) {
							if (f != null && f.EndsWith(SecuredValueFieldNameComputer.NameSuffix)) {
								ToValidate.Add(f.Substring(0, f.Length - SecuredValueFieldNameComputer.NameSuffix.Length));
							}
						}
						//Access Level Filtering
						var accessAttributes = filterContext.ActionDescriptor.GetCustomAttributes(typeof(AccessAttribute), false).Cast<AccessAttribute>();
						if (accessAttributes.Count() == 0)
							throw new NotImplementedException("Access attribute missing.");

						switch ((AccessLevel)accessAttributes.Min(x => (int)x.AccessLevel)) {
							case AccessLevel.SignedOut: {
									if (Request.IsAuthenticated) {
										SignOut();
									}
								}
								break;
							case AccessLevel.Any:
								break;
							case AccessLevel.User:
								GetUserModel(s);
								break;
							case AccessLevel.UserOrganization:
								var u1 = GetUser(s);
								if (u1.DeleteTime != null)
									throw new PermissionsException("You're no longer attached to this organization.");
								if (u1.Organization.DeleteTime != null)
									throw new PermissionsException("This organization no longer exists.");
								break;
							case AccessLevel.Manager:
								var u2 = GetUser(s);
								if (u2.DeleteTime != null)
									throw new PermissionsException("You're no longer attached to this organization.");
								if (u2.Organization.DeleteTime != null)
									throw new PermissionsException("This organization no longer exists.");
								if (!u2.IsManager())
									throw new PermissionsException("You must be a " + Config.ManagerName() + " to view this resource.");
								break;
							case AccessLevel.Radial:
								if (!(GetUserModel(s).IsRadialAdmin || GetUser(s).IsRadialAdmin))
									throw new PermissionsException("You do not have access to this resource.");
								break;
							default:
								throw new Exception("Unknown Access Type");
						}

						// eat the cookie (if any) and set the culture
						//if (Request.Cookies["lang"] != null) {
						//    HttpCookie cookie = Request.Cookies["lang"];
						//    string lang = cookie.Value;
						//    var culture = new System.Globalization.CultureInfo(lang);
						//    Thread.CurrentThread.CurrentCulture = culture;
						//    Thread.CurrentThread.CurrentUICulture = culture;
						//}

						filterContext.Controller.ViewBag.IsLocal = Config.IsLocal();
						filterContext.Controller.ViewBag.HasBaseController = true;
						filterContext.Controller.ViewBag.AppVersion = GetAppVersion();
						filterContext.Controller.ViewBag.IsRadialAdmin = false;

						if (IsLoggedIn()) {
							var userOrgsCount = GetUserOrganizationCounts(s, Request.Url.PathAndQuery);
							UserOrganizationModel oneUser = null;
							try {
								oneUser = GetUser(s);
								if (oneUser == null)
									throw new NoUserOrganizationException();

#pragma warning disable CS0618 // Type or member is obsolete
								var lu = s.Get<UserLookup>(oneUser.Cache.Id);
#pragma warning restore CS0618 // Type or member is obsolete
								var isRadialAdmin = GetUserModel(s).IsRadialAdmin;
								oneUser._IsRadialAdmin = oneUser._IsRadialAdmin || isRadialAdmin;

								Thread.SetData(Thread.GetNamedDataSlot("IsRadialAdmin"), oneUser._IsRadialAdmin);
								filterContext.Controller.ViewBag.IsRadialAdmin = oneUser._IsRadialAdmin;

								if (!oneUser._IsRadialAdmin) {
									lu.LastLogin = DateTime.UtcNow;
									s.Update(lu);

									var ol = s.QueryOver<OrganizationLookup>().Where(x => x.OrgId == oneUser.Organization.Id).Take(1).SingleOrDefault();
									if (ol == null) {
										ol = new OrganizationLookup() {
											OrgId = oneUser.Organization.Id,
											CreateTime = oneUser.Organization.CreationTime
										};
									}
									ol.LastUserLogin = oneUser.Id;
									ol.LastUserLoginTime = lu.LastLogin.Value;
									s.SaveOrUpdate(ol);


								}
							} catch (OrganizationIdException) {
							} catch (NoUserOrganizationException) {
							}

							filterContext.Controller.ViewBag.UserName = MessageStrings.User;
							filterContext.Controller.ViewBag.UserImage = "/img/placeholder";
							filterContext.Controller.ViewBag.UserInitials = "";
							filterContext.Controller.ViewBag.UserColor = 0;
							filterContext.Controller.ViewBag.IsManager = false;
							filterContext.Controller.ViewBag.ShowL10 = false;
							filterContext.Controller.ViewBag.ShowReview = false;
							filterContext.Controller.ViewBag.ShowSurvey = false;
							filterContext.Controller.ViewBag.ShowPeople = false;
							filterContext.Controller.ViewBag.Organizations = userOrgsCount;
							filterContext.Controller.ViewBag.Hints = true;
							filterContext.Controller.ViewBag.ManagingOrganization = false;
							filterContext.Controller.ViewBag.Organization = null;
							filterContext.Controller.ViewBag.UserId = 0L;
							filterContext.Controller.ViewBag.ConsoleLog = false;
							filterContext.Controller.ViewBag.LimitFiveState = true;
							filterContext.Controller.ViewBag.ShowAC = false;
                            filterContext.Controller.ViewBag.ShowCoreProcess = false;
							


							if (oneUser != null) {
                                OneUserViewBagSetup(filterContext, s, userOrgsCount, oneUser);

                                SetupToolTips(filterContext.Controller.ViewBag, s, oneUser,Request.NotNull(x=>x.Path));

                            } else {
								var user = GetUserModel(s);
								filterContext.Controller.ViewBag.Hints = user.Hints;
								filterContext.Controller.ViewBag.UserName = user.Name() ?? MessageStrings.User;
								filterContext.Controller.ViewBag.UserColor = user.GetUserHashCode();
							}

						}

						tx.Commit();
						s.Flush();
					}
				}
				//DataCollection.CommentMarkProfile(2, "End");
				base.OnActionExecuting(filterContext);
			} catch (LoginException) {
				var f = filterContext.HttpContext.Response.Filter;
				filterContext.HttpContext.Response.Filter = null;
				SignOut();
				var redirectUrl = Request.Url.PathAndQuery;
				log.Info("Login: [" + Request.Url.PathAndQuery + "] --> [" + redirectUrl + "]");
				filterContext.Result = RedirectToAction("Login", "Account", new { area = "", returnUrl = redirectUrl });
				//filterContext.ExceptionHandled = true;
				filterContext.HttpContext.Response.Clear();
			} catch (PermissionsException e) {
				var f = filterContext.HttpContext.Response.Filter;
				filterContext.HttpContext.Response.Filter = null;
				filterContext.Result = RedirectToAction("Index", "Error", new {
					area = "",
					message = e.Message,
					redirectUrl = e.RedirectUrl
				});
			}
		}

		protected string GetAppVersion() {
			var version = Assembly.GetExecutingAssembly().GetName().Version;
			//var buildDate = new DateTime(2000, 1, 1).AddDays(version.Build).AddSeconds(version.Revision * 2);
			return version.ToString();
		}

		private void SetupToolTips(dynamic ViewBag, ISession s, UserOrganizationModel oneUser,string path) {
            try {
                var username = oneUser.User.NotNull(x => x.Id);
                var enabled =  !oneUser.User.NotNull(x=>x.DisableTips);
                if (username != null && path != null && enabled) {
                    ViewBag.TooltipsEnabled = true;
                    ViewBag.Tooltips = SupportAccessor.GetTooltips(username, path);
                }
            } catch (Exception e) {
                //Eat it! Get yourself a fork and feed it.
            }
        }

        private static void OneUserViewBagSetup(ActionExecutingContext filterContext, ISession s, int userOrgsCount, UserOrganizationModel oneUser) {
            var name = new HtmlString(oneUser.GetName());

            if (userOrgsCount > 1) {
                name = new HtmlString(oneUser.GetNameAndTitle(1));
                try {
                    name = new HtmlString(name + " <span class=\"visible-md visible-lg\" style=\"display:inline ! important\">at " + oneUser.Organization.Name.Translate() + "</span>");
                } catch (Exception e) {
                    log.Error(e);
                }
            }
            filterContext.Controller.ViewBag.UserImage = oneUser.ImageUrl(true, ImageSize._img);
            filterContext.Controller.ViewBag.UserInitials = oneUser.GetInitials();
            filterContext.Controller.ViewBag.UserColor = oneUser.GeUserHashCode();
            filterContext.Controller.ViewBag.UsersName = oneUser.GetName();

            filterContext.Controller.ViewBag.UserOrganization = oneUser;
            filterContext.Controller.ViewBag.ConsoleLog = oneUser.User.NotNull(x => x.ConsoleLog);

            filterContext.Controller.ViewBag.TaskCount = 0;

            filterContext.Controller.ViewBag.UserName = name;
            filterContext.Controller.ViewBag.ShowL10 = oneUser.Organization.Settings.EnableL10;
            filterContext.Controller.ViewBag.ShowReview = oneUser.Organization.Settings.EnableReview && !oneUser.IsClient;
            filterContext.Controller.ViewBag.ShowSurvey = oneUser.Organization.Settings.EnableSurvey && oneUser.IsManager();
            filterContext.Controller.ViewBag.ShowPeople = oneUser.Organization.Settings.EnablePeople;// && oneUser.IsManager();
            filterContext.Controller.ViewBag.ShowCoreProcess = oneUser.Organization.Settings.EnableCoreProcess;// && oneUser.IsManager();

            filterContext.Controller.ViewBag.ShowAC = PermissionsAccessor.IsPermitted(s, oneUser, x => x.CanView(PermItem.ResourceType.AccountabilityHierarchy, oneUser.Organization.AccountabilityChartId)); // oneUser.Organization.acc && oneUser.IsManager();

            var isManager = oneUser.ManagerAtOrganization || oneUser.ManagingOrganization || oneUser.IsRadialAdmin;
            filterContext.Controller.ViewBag.LimitFiveState = oneUser.Organization.Settings.LimitFiveState;
            filterContext.Controller.ViewBag.IsRadialAdmin = oneUser.IsRadialAdmin;
            filterContext.Controller.ViewBag.IsManager = isManager;
            filterContext.Controller.ViewBag.ManagingOrganization = oneUser.ManagingOrganization || oneUser.IsRadialAdmin;
            filterContext.Controller.ViewBag.UserId = oneUser.Id;
            filterContext.Controller.ViewBag.OrganizationId = oneUser.Organization.Id;
            filterContext.Controller.ViewBag.Organization = oneUser.Organization;
            filterContext.Controller.ViewBag.Hints = oneUser.User.NotNull(x => x.Hints);
        }

        protected override void OnActionExecuted(ActionExecutedContext filterContext) {
			HibernateSession.CloseCurrentSession();
			if (ToValidate.Any()) {
				var err = "Didn't validate: " + String.Join(",", ToValidate);
				TempData["Message"] = err;
				throw new PermissionsException(err);
			}

			if (TempData["ModelState"] != null && !ModelState.Equals(TempData["ModelState"]))
				ModelState.Merge((ModelStateDictionary)TempData["ModelState"]);
			if (TempData["Message"] != null)
				ViewBag.Message = TempData["Message"];
			if (TempData["InfoAlert"] != null)
				ViewBag.InfoAlert = TempData["InfoAlert"];


			CompressContent(filterContext);
			base.OnActionExecuted(filterContext);
		}
		#region Angular Json Overrides
		protected new JsonResult Json(object data) {
			if (data is IAngular && (Request == null || Request.Params["transform"] == null))
				return base.Json(AngularSerializer.Serialize((IAngular)data));
			if (data is IEnumerable<IAngular> && (Request == null || Request.Params["transform"] == null))
				return base.Json(((IEnumerable<IAngular>)data).Select(x => AngularSerializer.Serialize(x)).ToArray());
			return base.Json(data);
		}

		protected new JsonResult Json(object data, JsonRequestBehavior behavior) {
			if (data is IAngular && TransformAngular && (Request == null || Request.Params["transform"] == null))
				return base.Json(AngularSerializer.Serialize((IAngular)data), behavior);
			if (data is IEnumerable<IAngular> && TransformAngular && (Request == null || Request.Params["transform"] == null))
				return base.Json(((IEnumerable<IAngular>)data).Select(x => AngularSerializer.Serialize(x)).ToArray(), behavior);
			return base.Json(data, behavior);
		}
		protected new JsonResult Json(object data, string contentType) {
			if (data is IAngular && TransformAngular && (Request == null || Request.Params["transform"] == null))
				return base.Json(AngularSerializer.Serialize((IAngular)data), contentType);
			if (data is IEnumerable<IAngular> && TransformAngular && (Request == null || Request.Params["transform"] == null))
				return base.Json(((IEnumerable<IAngular>)data).Select(x => AngularSerializer.Serialize(x)).ToArray(), contentType);
			return base.Json(data, contentType);
		}
		protected new JsonResult Json(object data, string contentType, JsonRequestBehavior behavior) {
			if (data is IAngular && TransformAngular && (Request == null || Request.Params["transform"] == null))
				return base.Json(AngularSerializer.Serialize((IAngular)data), contentType, behavior);
			if (data is IEnumerable<IAngular> && TransformAngular && (Request == null || Request.Params["transform"] == null))
				return base.Json(((IEnumerable<IAngular>)data).Select(x => AngularSerializer.Serialize(x)).ToArray(), contentType, behavior);
			return base.Json(data, contentType, behavior);
		}
		protected new JsonResult Json(object data, string contentType, Encoding encoding) {
			if (data is IAngular && TransformAngular && (Request == null || Request.Params["transform"] == null))
				return base.Json(AngularSerializer.Serialize((IAngular)data), contentType, encoding);
			if (data is IEnumerable<IAngular> && TransformAngular && (Request == null || Request.Params["transform"] == null))
				return base.Json(((IEnumerable<IAngular>)data).Select(x => AngularSerializer.Serialize(x)).ToArray(), contentType, encoding);
			return base.Json(data, contentType, encoding);
		}
		protected new JsonResult Json(object data, string contentType, Encoding encoding, JsonRequestBehavior behavior) {
			if (data is IAngular && TransformAngular && (Request == null || Request.Params["transform"] == null))
				return base.Json(AngularSerializer.Serialize((IAngular)data), contentType, encoding, behavior);
			if (data is IEnumerable<IAngular> && TransformAngular && (Request == null || Request.Params["transform"] == null))
				return base.Json(((IEnumerable<IAngular>)data).Select(x => AngularSerializer.Serialize(x)).ToArray(), contentType, encoding, behavior);
			return base.Json(data, contentType, encoding, behavior);
		}

		#endregion
		#endregion
	}

}