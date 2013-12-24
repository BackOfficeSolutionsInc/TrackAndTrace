using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security;
using RadialReview.Models;
using RadialReview.NHibernate;
using RadialReview.Properties;
using RadialReview.Models.Json;
using RadialReview.Accessors;
using RadialReview.Models.ViewModels;

namespace RadialReview.Controllers
{
    [Authorize]
    public partial class AccountController : BaseController
    {
        protected static NexusAccessor _NexusAccessor = new NexusAccessor();
        protected static ImageAccessor _ImageAccessor = new ImageAccessor();

        public AccountController() : this(new NHibernateUserManager(new NHibernateUserStore())) //this(new UserManager<ApplicationUser>(new NHibernateUserStore<UserModel>(new ApplicationDbContext())))
        {
        }

        public AccountController(NHibernateUserManager userManager)
        {
            UserManager = userManager;
        }


        [Access(AccessLevel.Any)]
        public ActionResult Role(String ReturnUrl)
        {
            var userOrgs = GetUserOrganizations();
            ViewBag.ReturnUrl = ReturnUrl;
            return View(userOrgs.ToList());
        }

        [Access(AccessLevel.Any)]
        public ActionResult SetRole(long id,String ReturnUrl=null)
        {
            UserOrganizationModel userOrg=null;
            try
            {
                userOrg = GetUser();
            }
            catch (Exception)
            {

            }

            _UserAccessor.ChangeRole(GetUserModel(),userOrg, id);
            GetUser(id);
            if (ReturnUrl == null || ReturnUrl.StartsWith("/Account/Role"))
                return RedirectToAction("Index", "Home");
            return RedirectToLocal(ReturnUrl);
        }

