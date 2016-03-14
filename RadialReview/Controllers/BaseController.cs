﻿using System.Collections.Specialized;
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


namespace RadialReview.Controllers
{
	public class BaseController : Controller
	{
		#region Helpers
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        #region ViewBag

        protected void ShowAlert(string status, AlertType type = AlertType.Info)
        {
            switch (type)
            {
                case AlertType.Info: ViewBag.InfoAlert = status; break;
                case AlertType.Error: ViewBag.Alert = status; break;
                case AlertType.Success: ViewBag.Success= status; break;
                default: throw new ArgumentOutOfRangeException("AlertType:"+type);
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
		protected static DeepSubordianteAccessor _DeepSubordianteAccessor = new DeepSubordianteAccessor();
		protected static ResponsibilitiesAccessor _ResponsibilitiesAccessor = new ResponsibilitiesAccessor();
		#endregion
		#region GetUserModel
		protected UserModel GetUserModel()
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					return GetUserModel(s);
				}
			}
		}

		protected UserModel GetUserModel(ISession s)
		{
			return new Cache().GetOrGenerate(CacheKeys.USER, x =>
			{
				x.LifeTime = LifeTime.Request/*Session*/;
				var id = User.Identity.GetUserId();
				return _UserAccessor.GetUserById(s, id);
			});
		}
		#endregion
		#region GetUser
		private long? _CurrentUserOrganizationId = null;

        private UserOrganizationModel MockUser = null;

		private UserOrganizationModel PopulateUserData(UserOrganizationModel user)
		{
            if (user != null && Request != null)
            {
				user._ClientTimestamp = Request.Params.Get("_clientTimestamp").TryParseLong();
			}
			return user;
		}
		public UserOrganizationModel GetUser()
		{
			return PopulateUserData(_GetUser());
		}
		public UserOrganizationModel GetUser(ISession s)
		{
			return PopulateUserData(_GetUser(s));
		}
		public UserOrganizationModel GetUser(long userOrganizationId)
		{
			return PopulateUserData(_GetUser(userOrganizationId));
		}
		private UserOrganizationModel GetUserOrganization(ISession s, long userOrganizationId, String redirectUrl)//, Boolean full = false)
		{
			var cache = new Cache();

			return cache.GetOrGenerate(CacheKeys.USERORGANIZATION, x =>
			{
				var id = User.Identity.GetUserId();
				var found = _UserAccessor.GetUserOrganizations(s, id, userOrganizationId, redirectUrl);
				if (found != null && found.User != null && !cache.Contains(CacheKeys.USER))
				{
					cache.Push(CacheKeys.USER, found.User, LifeTime.Request/*Session*/);
				}
				x.LifeTime = LifeTime.Request/*Session*/;
				return found;
			}, x => x.Id != userOrganizationId);
		}

