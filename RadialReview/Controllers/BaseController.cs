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

        protected UserModel GetUser()
        {
            var id = User.Identity.GetUserId();
            return _UserAccessor.GetUser(id);
        }
        protected List<UserOrganizationModel> GetUserOrganization(Boolean full = false)
        {
            var id = User.Identity.GetUserId();
            return _UserAccessor.GetUserOrganizations(id, full);
        }

        protected UserOrganizationModel GetUserOrganization(long organizationId, Boolean full = false)
        {
            var id = User.Identity.GetUserId();
            return _UserAccessor.GetUserOrganizations(id, organizationId, full);
        }

        protected UserOrganizationModel GetOneUserOrganization(long? organizationId, Boolean full = false)
        {
            long sessionOrgId = 0;
            if (organizationId==null && long.TryParse((string)Session["organizationId"],out sessionOrgId))
            {
                organizationId = sessionOrgId;
            }

            if (organizationId == null)
            {
                var found = GetUserOrganization(full);
                if (found.Count == 0)
                    throw new PermissionsException();
                else if (found.Count == 1)
                    return found.First();
                else
                    throw new OrganizationIdException();
            }
            else
            {
                return GetUserOrganization(organizationId.Value, full);
            }
        }

        /*
        public UserOrganizationModel GetUserOrganization(UserModel user,int organizationId)
        {
            return _UserAccessor.GetUserOrganizations(user, organizationId);
        }*/

        protected override void OnException(ExceptionContext filterContext)
        {
            if (filterContext.ExceptionHandled)
                return;

            if (filterContext.Exception is LoginException)
            {
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
                filterContext.Result = RedirectToAction("ManagingList", "Organization", new { message = filterContext.Exception.Message, returnUrl = redirectUrl });
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
            if (IsLoggedIn())
            {
                var userOrgs= GetUserOrganization();
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
                    var user = GetUser();
                    filterContext.Controller.ViewBag.UserName = user.Name() ?? MessageStrings.User;
                }
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

    }
}