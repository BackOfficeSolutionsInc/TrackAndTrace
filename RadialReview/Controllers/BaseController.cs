using System.Collections.Specialized;
using System.Linq.Expressions;
using Microsoft.Ajax.Utilities;
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


namespace RadialReview.Controllers
{
	public class BaseController : Controller
	{
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		protected static UserEngine _UserEngine = new UserEngine();
		protected static ChartsEngine _ChartsEngine = new ChartsEngine();
		protected static ReviewEngine _ReviewEngine = new ReviewEngine();

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

		protected void ManagerAndCanEditOrException(UserOrganizationModel user)
		{
			if (!user.IsManagerCanEditOrganization())
				throw new PermissionsException();
		}
		protected UserModel GetUserModel()
		{
			using (var s = HibernateSession.GetCurrentSession()){
				using (var tx = s.BeginTransaction()){
					return GetUserModel(s);
				}
			}
		}

		protected UserModel GetUserModel(ISession s)
		{
			return new Cache().GetOrGenerate(CacheKeys.USER, x =>{
				x.LifeTime=LifeTime.Session;
				var id = User.Identity.GetUserId();
				return _UserAccessor.GetUserById(s, id);
			});
		}

		protected List<UserOrganizationModel> GetUserOrganizations(String redirectUrl) //Boolean full = false)
		{
			using (var s = HibernateSession.GetCurrentSession()){
				using (var tx = s.BeginTransaction()){
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
		/*protected List<UserOrganizationModel> GetUserOrganizations(String redirectUrl)//Boolean full = false)
		{
			var id = User.Identity.GetUserId();
			return _UserAccessor.GetUserOrganizations(s, id, redirectUrl/*, full*);
		}*/

		private UserOrganizationModel GetUserOrganization(ISession s, long userOrganizationId, String redirectUrl)//, Boolean full = false)
		{
			var cache = new Cache();

			return cache.GetOrGenerate(CacheKeys.USERORGANIZATION, x =>
			{
				var id = User.Identity.GetUserId();
				var found = _UserAccessor.GetUserOrganizations(s, id, userOrganizationId, redirectUrl);
				if (found != null && found.User != null && !cache.Contains(CacheKeys.USER)){
					cache.Push(CacheKeys.USER, found.User, LifeTime.Session);
				}
				x.LifeTime= LifeTime.Session;
				return found;
			}, x => x.Id != userOrganizationId);
		}

		//private UserOrganizationModel _CurrentUser = null;
		private long? _CurrentUserOrganizationId = null;
		/*
		protected void ChangeRole(long roleId)
		{
			_UserAccessor.ChangeRole(GetUserModel(),, roleId);
		}
		*/

		public UserOrganizationModel GetUser(ISession s)
		{
			return _GetUser(s, null);
		}

		public UserOrganizationModel GetUser()
		{
			long? userOrganizationId = null;

			if (userOrganizationId == null){
				var orgIdParam = Request.Params.Get("organizationId");
				if (orgIdParam != null)
					userOrganizationId = long.Parse(orgIdParam);
			}

			var cache = new Cache();

			if (userOrganizationId == null && cache.Get(CacheKeys.USERORGANIZATION_ID) is long){
				userOrganizationId = (long)cache.Get(CacheKeys.USERORGANIZATION_ID);
			}



			if (cache.Get(CacheKeys.USERORGANIZATION) is UserOrganizationModel && userOrganizationId == ((UserOrganizationModel)cache.Get(CacheKeys.USERORGANIZATION)).Id)
				return (UserOrganizationModel)cache.Get(CacheKeys.USERORGANIZATION);

			using (var s = HibernateSession.GetCurrentSession()){
				using (var tx = s.BeginTransaction()){
					return _GetUser(s, null);
				}
			}
		}
		public UserOrganizationModel GetUser(long userOrganizationId)
		{
			var cache = new Cache();

			

			if (cache.Get(CacheKeys.USERORGANIZATION) is UserOrganizationModel && userOrganizationId == ((UserOrganizationModel)cache.Get(CacheKeys.USERORGANIZATION)).Id)
				return (UserOrganizationModel)cache.Get(CacheKeys.USERORGANIZATION);

			using (var s = HibernateSession.GetCurrentSession()){
				using (var tx = s.BeginTransaction()){
					return _GetUser(s, userOrganizationId);
				}
			}
		}


		private UserOrganizationModel _GetUser(ISession s, long? userOrganizationId = null)//long? organizationId, Boolean full = false)
		{
			/**/
			if (userOrganizationId == null){
				var orgIdParam = Request.Params.Get("organizationId");
				if (orgIdParam != null)
					userOrganizationId = long.Parse(orgIdParam);
			}
			var cache=new Cache();

			if (userOrganizationId == null && cache.Get(CacheKeys.USERORGANIZATION_ID) != null)
			{
				userOrganizationId = (long)cache.Get(CacheKeys.USERORGANIZATION_ID);
			}
			if (userOrganizationId == null){
				userOrganizationId = GetUserModel(s).GetCurrentRole();
			}


			var user = cache.Get(CacheKeys.USERORGANIZATION);

			if (user is UserOrganizationModel && userOrganizationId == ((UserOrganizationModel)user).Id)
				return (UserOrganizationModel)user;

			if (userOrganizationId == null){
				var returnPath = Server.HtmlEncode(Request.Path);

				var found = GetUserOrganizations(s,returnPath);
				if (found.Count() == 0)
					throw new NoUserOrganizationException();
				else if (found.Count() == 1)
				{
					var uo = found.First();
					//_CurrentUserOrganizationId = uo.Id;
					cache.Push(CacheKeys.USERORGANIZATION, uo, LifeTime.Session);
					cache.Push(CacheKeys.USERORGANIZATION_ID, uo.Id, LifeTime.Session);
					return uo;
				}
				else
					throw new OrganizationIdException(Request.Url.PathAndQuery);
			}else{
				var uo=GetUserOrganization(s, userOrganizationId.Value, Request.Url.PathAndQuery);
				//_CurrentUserOrganizationId = uo.Id;
				cache.Push(CacheKeys.USERORGANIZATION, uo, LifeTime.Session);
				cache.Push(CacheKeys.USERORGANIZATION_ID, userOrganizationId.Value, LifeTime.Session);
				return uo;

			}
		}

		/*
		public UserOrganizationModel GetUserOrganization(UserModel user,int organizationId)
		{
			return _UserAccessor.GetUserOrganizations(user, organizationId);
		}*/

		protected void SignOut()
		{
			new Cache().Invalidate(CacheKeys.USERORGANIZATION_ID);
			AuthenticationManager.SignOut();
			FormsAuthentication.SignOut();
			HttpContext.User = new GenericPrincipal(new GenericIdentity(string.Empty), null);
			Session.Clear();
		}

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
			var action = GetActionMethod(filterContext);

			if (typeof(JsonResult).IsAssignableFrom(action.ReturnType))
			{
				filterContext.Result = Json(new ResultObject(filterContext.Exception), JsonRequestBehavior.AllowGet);
				filterContext.ExceptionHandled = true;
				return;
			}


			if (filterContext.ExceptionHandled)
				return;

			if (filterContext.Exception is LoginException)
			{
				SignOut();
				var redirectUrl = ((RedirectException)filterContext.Exception).RedirectUrl;
				if (redirectUrl == null)
					redirectUrl = Request.Url.PathAndQuery;
				log.Info("Login: [" + Request.Url.PathAndQuery + "] --> [" + redirectUrl + "]");
				filterContext.Result = RedirectToAction("Login", "Account", new { message = filterContext.Exception.Message, returnUrl = redirectUrl });
				filterContext.ExceptionHandled = true;
				filterContext.HttpContext.Response.Clear();
			}
			else if (filterContext.Exception is OrganizationIdException)
			{
				var redirectUrl = ((RedirectException)filterContext.Exception).RedirectUrl;
				log.Info("Organization: [" + Request.Url.PathAndQuery + "] --> [" + redirectUrl + "]");
				filterContext.Result = RedirectToAction("Role", "Account", new { message = filterContext.Exception.Message, returnUrl = redirectUrl });
				filterContext.ExceptionHandled = true;
				filterContext.HttpContext.Response.Clear();
			}
			else if (filterContext.Exception is PermissionsException)
			{
				var returnUrl = ((RedirectException)filterContext.Exception).RedirectUrl;
				log.Info("Permissions: [" + Request.Url.PathAndQuery + "] --> [" + returnUrl + "]");
				ViewBag.Message = filterContext.Exception.Message;
				filterContext.Result = View("~/Views/Error/Index.cshtml", filterContext.Exception);
				filterContext.ExceptionHandled = true;
				filterContext.HttpContext.Response.Clear();
			}
			else if (filterContext.Exception is MeetingException)
			{
				var type = ((MeetingException)filterContext.Exception).MeetingExceptionType;
				log.Info("MeetingException: [" + Request.Url.PathAndQuery + "] --> [" + type + "]");
				filterContext.Result = RedirectToAction("ErrorMessage", "L10", new { message = filterContext.Exception.Message, type });
				filterContext.ExceptionHandled = true;
				filterContext.HttpContext.Response.Clear();
			}
			else if (filterContext.Exception is RedirectException)
			{
				var returnUrl = ((RedirectException)filterContext.Exception).RedirectUrl;
				log.Info("Redirect: [" + Request.Url.PathAndQuery + "] --> [" + returnUrl + "]");
				filterContext.Result = RedirectToAction("Index", "Error", new { message = filterContext.Exception.Message, returnUrl = returnUrl });
				filterContext.ExceptionHandled = true;
				filterContext.HttpContext.Response.Clear();
			}
			else
			{
				log.Error("Error: [" + Request.Url.PathAndQuery + "]<<" + filterContext.Exception.Message + ">>", filterContext.Exception);
				base.OnException(filterContext);
			}
		}

		private List<String> ToValidate = new List<string>();
		private NameValueCollection ValidationCollection;

		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{

			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					//DataCollection.CommentMarkProfile(2, "Validation");
					//Secure hidden fields
					ValidationCollection = filterContext.RequestContext.HttpContext.Request.Form;
					foreach (var f in ValidationCollection.AllKeys)
					{
						if (f != null && f.EndsWith(SecuredValueFieldNameComputer.NameSuffix))
						{
							ToValidate.Add(f.Substring(0, f.Length - SecuredValueFieldNameComputer.NameSuffix.Length));
						}
					}
					//DataCollection.CommentMarkProfile(2, "Attribute");


					//Access Level Filtering
					var accessAttributes = filterContext.ActionDescriptor.GetCustomAttributes(typeof(AccessAttribute), false).Cast<AccessAttribute>();
					if (accessAttributes.Count() == 0)
						throw new NotImplementedException("Access attribute missing.");

					switch ((AccessLevel)accessAttributes.Min(x => (int)x.AccessLevel))
					{
						case AccessLevel.SignedOut:
							{
								if (Request.IsAuthenticated)
								{
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
							GetUser(s);
							break;
						case AccessLevel.Manager:
							if (!GetUser(s).IsManager()) throw new PermissionsException("You must be a manager to view this resource.");
							break;
						case AccessLevel.Radial:
							if (!(GetUserModel(s).IsRadialAdmin || GetUser(s).IsRadialAdmin)) throw new PermissionsException("You must be a Radial Admin to view this resource.");
							break;
						default:
							throw new Exception("Unknown Access Type");
					}




					// eat the cookie (if any) and set the culture
					if (Request.Cookies["lang"] != null)
					{
						HttpCookie cookie = Request.Cookies["lang"];
						string lang = cookie.Value;
						var culture = new System.Globalization.CultureInfo(lang);
						Thread.CurrentThread.CurrentCulture = culture;
						Thread.CurrentThread.CurrentUICulture = culture;
					}

					filterContext.Controller.ViewBag.IsLocal = Config.IsLocal();

					filterContext.Controller.ViewBag.HasBaseController = true;

					//DataCollection.CommentMarkProfile(2, "ViewBag");
					if (IsLoggedIn())
					{
						var userOrgsCount = GetUserOrganizationCounts(s,Request.Url.PathAndQuery);
						UserOrganizationModel oneUser = null;
						var hints = true;
						try
						{
							oneUser = GetUser(s);
						}
						catch (OrganizationIdException)
						{
						}
						catch (NoUserOrganizationException)
						{
						}

						filterContext.Controller.ViewBag.UserName = MessageStrings.User;
						filterContext.Controller.ViewBag.UserImage = "/img/placeholder";
						filterContext.Controller.ViewBag.IsManager = false;
						filterContext.Controller.ViewBag.ShowL10 = false;
						filterContext.Controller.ViewBag.ShowReview = false;
						filterContext.Controller.ViewBag.Organizations = userOrgsCount;
						filterContext.Controller.ViewBag.Hints = true;
						filterContext.Controller.ViewBag.ManagingOrganization = false;
						filterContext.Controller.ViewBag.Organization = null;

						if (oneUser != null)
						{
							HtmlString name = new HtmlString(oneUser.GetName());

							if (userOrgsCount > 1)
							{
								name = new HtmlString(oneUser.GetNameAndTitle(1));
								try
								{
									name = new HtmlString(name + " <span class=\"visible-md visible-lg\" style=\"display:inline ! important\">at " + oneUser.Organization.Name.Translate() + "</span>");
								}
								catch (Exception e)
								{
									log.Error(e);
								}
							}
							//DataCollection.CommentMarkProfile(2, "ViewBagPop");
							filterContext.Controller.ViewBag.UserImage = oneUser.ImageUrl(true, ImageSize._64);
							filterContext.Controller.ViewBag.TaskCount = _TaskAccessor.GetUnstartedTaskCountForUser(s, oneUser.Id, DateTime.UtcNow);
							//filterContext.Controller.ViewBag.Hints = oneUser.User.Hints;
							filterContext.Controller.ViewBag.UserName = name;
							filterContext.Controller.ViewBag.ShowL10 = oneUser.Organization.Settings.EnableL10;
							filterContext.Controller.ViewBag.ShowReview = oneUser.Organization.Settings.EnableReview;
							var isManager = oneUser.ManagerAtOrganization || oneUser.ManagingOrganization || oneUser.IsRadialAdmin;
							filterContext.Controller.ViewBag.IsManager = isManager;
							filterContext.Controller.ViewBag.ManagingOrganization = oneUser.ManagingOrganization || oneUser.IsRadialAdmin;
							filterContext.Controller.ViewBag.UserId = oneUser.Id;
							filterContext.Controller.ViewBag.OrganizationId = oneUser.Organization.Id;
							filterContext.Controller.ViewBag.Organization = oneUser.Organization;
							filterContext.Controller.ViewBag.Hints = oneUser.User.Hints;

						}
						else
						{
							var user = GetUserModel(s);
							filterContext.Controller.ViewBag.Hints = user.Hints;
							filterContext.Controller.ViewBag.UserName = user.Name() ?? MessageStrings.User;
						}

						// ViewBag.OrganizationId = Session["OrganizationId"];
					}
				}
			}
			//DataCollection.CommentMarkProfile(2, "End");
			base.OnActionExecuting(filterContext);
		}


		protected void ValidateValues<T>(T model, params Expression<Func<T, object>>[] selectors)
		{
			foreach (var e in selectors)
			{
				//var meta = ModelMetadata.FromLambdaExpression(e, new ViewDataDictionary<T>());
				var name = e.GetMvcName();//meta.DisplayName;
				if (!ToValidate.Remove(name))
					throw new PermissionsException("Validation item does not exist.");
				SecuredValueValidator.ValidateValue(ValidationCollection, name);
			}
		}

		protected override void OnActionExecuted(ActionExecutedContext filterContext)
		{
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

			base.OnActionExecuted(filterContext);
		}

		protected ActionResult RedirectToLocal(string returnUrl)
		{
			if (Url.IsLocalUrl(returnUrl))
				return Redirect(returnUrl);
			else
				throw new RedirectException("Return URL is invalid.");
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

		protected new JsonResult Json(object data)
		{
			if (data is IAngular && Request.Params["transform"]==null)
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
		protected new  JsonResult Json(object data, string contentType, JsonRequestBehavior behavior)
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
	}

}