		// ReSharper disable once RedundantOverload.Local
		private UserOrganizationModel _GetUser(ISession s)
		{
            if (MockUser != null)
                return MockUser;

			return _GetUser(s, null);
		}
		private UserOrganizationModel _GetUser()
        {
            if (MockUser != null)
                return MockUser;
			long? userOrganizationId = null;

			if (userOrganizationId == null)
			{
				var orgIdParam = Request.Params.Get("organizationId");
				if (orgIdParam != null)
					userOrganizationId = long.Parse(orgIdParam);
			}

			var cache = new Cache();

			if (userOrganizationId == null && cache.Get(CacheKeys.USERORGANIZATION_ID) is long)
			{
				userOrganizationId = (long)cache.Get(CacheKeys.USERORGANIZATION_ID);
			}



			if (cache.Get(CacheKeys.USERORGANIZATION) is UserOrganizationModel && userOrganizationId == ((UserOrganizationModel)cache.Get(CacheKeys.USERORGANIZATION)).Id)
				return (UserOrganizationModel)cache.Get(CacheKeys.USERORGANIZATION);

			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					return _GetUser(s, null);
				}
			}
		}
		private UserOrganizationModel _GetUser(long userOrganizationId)
        {
            if (MockUser != null && MockUser.Id==userOrganizationId)
                return MockUser;
			var cache = new Cache();

			if (cache.Get(CacheKeys.USERORGANIZATION) is UserOrganizationModel && userOrganizationId == ((UserOrganizationModel)cache.Get(CacheKeys.USERORGANIZATION)).Id)
				return (UserOrganizationModel)cache.Get(CacheKeys.USERORGANIZATION);

			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					return _GetUser(s, userOrganizationId);
				}
			}
		}
		private UserOrganizationModel _GetUser(ISession s, long? userOrganizationId = null)//long? organizationId, Boolean full = false)
        {
            
			/**/
			if (userOrganizationId == null)
			{
                try
                {
                    var orgIdParam = Request.Params.Get("organizationId");
                    if (orgIdParam != null)
                        userOrganizationId = long.Parse(orgIdParam);
                }
                catch (Exception e)
                {
                    var o = false;
                }
			}
			var cache = new Cache();

			if (userOrganizationId == null && cache.Get(CacheKeys.USERORGANIZATION_ID) != null)
			{
				userOrganizationId = (long)cache.Get(CacheKeys.USERORGANIZATION_ID);
			}
			if (userOrganizationId == null)
			{
				userOrganizationId = GetUserModel(s).GetCurrentRole();
			}


			var user = cache.Get(CacheKeys.USERORGANIZATION);

			if (user is UserOrganizationModel && userOrganizationId == ((UserOrganizationModel)user).Id)
				return (UserOrganizationModel)user;

			if (userOrganizationId == null)
			{
				var returnPath = Server.HtmlEncode(Request.Path);

				var found = GetUserOrganizations(s, returnPath);
				if (found.Count() == 0)
					throw new NoUserOrganizationException();
				else if (found.Count() == 1)
				{
					var uo = found.First();
					if (uo.User != null){
						uo.User.CurrentRole = uo.Id;
						s.Update(uo.User);
					}
					//_CurrentUserOrganizationId = uo.Id;
					cache.Push(CacheKeys.USERORGANIZATION, uo, LifeTime.Request/*Session*/);
					cache.Push(CacheKeys.USERORGANIZATION_ID, uo.Id, LifeTime.Request/*Session*/);
					return uo;
				}
				else
					throw new OrganizationIdException(Request.Url.PathAndQuery);
			}
			else
			{
				var uo = GetUserOrganization(s, userOrganizationId.Value, Request.Url.PathAndQuery);
				//_CurrentUserOrganizationId = uo.Id;
				cache.Push(CacheKeys.USERORGANIZATION, uo, LifeTime.Request/*Session*/);
				cache.Push(CacheKeys.USERORGANIZATION_ID, userOrganizationId.Value, LifeTime.Request/*Session*/);
				return uo;

			}
		}

		protected List<UserOrganizationModel> GetUserOrganizations(String redirectUrl) //Boolean full = false)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
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
		protected void ValidateValues<T>(T model, params Expression<Func<T, object>>[] selectors)
		{
            if (SkipValidation)
                return;
			foreach (var e in selectors)
			{
				//var meta = ModelMetadata.FromLambdaExpression(e, new ViewDataDictionary<T>());
				var name = e.GetMvcName();//meta.DisplayName;
				if (!ToValidate.Remove(name))
					throw new PermissionsException("Validation item does not exist.");
				SecuredValueValidator.ValidateValue(ValidationCollection, name);
			}
		}
		#endregion

		protected void ManagerAndCanEditOrException(UserOrganizationModel user)
		{
			if (!user.IsManagerCanEditOrganization())
				throw new PermissionsException();
		}
		protected ActionResult RedirectToLocal(string returnUrl)
		{
			if (Url.IsLocalUrl(returnUrl))
				return Redirect(returnUrl);
			else
				throw new RedirectException("Return URL is invalid.");
		}

		protected FileResult Pdf(PdfDocument document,string name=null,bool inline=true)
		{
			var stream = new MemoryStream();
			document.Save(stream, false);
			name = name ??(Guid.NewGuid()+".pdf");
            //if (inline){
            //    Response.AppendHeader("content-disposition", "inline; filename=\""+name+"\"");
            //}
			return File(stream, System.Net.Mime.MediaTypeNames.Application.Pdf, name);
		}

		protected FileResult Pdf(Document document, string name = null, bool inline = true)
		{
			var pdfRenderer = new PdfDocumentRenderer(true, PdfFontEmbedding.Always);
			pdfRenderer.Document = document;
			pdfRenderer.RenderDocument();
			
			var stream = new MemoryStream();
			pdfRenderer.Save(stream, false);
			name = name ?? (Guid.NewGuid() + ".pdf");
            //var file = File(stream, System.Net.Mime.MediaTypeNames.Application.Pdf, name);
            if (inline)
            {
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
		protected void SignOut()
		{
			new Cache().Invalidate(CacheKeys.USERORGANIZATION_ID);
			AuthenticationManager.SignOut();
			FormsAuthentication.SignOut();
			HttpContext.User = new GenericPrincipal(new GenericIdentity(string.Empty), null);
			Session.Clear();
		}
		protected bool IsLoggedIn()
		{
			return User.Identity.GetUserId() != null;
		}
		protected IAuthenticationManager AuthenticationManager
		{
			get
			{
				return HttpContext.GetOwinContext().Authentication;
			}
		}
		#endregion
		#region Overrides
		private static MethodInfo GetActionMethod(ExceptionContext filterContext)
		{
			Type controllerType = filterContext.Controller.GetType();// Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(x => x.Name == requestContext.RouteData.Values["controller"].ToString());
			ControllerContext controllerContext = new ControllerContext(filterContext.RequestContext, Activator.CreateInstance(controllerType) as ControllerBase);
			ControllerDescriptor controllerDescriptor = new ReflectedControllerDescriptor(controllerType);
			ActionDescriptor actionDescriptor = controllerDescriptor.FindAction(controllerContext, controllerContext.RouteData.Values["action"].ToString());
			return (actionDescriptor as ReflectedActionDescriptor).MethodInfo;
		}

		protected override void OnException(ExceptionContext filterContext)
		{
            try
            {
                ChromeExtensionComms.SendCommand("pageError");
                var f=filterContext.HttpContext.Response.Filter;
                filterContext.HttpContext.Response.Filter = null;
                if (filterContext.HttpContext.Response.Headers.AllKeys.Any(x=>x=="Content-Encoding"))
                    filterContext.HttpContext.Response.Headers.Remove("Content-Encoding");
				var action = GetActionMethod(filterContext);

				if (typeof (JsonResult).IsAssignableFrom(action.ReturnType)){
					var exception = new ResultObject(filterContext.Exception);
					if (filterContext.Exception is RedirectException){
						var re = ((RedirectException) filterContext.Exception);
						if (re.Silent != null)
							exception.Silent = re.Silent.Value;

						if (re.ForceReload)
							exception.Refresh = true;
					}

					filterContext.ExceptionHandled = true;
					filterContext.HttpContext.Response.Clear();
					filterContext.HttpContext.Response.TrySkipIisCustomErrors = true; 
					filterContext.HttpContext.Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
					filterContext.Result = new JsonResult(){Data = exception,JsonRequestBehavior = JsonRequestBehavior.AllowGet};
					return;
				}


				if (filterContext.ExceptionHandled)
					return;

				if (filterContext.Exception is LoginException){
					SignOut();
					var redirectUrl = ((RedirectException) filterContext.Exception).RedirectUrl;
					if (redirectUrl == null)
						redirectUrl = Request.Url.PathAndQuery;
					log.Info("Login: [" + Request.Url.PathAndQuery + "] --> [" + redirectUrl + "]");
					filterContext.Result = RedirectToAction("Login", "Account", new{message = filterContext.Exception.Message, returnUrl = redirectUrl});
					filterContext.ExceptionHandled = true;
					filterContext.HttpContext.Response.Clear();
				}
				else if (filterContext.Exception is OrganizationIdException){
					var redirectUrl = ((RedirectException) filterContext.Exception).RedirectUrl;
					log.Info("Organization: [" + Request.Url.PathAndQuery + "] --> [" + redirectUrl + "]");
					filterContext.Result = RedirectToAction("Role", "Account", new{message = filterContext.Exception.Message, returnUrl = redirectUrl});
					filterContext.ExceptionHandled = true;
					filterContext.HttpContext.Response.Clear();
				}
				else if (filterContext.Exception is PermissionsException){
					filterContext.HttpContext.Response.Clear();
					var returnUrl = ((RedirectException) filterContext.Exception).RedirectUrl;
					log.Info("Permissions: [" + Request.Url.PathAndQuery + "] --> [" + returnUrl + "]");
					ViewBag.Message = filterContext.Exception.Message;
					if (typeof (PartialViewResult).IsAssignableFrom(action.ReturnType)){
						filterContext.Result = PartialView("~/Views/Error/Index.cshtml", filterContext.Exception);
					}
					else{
						filterContext.Result = View("~/Views/Error/Index.cshtml", filterContext.Exception);
					}
					filterContext.ExceptionHandled = true;
				}
				else if (filterContext.Exception is MeetingException){
					var type = ((MeetingException) filterContext.Exception).MeetingExceptionType;
					log.Info("MeetingException: [" + Request.Url.PathAndQuery + "] --> [" + type + "]");
					filterContext.Result = RedirectToAction("ErrorMessage", "L10", new{message = filterContext.Exception.Message, type});
					filterContext.ExceptionHandled = true;
					filterContext.HttpContext.Response.Clear();
				}
				else if (filterContext.Exception is RedirectException){
					var returnUrl = ((RedirectException) filterContext.Exception).RedirectUrl;
					log.Info("Redirect: [" + Request.Url.PathAndQuery + "] --> [" + returnUrl + "]");
					filterContext.Result = RedirectToAction("Index", "Error", new{message = filterContext.Exception.Message, returnUrl = returnUrl});
					filterContext.ExceptionHandled = true;
					filterContext.HttpContext.Response.Clear();
				}
				else if (filterContext.Exception is HttpAntiForgeryException){
					log.Info("AntiForgery: [" + Request.Url.PathAndQuery + "] --> []");
					filterContext.Result = RedirectToAction("Login", "Account", new{message = filterContext.Exception.Message});
					filterContext.ExceptionHandled = true;
					filterContext.HttpContext.Response.Clear();
				}
				else{
					log.Error("Error: [" + Request.Url.PathAndQuery + "]<<" + filterContext.Exception.Message + ">>", filterContext.Exception);
					base.OnException(filterContext);
				}
			}
			catch (Exception e){
				log.Info("OnException(Exception)", e);
				filterContext.Result = Content(e.Message+"  "+e.StackTrace);
			}
		}

        protected void CompressContent(ActionExecutedContext filterContext)
        {
            var encodingsAccepted = filterContext.HttpContext.Request.Headers["Accept-Encoding"];
            var contentType = filterContext.HttpContext.Request.Headers["Content-Type"];
            if (filterContext.IsChildAction) return;

            if (string.IsNullOrEmpty(encodingsAccepted)) return;
            if (contentType!=null && contentType.ToLower().Contains("pdf")) return;

            encodingsAccepted = encodingsAccepted.ToLowerInvariant();
            var response = filterContext.HttpContext.Response;

            if (encodingsAccepted.Contains("deflate"))
            {
                Response.Headers.Remove("Content-Encoding");
                response.AppendHeader("Content-Encoding", "deflate");
                response.Filter = new DeflateStream(response.Filter, CompressionMode.Compress);
            }
            else if (encodingsAccepted.Contains("gzip"))
            {
                Response.Headers.Remove("Content-Encoding");
                response.AppendHeader("Content-Encoding", "gzip");
                response.Filter = new GZipStream(response.Filter, CompressionMode.Compress);
            }
        }

		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			try{
				using (var s = HibernateSession.GetCurrentSession()){
					using (var tx = s.BeginTransaction()){
						//DataCollection.CommentMarkProfile(2, "Validation");
						//Secure hidden fields
						ValidationCollection = filterContext.RequestContext.HttpContext.Request.Form;
						foreach (var f in ValidationCollection.AllKeys){
							if (f != null && f.EndsWith(SecuredValueFieldNameComputer.NameSuffix)){
								ToValidate.Add(f.Substring(0, f.Length - SecuredValueFieldNameComputer.NameSuffix.Length));
							}
						}
						//DataCollection.CommentMarkProfile(2, "Attribute");


						//Access Level Filtering
						var accessAttributes = filterContext.ActionDescriptor.GetCustomAttributes(typeof (AccessAttribute), false).Cast<AccessAttribute>();
						if (accessAttributes.Count() == 0)
							throw new NotImplementedException("Access attribute missing.");

						switch((AccessLevel) accessAttributes.Min(x => (int) x.AccessLevel)){
							case AccessLevel.SignedOut:{
								if (Request.IsAuthenticated){
									SignOut();
									//HttpContext.User.Identity = null;
									//throw new LoginException(Request.Url.PathAndQuery);
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
									throw new PermissionsException("You do not have access to this resource.");
								if (u1.Organization.DeleteTime != null)
									throw new PermissionsException("This organization no longer exists.");
								break;
							case AccessLevel.Manager:
								var u2 = GetUser(s);
								if (u2.DeleteTime != null)
									throw new PermissionsException("You do not have access to this resource.");
								if (u2.Organization.DeleteTime != null)
									throw new PermissionsException("This organization no longer exists.");
								if (!u2.IsManager()) 
									throw new PermissionsException("You must be a manager to view this resource.");
								break;
							case AccessLevel.Radial:
                                if (!(GetUserModel(s).IsRadialAdmin || GetUser(s).IsRadialAdmin)) throw new PermissionsException("You do not have access to this resource.");
								break;
							default:
								throw new Exception("Unknown Access Type");
						}




						// eat the cookie (if any) and set the culture
						if (Request.Cookies["lang"] != null){
							HttpCookie cookie = Request.Cookies["lang"];
							string lang = cookie.Value;
							var culture = new System.Globalization.CultureInfo(lang);
							Thread.CurrentThread.CurrentCulture = culture;
							Thread.CurrentThread.CurrentUICulture = culture;
						}

						filterContext.Controller.ViewBag.IsLocal = Config.IsLocal();

						filterContext.Controller.ViewBag.HasBaseController = true;

						//DataCollection.CommentMarkProfile(2, "ViewBag");
						if (IsLoggedIn()){
							var userOrgsCount = GetUserOrganizationCounts(s, Request.Url.PathAndQuery);
							UserOrganizationModel oneUser = null;
							var hints = true;
							try{
								oneUser = GetUser(s);
								var lu = s.Get<UserLookup>(oneUser.Cache.Id);
								if (!GetUserModel(s).IsRadialAdmin){
									lu.LastLogin = DateTime.UtcNow;
									s.Update(lu);
								}
							}
							catch (OrganizationIdException){
							}
							catch (NoUserOrganizationException){
							}

							filterContext.Controller.ViewBag.UserName = MessageStrings.User;
							filterContext.Controller.ViewBag.UserImage = "/img/placeholder";
							filterContext.Controller.ViewBag.UserInitials = "";
							filterContext.Controller.ViewBag.UserColor = 0;
							filterContext.Controller.ViewBag.IsManager = false;
							filterContext.Controller.ViewBag.ShowL10 = false;
							filterContext.Controller.ViewBag.ShowReview = false;
							filterContext.Controller.ViewBag.ShowSurvey = false;
							filterContext.Controller.ViewBag.Organizations = userOrgsCount;
							filterContext.Controller.ViewBag.Hints = true;
							filterContext.Controller.ViewBag.ManagingOrganization = false;
                            filterContext.Controller.ViewBag.Organization = null;
                            filterContext.Controller.ViewBag.UserId = 0L;
                            filterContext.Controller.ViewBag.ConsoleLog = false;

							if (oneUser != null){
								var name = new HtmlString(oneUser.GetName());

								if (userOrgsCount > 1){
									name = new HtmlString(oneUser.GetNameAndTitle(1));
									try{
										name = new HtmlString(name + " <span class=\"visible-md visible-lg\" style=\"display:inline ! important\">at " + oneUser.Organization.Name.Translate() + "</span>");
									}
									catch (Exception e){
										log.Error(e);
									}
								}
								//DataCollection.CommentMarkProfile(2, "ViewBagPop");
								filterContext.Controller.ViewBag.UserImage = oneUser.ImageUrl(true, ImageSize._img);
								filterContext.Controller.ViewBag.UserInitials = oneUser.GetInitials();
								filterContext.Controller.ViewBag.UserColor = oneUser.GeUserHashCode();
								filterContext.Controller.ViewBag.UsersName = oneUser.GetName();

                                filterContext.Controller.ViewBag.UserOrganization = oneUser;
                                filterContext.Controller.ViewBag.ConsoleLog = oneUser.User.NotNull(x=>x.ConsoleLog);

								filterContext.Controller.ViewBag.TaskCount = _TaskAccessor.GetUnstartedTaskCountForUser(s, oneUser.Id, DateTime.UtcNow);
								//filterContext.Controller.ViewBag.Hints = oneUser.User.Hints;
								filterContext.Controller.ViewBag.UserName = name;
								filterContext.Controller.ViewBag.ShowL10 = oneUser.Organization.Settings.EnableL10;
								filterContext.Controller.ViewBag.ShowReview = oneUser.Organization.Settings.EnableReview && !oneUser.IsClient;
								filterContext.Controller.ViewBag.ShowSurvey = oneUser.Organization.Settings.EnableSurvey && oneUser.IsManager();
								var isManager = oneUser.ManagerAtOrganization || oneUser.ManagingOrganization || oneUser.IsRadialAdmin;
								filterContext.Controller.ViewBag.IsManager = isManager;
								filterContext.Controller.ViewBag.ManagingOrganization = oneUser.ManagingOrganization || oneUser.IsRadialAdmin;
								filterContext.Controller.ViewBag.UserId = oneUser.Id;
								filterContext.Controller.ViewBag.OrganizationId = oneUser.Organization.Id;
								filterContext.Controller.ViewBag.Organization = oneUser.Organization;
								filterContext.Controller.ViewBag.Hints = oneUser.User.NotNull(x => x.Hints);

							}
							else{
								var user = GetUserModel(s);
								filterContext.Controller.ViewBag.Hints = user.Hints;
								filterContext.Controller.ViewBag.UserName = user.Name() ?? MessageStrings.User;
								filterContext.Controller.ViewBag.UserColor = user.GetUserHashCode();
							}

							// ViewBag.OrganizationId = Session["OrganizationId"];
						}
					}
				}
				//DataCollection.CommentMarkProfile(2, "End");
				base.OnActionExecuting(filterContext);
			}
            catch (LoginException e)
            {
                var f = filterContext.HttpContext.Response.Filter;
                filterContext.HttpContext.Response.Filter = null;
                SignOut();
                var redirectUrl = Request.Url.PathAndQuery;
                log.Info("Login: [" + Request.Url.PathAndQuery + "] --> [" + redirectUrl + "]");
                filterContext.Result = RedirectToAction("Login", "Account", new { returnUrl = redirectUrl });
                //filterContext.ExceptionHandled = true;
                filterContext.HttpContext.Response.Clear();
            }
            catch (PermissionsException e)
            {
                var f = filterContext.HttpContext.Response.Filter;
                filterContext.HttpContext.Response.Filter = null;
				filterContext.Result = RedirectToAction("Index","Error",new{
					message=e.Message,
					redirectUrl = e.RedirectUrl
				});
			}
		}
		protected override void OnActionExecuted(ActionExecutedContext filterContext)
		{
			HibernateSession.CloseCurrentSession();
			if (ToValidate.Any())
			{
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
		protected new JsonResult Json(object data)
		{
			if (data is IAngular && Request.Params["transform"] == null)
				return base.Json(AngularSerializer.Serialize((IAngular)data));
			return base.Json(data);
		}

		protected new JsonResult Json(object data, JsonRequestBehavior behavior)
		{
			if (data is IAngular && Request.Params["transform"] == null)
				return base.Json(AngularSerializer.Serialize((IAngular)data), behavior);
			return base.Json(data, behavior);
		}
		protected new JsonResult Json(object data, string contentType)
		{
			if (data is IAngular && Request.Params["transform"] == null)
				return base.Json(AngularSerializer.Serialize((IAngular)data), contentType);
			return base.Json(data, contentType);
		}
		protected new JsonResult Json(object data, string contentType, JsonRequestBehavior behavior)
		{
			if (data is IAngular && Request.Params["transform"] == null)
				return base.Json(AngularSerializer.Serialize((IAngular)data), contentType, behavior);
			return base.Json(data, contentType, behavior);
		}
		protected new JsonResult Json(object data, string contentType, Encoding encoding)
		{
			if (data is IAngular && Request.Params["transform"] == null)
				return base.Json(AngularSerializer.Serialize((IAngular)data), contentType, encoding);
			return base.Json(data, contentType, encoding);
		}
		protected new JsonResult Json(object data, string contentType, Encoding encoding, JsonRequestBehavior behavior)
		{
			if (data is IAngular && Request.Params["transform"] == null)
				return base.Json(AngularSerializer.Serialize((IAngular)data), contentType, encoding, behavior);
			return base.Json(data, contentType, encoding, behavior);
		}

		#endregion
		#endregion
	}

}