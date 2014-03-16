using RadialReview.Accessors;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using RadialReview.Exceptions;
using System.Web.Routing;
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


namespace RadialReview.Controllers
{
    public class BaseController : Controller
    {
        protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected static UserEngine _UserEngine = new UserEngine();
        protected static ChartsEngine _ChartsEngine = new ChartsEngine();
        protected static ReviewEngine _ReviewEngine = new ReviewEngine();

        protected static UserAccessor _UserAccessor = new UserAccessor();
        protected static TaskAccessor _TaskAccessor = new TaskAccessor();
        protected static TeamAccessor _TeamAccessor = new TeamAccessor();
        protected static NexusAccessor _NexusAccessor = new NexusAccessor();
        protected static ImageAccessor _ImageAccessor = new ImageAccessor();
        protected static GroupAccessor _GroupAccessor = new GroupAccessor();
        protected static OriginAccessor _OriginAccessor = new OriginAccessor();
        protected static ReviewAccessor _ReviewAccessor = new ReviewAccessor();
        protected static PaymentAccessor _PaymentAccessor = new PaymentAccessor();
        protected static KeyValueAccessor _KeyValueAccessor = new KeyValueAccessor();
        protected static PositionAccessor _PositionAccessor = new PositionAccessor();
        protected static QuestionAccessor _QuestionAccessor = new QuestionAccessor();
        protected static CategoryAccessor _CategoryAccessor = new CategoryAccessor();
        protected static PrereviewAccessor _PrereviewAccessor = new PrereviewAccessor();
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
            return HttpContextUtility.Get(HttpContext, "User", x =>
            {
                var id = User.Identity.GetUserId();
                return _UserAccessor.GetUserById(id);
            }, false);
        }
        protected List<UserOrganizationModel> GetUserOrganizations()//Boolean full = false)
        {
            var id = User.Identity.GetUserId();
            return _UserAccessor.GetUserOrganizations(id/*, full*/);
        }

        private UserOrganizationModel GetUserOrganization(long userOrganizationId)//, Boolean full = false)
        {
            return HttpContextUtility.Get(HttpContext, "UserOrganization", x =>
            {
                var id = User.Identity.GetUserId();
                return _UserAccessor.GetUserOrganizations(id, userOrganizationId/*, full*/);
            }, x => x.Id != userOrganizationId);
        }

        private UserOrganizationModel _CurrentUser = null;
        private long? _CurrentUserOrganizationId = null;
        /*
        protected void ChangeRole(long roleId)
        {
            _UserAccessor.ChangeRole(GetUserModel(),, roleId);
        }
        */
        protected UserOrganizationModel GetUser(long? userOrganizationId = null)//long? organizationId, Boolean full = false)
        {
            /**/
            if (userOrganizationId == null)
            {
                var orgIdParam = Request.Params.Get("organizationId");
                if (orgIdParam != null)
                    userOrganizationId = long.Parse(orgIdParam);
            }

            if (userOrganizationId == null && Session["UserOrganizationId"] != null)
            {
                userOrganizationId = (long)Session["UserOrganizationId"];
            }
            if (userOrganizationId == null)
            {
                userOrganizationId = GetUserModel().GetCurrentRole();
            }


            if (_CurrentUser != null && userOrganizationId == _CurrentUserOrganizationId)
                return _CurrentUser;

            if (userOrganizationId == null)
            {
                var found = GetUserOrganizations();
                if (found.Count == 0)
                    throw new NoUserOrganizationException();
                else if (found.Count == 1)
                {
                    _CurrentUser = found.First();
                    _CurrentUserOrganizationId = _CurrentUser.Id;
                    Session["UserOrganizationId"] = _CurrentUserOrganizationId;
                    return _CurrentUser;
                }
                else
                    throw new OrganizationIdException();
            }
            else
            {
                _CurrentUser = GetUserOrganization(userOrganizationId.Value);
                _CurrentUserOrganizationId = _CurrentUser.Id;
                Session["UserOrganizationId"] = userOrganizationId.Value;
                return _CurrentUser;
            }
        }

        /*
        public UserOrganizationModel GetUserOrganization(UserModel user,int organizationId)
        {
            return _UserAccessor.GetUserOrganizations(user, organizationId);
        }*/

        protected void SignOut()
        {
            Session["UserOrganizationId"] = null;
            AuthenticationManager.SignOut();
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

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // eat the cookie (if any) and set the culture
            if (Request.Cookies["lang"] != null)
            {
                HttpCookie cookie = Request.Cookies["lang"];
                string lang = cookie.Value;
                var culture = new System.Globalization.CultureInfo(lang);
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
            }
            
            filterContext.Controller.ViewBag.IsLocal = ServerUtility.GetConfigValue("BaseUrl").Contains("localhost");
            
            filterContext.Controller.ViewBag.HasBaseController = true;
            if (IsLoggedIn())
            {
                var userOrgs = GetUserOrganizations();
                UserOrganizationModel oneUser = null;
                try
                {
                    oneUser = GetUser();
                }
                catch (OrganizationIdException)
                {
                }
                catch (NoUserOrganizationException)
                {
                }

                filterContext.Controller.ViewBag.UserName = MessageStrings.User;
                filterContext.Controller.ViewBag.IsManager = false;
                filterContext.Controller.ViewBag.Organizations = userOrgs.Count();
                filterContext.Controller.ViewBag.Hints = GetUserModel().Hints;
                filterContext.Controller.ViewBag.ManagingOrganization = false;

                if (oneUser != null)
                {
                    HtmlString name = new HtmlString(oneUser.GetName());

                    if (userOrgs.Count > 1)
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

                    filterContext.Controller.ViewBag.TaskCount = _TaskAccessor.GetUnstartedTaskCountForUser(oneUser, oneUser.Id, DateTime.UtcNow);
                    //filterContext.Controller.ViewBag.Hints = oneUser.User.Hints;
                    filterContext.Controller.ViewBag.UserName = name;
                    filterContext.Controller.ViewBag.IsManager = oneUser.ManagerAtOrganization || oneUser.ManagingOrganization || oneUser.IsRadialAdmin;
                    filterContext.Controller.ViewBag.ManagingOrganization = oneUser.ManagingOrganization || oneUser.IsRadialAdmin;
                    filterContext.Controller.ViewBag.UserId = oneUser.Id;
                    filterContext.Controller.ViewBag.OrganizationId = oneUser.Organization.Id;
                }
                else
                {
                    var user = GetUserModel();
                    filterContext.Controller.ViewBag.UserName = user.Name() ?? MessageStrings.User;
                }

                // ViewBag.OrganizationId = Session["OrganizationId"];

            }

            //Access Level Filtering
            var accessAttributes = filterContext.ActionDescriptor.GetCustomAttributes(typeof(AccessAttribute), false).Cast<AccessAttribute>();
            if (accessAttributes.Count() == 0)
                throw new NotImplementedException("Access attribute missing.");

            switch ((AccessLevel)accessAttributes.Min(x => (int)x.AccessLevel))
            {
                case AccessLevel.Any: break;
                case AccessLevel.User: GetUserModel(); break;
                case AccessLevel.UserOrganization: GetUser(); break;
                case AccessLevel.Manager: if (!GetUser().IsManager()) throw new PermissionsException("You must be a manager to view this resource."); break;
                case AccessLevel.Radial: if (!(GetUserModel().IsRadialAdmin || GetUser().IsRadialAdmin)) throw new PermissionsException("You must be a Radial Admin to view this resource."); break;
                default: throw new Exception("Unknown Access Type");
            }

            base.OnActionExecuting(filterContext);
        }

        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
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
    }
}