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


namespace RadialReview.Controllers
{
    public class BaseController : Controller
    {
        protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected static UserAccessor _UserAccessor = new UserAccessor();

        protected void EditableOrException(UserOrganizationModel user)
        {
            if (!user.IsManagerCanEditOrganization())
                throw new PermissionsException();
        }

        protected UserModel GetUserModel()
        {
            return HttpContextUtility.Get(HttpContext, "User", x =>
            {
                var id = User.Identity.GetUserId();
                return _UserAccessor.GetUser(id);
            }, false);
        }
        protected List<UserOrganizationModel> GetUserOrganizations()//Boolean full = false)
        {
            var id = User.Identity.GetUserId();
            return _UserAccessor.GetUserOrganizations(id/*, full*/);
        }

        private UserOrganizationModel GetUserOrganization(long organizationId)//, Boolean full = false)
        {
            return HttpContextUtility.Get(HttpContext,"UserOrganization",x=>{
                var id = User.Identity.GetUserId();
                return _UserAccessor.GetUserOrganizations(id, organizationId/*, full*/);
            },x=>x.Organization.Id!=organizationId);
        }

        private UserOrganizationModel _CurrentUser = null;
        private long? _CurrentUserOrganizationId   = null;

        protected UserOrganizationModel GetUser(long? organizationId = null)//long? organizationId, Boolean full = false)
        {

            if (organizationId == null)
            {
                var orgIdParam = Request.Params.Get("organizationId");
                if (orgIdParam != null)
                    organizationId = long.Parse(orgIdParam);
            }


            if (organizationId==null && Session["organizationId"]!=null )
            {
                organizationId = (long)Session["organizationId"];
            }

            if (organizationId!=null)
            {
                Session["organizationId"] = organizationId.Value;
            }

            if (_CurrentUser != null && organizationId == _CurrentUserOrganizationId)
                return _CurrentUser;

            if (organizationId == null)
            {
                var found = GetUserOrganizations();
                if (found.Count == 0)
                    throw new PermissionsException();
                else if (found.Count == 1)
                {
                    _CurrentUser=found.First();
                    _CurrentUserOrganizationId = _CurrentUser.Organization.Id;
                    Session["organizationId"] = _CurrentUserOrganizationId;
                    return _CurrentUser;
                }
                else
                    throw new OrganizationIdException();
            }
            else
            {
                _CurrentUser = GetUserOrganization(organizationId.Value);
                _CurrentUserOrganizationId = _CurrentUser.Organization.Id;
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
            AuthenticationManager.SignOut();
        }

        protected override void OnException(ExceptionContext filterContext)
        {
            if (filterContext.ExceptionHandled)
                return;

            if (filterContext.Exception is LoginException)
            {
                SignOut();
                var redirectUrl = ((RedirectException)filterContext.Exception).RedirectUrl;
                if (redirectUrl == null)
                    redirectUrl = Request.Url.PathAndQuery;
                log.Info("Login: [" + Request.Url.PathAndQuery+"] --> ["+redirectUrl+"]");
                filterContext.Result = RedirectToAction("Login", "Account", new { message = filterContext.Exception.Message, returnUrl = redirectUrl });
                filterContext.ExceptionHandled = true;
                filterContext.HttpContext.Response.Clear();
            }
            else if (filterContext.Exception is OrganizationIdException)
            {
                var redirectUrl=((RedirectException)filterContext.Exception).RedirectUrl;
                log.Info("Organization: [" + Request.Url.PathAndQuery + "] --> [" + redirectUrl + "]");
                filterContext.Result = RedirectToAction("ManageList", "Organization", new { message = filterContext.Exception.Message, returnUrl = redirectUrl });
                filterContext.ExceptionHandled = true;
                filterContext.HttpContext.Response.Clear();
            }
            else if (filterContext.Exception is PermissionsException)
            {
                var returnUrl = ((RedirectException)filterContext.Exception).RedirectUrl;
                log.Info("Permissions: [" + Request.Url.PathAndQuery + "] --> [" + returnUrl + "]");
                ViewBag.Message=filterContext.Exception.Message;
                filterContext.Result = View("~/Views/Error/Index.cshtml");
                filterContext.ExceptionHandled = true;
                filterContext.HttpContext.Response.Clear();
            }
            else if (filterContext.Exception is RedirectException)
            {
                var returnUrl=((RedirectException)filterContext.Exception).RedirectUrl;
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

            if (IsLoggedIn())
            {
                var userOrgs= GetUserOrganizations();
                var oneUser = userOrgs.FirstOrDefault();
                filterContext.Controller.ViewBag.UserName = MessageStrings.User;
                filterContext.Controller.ViewBag.IsManager = false;
                
                if (oneUser != null)
                {
                    filterContext.Controller.ViewBag.UserName = oneUser.Name();
                    filterContext.Controller.ViewBag.IsManager = userOrgs.Any(x => x.ManagerAtOrganization || x.ManagingOrganization);
                }
                else
                {
                    var user = GetUserModel();
                    filterContext.Controller.ViewBag.UserName = user.Name() ?? MessageStrings.User;
                }

                ViewBag.OrganizationId = Session["OrganizationId"];
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