        public NHibernateUserManager UserManager { get; private set; }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        [Access(AccessLevel.Any)]
        public ActionResult Login(string returnUrl,String message)
        {
            if (User.Identity.GetUserId() != null)
            {
                AuthenticationManager.SignOut();
                return RedirectToAction("Login", new { returnUrl = returnUrl,message=message });
            }
            ViewBag.Message = message;
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [Access(AccessLevel.Any)]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {

                var user = await UserManager.FindAsync(model.UserName.ToLower(), model.Password);
                if (user != null)
                {
                    await SignInAsync(user, model.RememberMe);
                    return RedirectToLocal(returnUrl);
                }
                else
                {
                    ModelState.AddModelError("", "Invalid email or password.");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        [Access(AccessLevel.Any)]
        public ActionResult Register(string returnUrl)
        {

            ViewBag.ReturnUrl = returnUrl;
            var model = new RegisterViewModel() { ReturnUrl=returnUrl };
            if (returnUrl!=null && returnUrl.StartsWith("/Organization/Join/"))
            {
                try
                {
                    var guid = returnUrl.Substring(19);
                    var nexus = _NexusAccessor.Get(guid);//[organizationId,EmailAddress,userOrgId,Firstname,Lastname]
                    model.Email = nexus.GetArgs()[1];
                    model.fname = nexus.GetArgs()[3];
                    model.lname = nexus.GetArgs()[4];
                }
                catch (Exception e)
                {
                    log.Info(e);
                }
            }
            return View(model);
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [Access(AccessLevel.Any)]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {

            if (ModelState.IsValid)
            {
                model.Email=model.Email.ToLower();

                var user = new UserModel() { UserName = model.Email, FirstName=model.fname,LastName=model.lname };
                var resultx = UserManager.CreateAsync(user, model.Password);
                var result = await resultx;
                if (result.Succeeded)
                {
                    await SignInAsync(user, isPersistent: false);
                    if (model.ReturnUrl != null)
                        return RedirectToLocal(model.ReturnUrl);
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    AddErrors(result);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // POST: /Account/Disassociate
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Access(AccessLevel.Any)]
        public async Task<ActionResult> Disassociate(string loginProvider, string providerKey)
        {
            ManageMessageId? message = null;
            IdentityResult result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(), new UserLoginInfo(loginProvider, providerKey));
            if (result.Succeeded)
            {
                message = ManageMessageId.RemoveLoginSuccess;
            }
            else
            {
                message = ManageMessageId.Error;
            }
            return RedirectToAction("Manage", new { Message = message });
        }

        //
        // GET: /Account/Manage
        [Access(AccessLevel.User)]
        public ActionResult Manage(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            ViewBag.HasLocalPassword = HasPassword();
            ViewBag.ReturnUrl = Url.Action("Manage");
            try
            {
                var user = GetUser();
                ViewBag.ImageUrl = user.ImageUrl();
            }catch(Exception e)
            {
                ViewBag.ImageUrl = ConstantStrings.ImageUserPlaceholder;
            }

            return View();
        }

        //
        // POST: /Account/Manage
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Access(AccessLevel.User)]
        public async Task<ActionResult> Manage(ManageUserViewModel model)
        {
            bool hasPassword = HasPassword();
            ViewBag.HasLocalPassword = hasPassword;
            ViewBag.ReturnUrl = Url.Action("Manage");
            if (hasPassword)
            {
                if (ModelState.IsValid)
                {
                    IdentityResult result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Manage", new { Message = ManageMessageId.ChangePasswordSuccess });
                    }
                    else
                    {
                        AddErrors(result);
                    }
                }
            }
            else
            {
                // User does not have a password so remove any validation errors caused by a missing OldPassword field
                ModelState state = ModelState["OldPassword"];
                if (state != null)
                {
                    state.Errors.Clear();
                }

                if (ModelState.IsValid)
                {
                    IdentityResult result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Manage", new { Message = ManageMessageId.SetPasswordSuccess });
                    }
                    else
                    {
                        AddErrors(result);
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [Access(AccessLevel.Any)]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        [Access(AccessLevel.Any)]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            // Sign in the user with this external login provider if the user already has a login
            var user = await UserManager.FindAsync(loginInfo.Login);
            if (user != null)
            {
                await SignInAsync(user, isPersistent: false);
                return RedirectToLocal(returnUrl);
            }
            else
            {
                // If the user does not have an account, then prompt the user to create an account
                ViewBag.ReturnUrl = returnUrl;
                ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { UserName = loginInfo.DefaultUserName });
            }
        }

        //
        // POST: /Account/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Access(AccessLevel.Any)]
        public ActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            return new ChallengeResult(provider, Url.Action("LinkLoginCallback", "Account"), User.Identity.GetUserId());
        }

        //
        // GET: /Account/LinkLoginCallback
        public async Task<ActionResult> LinkLoginCallback()
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
            if (loginInfo == null)
            {
                return RedirectToAction("Manage", new { Message = ManageMessageId.Error });
            }
            var result = await UserManager.AddLoginAsync(User.Identity.GetUserId(), loginInfo.Login);
            if (result.Succeeded)
            {
                return RedirectToAction("Manage");
            }
            return RedirectToAction("Manage", new { Message = ManageMessageId.Error });
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [Access(AccessLevel.Any)]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new UserModel() { UserName = model.UserName };
                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        await SignInAsync(user, isPersistent: false);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Access(AccessLevel.Any)]
        public ActionResult LogOff()
        {
            this.SignOut();
            //AuthenticationManager.SignOut();
            //Session["organizationId"] = null;
            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        [Access(AccessLevel.Any)]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        [ChildActionOnly]
        [Access(AccessLevel.Any)]
        public ActionResult RemoveAccountList()
        {
            var linkedAccounts = UserManager.GetLogins(User.Identity.GetUserId());
            ViewBag.ShowRemoveButton = HasPassword() || linkedAccounts.Count > 1;
            return (ActionResult)PartialView("_RemoveAccountPartial", linkedAccounts);
        }

        [Access(AccessLevel.User)]
        public JsonResult SetHint(bool? hint)
        {
            _UserAccessor.SetHints(GetUserModel(), hint.Value);
            return Json(ResultObject.Success,JsonRequestBehavior.AllowGet);
        }

        [Access(AccessLevel.User)]
        public ActionResult Profile()
        {
            var user=GetUserModel();

            return View(new ProfileViewModel() { 
                FirstName = user.FirstName,
                LastName = user.LastName,
                ImageUrl = _ImageAccessor.GetImagePath(GetUserModel(),user.ImageGuid)
            });
        }

        [HttpPost]
        [Access(AccessLevel.User)]
        public ActionResult Profile(ProfileViewModel model)
        {
            _UserAccessor.EditUserModel(GetUserModel(), GetUserModel().Id, model.FirstName,model.LastName,null);
            return RedirectToAction("Profile");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && UserManager != null)
            {
                UserManager.Dispose();
                UserManager = null;
            }
            base.Dispose(disposing);
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";


        private async Task SignInAsync(UserModel user, bool isPersistent)
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ExternalCookie);
            var identity = await UserManager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie);
            AuthenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = isPersistent }, identity);
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            Error
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        private class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri) : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties() { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}