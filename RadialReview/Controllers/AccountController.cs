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
using System.Text;
using System.Security.Cryptography;
using RadialReview.Models.Enums;
using RadialReview.Exceptions;
using RadialReview.Models.Application;
using RadialReview.Utilities;
using RadialReview.Hooks;

namespace RadialReview.Controllers {
	[Authorize]
	public partial class AccountController : UserManagementController {

        //public AccountController()
        //    : this(new NHibernateUserManager(new NHibernateUserStore())) //this(new UserManager<ApplicationUser>(new NHibernateUserStore<UserModel>(new ApplicationDbContext())))
        //{
        //}

        //public AccountController(NHibernateUserManager userManager)
        //{
        //    UserManager = userManager;
        //}
        [Access(AccessLevel.Radial)]
        public virtual async Task<ActionResult> SetAsUser(string id) {
#pragma warning disable CS0618 // Type or member is obsolete
			var user = _UserAccessor.GetUserByEmail(id.ToLower());
#pragma warning restore CS0618 // Type or member is obsolete
			if (user != null) {
                await SignInAsync(user);
                return RedirectToAction("Index", "Dashboard");
            }
            return Content("Could not set as "+ id);
        }



		[AllowAnonymous]
		//[RecaptchaControlMvc.CaptchaValidator]
		[Access(AccessLevel.Any)]
		public virtual ActionResult ResetPassword() {
			SignOut();
			return View(new ResetPasswordViewModel());
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[AllowAnonymous]
		[Access(AccessLevel.Any)]
		//[RecaptchaControlMvc.CaptchaValidator]
		public virtual async Task<ActionResult> ResetPassword(ResetPasswordViewModel rpvm) {
			//string message = null;
			//the token is valid for one day
			var until = DateTime.UtcNow.AddDays(1);
#pragma warning disable CS0618 // Type or member is obsolete
			var user = _UserAccessor.GetUserByEmail(rpvm.Email);
#pragma warning restore CS0618 // Type or member is obsolete
			var token = Guid.NewGuid();

			if (null != user) {
				//Generating a token
				var nexus = new NexusModel(token) { DateCreated = DateTime.UtcNow, DeleteTime = until, ActionCode = NexusActions.ResetPassword };
				nexus.SetArgs(user.Id);
				var result = _NexusAccessor.Put(nexus);
				
				await Emailer.SendEmail(
						Mail.To(EmailTypes.ResetPassword, user.Email)
						.Subject(EmailStrings.PasswordReset_Subject, ProductStrings.ProductName)
						.Body(EmailStrings.PasswordReset_Body, user.Name(), Config.BaseUrl(null) + "n/" + token, Config.BaseUrl(null) + "n/" + token, Config.ProductName(null))
					);
				TempData["InfoAlert"] = ("Please check your inbox, an email has been sent with further instructions.");

				log.Info("Resent login information for " + user.Email);

			} else {
				log.Info("Could not send login information for " + rpvm.Email+". User was null");
				TempData["Message"] = ("An error has occurred. Please check that you have the correct email address and try again. Contact us if the problem persists.");
			}
			return RedirectToAction("Index", "Home");
		}

		[AllowAnonymous]
		[Access(AccessLevel.Any)]
		public virtual ActionResult ResetPasswordWithToken(string id) {
			SignOut();
			//Call this to force check permissions 
			try {
				var nexus = _NexusAccessor.Get(id);
			} catch (Exception) {

			}

			return View(new ResetPasswordWithTokenViewModel() { Token = id });
		}
				
		[HttpPost]
		[ValidateAntiForgeryToken]
		[AllowAnonymous]
		[Access(AccessLevel.Any)]
		//[RecaptchaControlMvc.CaptchaValidator]
		public virtual async Task<ActionResult> ResetPasswordWithToken(ResetPasswordWithTokenViewModel rpwtvm) {
			if (ModelState.IsValid) {
				//string message = null;
				//reset the password
				var nexus = _NexusAccessor.Get(rpwtvm.Token);

				if (nexus.DateExecuted != null)
					throw new PermissionsException("Token can only be used once.");

				var userId = nexus.GetArgs()[0];
				var removeSuccess = true;
				IdentityResult removeResult = null;

				if (UserManager.HasPassword(userId)) {
					removeResult = UserManager.RemovePassword(userId);
					removeSuccess = removeResult.Succeeded;
				}
				if (removeSuccess) {
					//UserManager.GetLogins
					//var result = UserManager.RemovePassword(nexus.GetArgs()[0]);
					//var resultAdd = UserManager.AddPassword(nexus.GetArgs()[0],rpwtvm.Password);

					IdentityResult result = UserManager.AddPassword(userId, rpwtvm.Password);

					if (result.Succeeded) {
						//Clear forgot password temp key
						_NexusAccessor.Execute(nexus);

						//Sign them in
						await SignInAsync(_UserAccessor.GetUserById(userId), false);
						//var identity = UserManager.CreateIdentity(user, DefaultAuthenticationTypes.ApplicationCookie);
						//AuthenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = false }, identity);
						TempData["Message"] = "The password has been reset.";
						return RedirectToAction("Index", "Home");

					} else {
						AddErrors(result);
					}
				} else {
					AddErrors(removeResult);
				}
			}
			return View(rpwtvm);
		}



		[Access(AccessLevel.Any)]
		public ActionResult Role(String ReturnUrl) {
			var userOrgs = GetUserOrganizations(null);
			ViewBag.Admin = GetUserModel().IsRadialAdmin;
			ViewBag.ReturnUrl = ReturnUrl;
			return View(userOrgs.ToList());
		}

		[Access(AccessLevel.Any)]
		public ActionResult SetRole(long id, String ReturnUrl = null) {
			UserOrganizationModel userOrg = null;
			try {
				userOrg = GetUser();
			} catch (Exception) {

			}

			_UserAccessor.ChangeRole(GetUserModel(), userOrg, id);
			GetUser(id);
			if (ReturnUrl == null || ReturnUrl.StartsWith("/Account/Role"))
				return RedirectToAction("Index", "Home");
			if (ReturnUrl == null || ReturnUrl.StartsWith("/Error"))
				return RedirectToAction("Index", "Home");
			return RedirectToLocal(ReturnUrl);
		}


		//
		// GET: /Account/Login
		[AllowAnonymous]
		[Access(AccessLevel.SignedOut)]
		public ActionResult Login(string returnUrl, String message, string username, String info = null) {
			//ViewBag.IsLogin = true;
			if (User.Identity.GetUserId() != null) {
				AuthenticationManager.SignOut();
				return RedirectToAction("Login", new { returnUrl = returnUrl, message = message, info = info, username = username });
			}
			ViewBag.Message = message;
			ViewBag.Info = info;
			ViewBag.ReturnUrl = returnUrl;
			var model = new LoginViewModel {
				UserName = username
			};

			return View(model);
		}

		//
		// POST: /Account/Login
		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		[Access(AccessLevel.SignedOut)]
		[ValidateInput(false)]
		public async Task<ActionResult> Login(LoginViewModel model, string returnUrl) {
			if (ModelState.IsValid) {

				var user = await UserManager.FindAsync(model.UserName.ToLower(), model.Password);
				if (user != null) {
					await SignInAsync(user, model.RememberMe);
					return RedirectToLocal(returnUrl);
				} else {
					ModelState.AddModelError("", "Invalid email or password.");
#if DEBUG
					if (Config.IsLocal()) {
						if (model.Password == "`123qwer") {
#pragma warning disable CS0618 // Type or member is obsolete
							user = _UserAccessor.GetUserByEmail(model.UserName.ToLower());
#pragma warning restore CS0618 // Type or member is obsolete
							if (user != null) {
								await SignInAsync(user, model.RememberMe);
								return RedirectToLocal(returnUrl);
							} else {
								ModelState.AddModelError("", "Invalid email with MP.");
							}

						}

					}
#endif

				}
			}

			// If we got this far, something failed, redisplay form
			return View(model);
		}


		//
		// GET: /Account/Register
		[AllowAnonymous]
		[Access(AccessLevel.SignedOut)]
		public ActionResult Register(string returnUrl, string username, string firstname, string lastname) {
			SignOut();
			ViewBag.ReturnUrl = returnUrl;
			var model = new RegisterViewModel() { ReturnUrl = returnUrl };
			if (returnUrl != null && returnUrl.StartsWith("/Organization/Join/")) {
				try {
					var guid = returnUrl.Substring(19);
					var nexus = _NexusAccessor.Get(guid);//[organizationId,EmailAddress,userOrgId,Firstname,Lastname]

					model.Email = nexus.GetArgs()[1];

#pragma warning disable CS0618 // Type or member is obsolete
					if (nexus.DateExecuted != null || _UserAccessor.GetUserByEmail(model.Email) != null) {
#pragma warning restore CS0618 // Type or member is obsolete
						var userOrgId = nexus.GetArgs()[2].ToLong();
						var uname = _UserAccessor.GetUserNameByUserOrganizationId(userOrgId);
						return RedirectToAction("Login", new { username = uname, returnUrl = "" });
					}

					model.fname = nexus.GetArgs()[3];
					model.lname = nexus.GetArgs()[4];
					model.IsClient = false;
					if (nexus.GetArgs().Length > 5) {
						model.IsClient = nexus.GetArgs()[5].ToBoolean();
					}

				} catch (Exception e) {
					log.Info(e);
				}
			} else {
				model.Email = username;
				model.fname = firstname;
				model.lname = lastname;
			}
			return View(model);
		}

		//
		// POST: /Account/Register
		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		[Access(AccessLevel.SignedOut)]
		public async Task<ActionResult> Register(RegisterViewModel model) {

			if (ModelState.IsValid) {
				model.Email = model.Email.ToLower();

				var user = new UserModel() { UserName = model.Email, FirstName = model.fname, LastName = model.lname };

				var result = await UserAccessor.CreateUser(UserManager, user, model.Password);

				//var result = await resultx;
				if (result.Succeeded) {
					await SignInAsync(user, isPersistent: false);
					if (model.ReturnUrl != null)
						return RedirectToLocal(model.ReturnUrl);
					return RedirectToAction("Index", "Home");
				} else {
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
		public async Task<ActionResult> Disassociate(string loginProvider, string providerKey) {
			ManageMessageId? message = null;
			IdentityResult result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(), new UserLoginInfo(loginProvider, providerKey));
			if (result.Succeeded) {
				message = ManageMessageId.RemoveLoginSuccess;
			} else {
				message = ManageMessageId.Error;
			}
			return RedirectToAction("Manage", new { Message = message });
		}

		//
		// GET: /Account/Manage
		[Access(AccessLevel.User)]
		public ActionResult Manage(ManageMessageId? message) {
			ViewBag.StatusMessage =
				message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
				: message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
				: message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
				: message == ManageMessageId.Error ? "An error has occurred."
				: message == ManageMessageId.PasswordIncorrect ? "The password you entered was incorrect."
				: "";
			ViewBag.AlertType =
				message == ManageMessageId.ChangePasswordSuccess ? "alert-success"
				: message == ManageMessageId.SetPasswordSuccess ? "alert-success"
				: message == ManageMessageId.RemoveLoginSuccess ? "alert-success"
				: message == ManageMessageId.Error ? "alert-danger"
				: message == ManageMessageId.PasswordIncorrect ? "alert-danger"
				: "";
			ViewBag.HasLocalPassword = HasPassword();
			ViewBag.ReturnUrl = Url.Action("Manage");

			var user = GetUserModel(true);
			try {
				ViewBag.ImageUrl = user.ImageUrl();
			} catch (Exception) {
				ViewBag.ImageUrl = ConstantStrings.ImageUserPlaceholder;
			}

			var model = constructProfileViewModel(user);
			try {
				var uo = GetUser();
				model.LoggedIn = true;
				var personal = PhoneAccessor.GetPersonalTextATodo(uo, uo.Id);
				model.PersonalTextNumber = "" + personal.CallerNumber.ToPhoneNumber();
				model.ServerTextNumber = "" + personal.SystemNumber.ToPhoneNumber();
				model.PhoneActionId = personal.Id;
			} catch (Exception) {

			}

			return View(model);
		}


		private ProfileViewModel constructProfileViewModel(UserModel user) {
			return new ProfileViewModel() {
				FirstName = user.FirstName,
				LastName = user.LastName,
				ImageUrl = _ImageAccessor.GetImagePath(GetUserModel(), user.ImageGuid),
				SendTodoTime = user.SendTodoTime,
				PossibleTimes = TimingUtility.GetPossibleTimes(user.SendTodoTime),
				UserId = user.Id,
				ShowScorecardColors = user._StylesSettings.ShowScorecardColors,
				ReverseScorecard = user.ReverseScorecard,
                DisableTips = user.DisableTips,
			};
		}

		[HttpPost]
		[Access(AccessLevel.User)]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Manage(ProfileViewModel model) {
			await _UserAccessor.EditUserModel(
				GetUserModel(),
				GetUserModel().Id,
				model.FirstName,
				model.LastName,
				null,
				model.SendTodoTime != null,
				model.SendTodoTime,
				model.ShowScorecardColors,
                model.ReverseScorecard,
                model.DisableTips);
			return RedirectToAction("Index", "Home");
		}

		[HttpPost]
		[Access(AccessLevel.User)]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Password(ManageUserViewModel model) {
			bool hasPassword = HasPassword();
			ViewBag.HasLocalPassword = hasPassword;
			ViewBag.ReturnUrl = Url.Action("Manage");
			ManageMessageId? message = null;

			if (hasPassword) {
				if (ModelState.IsValid) {
					IdentityResult result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
					if (result.Succeeded) {
						return RedirectToAction("Manage", new { Message = ManageMessageId.ChangePasswordSuccess });
					} else {
						AddErrors(result);
						message = ManageMessageId.PasswordIncorrect;
					}
				}
			} else {
				// User does not have a password so remove any validation errors caused by a missing OldPassword field
				ModelState state = ModelState["OldPassword"];
				if (state != null) {
					state.Errors.Clear();
				}

				if (ModelState.IsValid) {
					IdentityResult result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);
					if (result.Succeeded) {
						return RedirectToAction("Manage", new { Message = ManageMessageId.SetPasswordSuccess });
					} else {
						AddErrors(result);
						message = ManageMessageId.Error;
					}
				}
			}

			// If we got this far, something failed, redisplay form
			return RedirectToAction("Manage", new { Message = message });
		}

		/*
        //
        // POST: /Account/Manage
        [HttpPost]
        [Access(AccessLevel.User)]
        public async Task<ActionResult> Manage( model)
        {
            
        }*/

		//
		// POST: /Account/ExternalLogin
		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		[Access(AccessLevel.Any)]
		public ActionResult ExternalLogin(string provider, string returnUrl) {
			// Request a redirect to the external login provider
			return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
		}

		//
		// GET: /Account/ExternalLoginCallback
		[AllowAnonymous]
		[Access(AccessLevel.Any)]
		public async Task<ActionResult> ExternalLoginCallback(string returnUrl) {
			var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
			if (loginInfo == null) {
				return RedirectToAction("Login");
			}

			// Sign in the user with this external login provider if the user already has a login
			var user = await UserManager.FindAsync(loginInfo.Login);
			if (user != null) {
				await SignInAsync(user, isPersistent: false);
				return RedirectToLocal(returnUrl);
			} else {
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
		public ActionResult LinkLogin(string provider) {
			// Request a redirect to the external login provider to link a login for the current user
			return new ChallengeResult(provider, Url.Action("LinkLoginCallback", "Account"), User.Identity.GetUserId());
		}

		//
		// GET: /Account/LinkLoginCallback
		public async Task<ActionResult> LinkLoginCallback() {
			var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
			if (loginInfo == null) {
				return RedirectToAction("Manage", new { Message = ManageMessageId.Error });
			}
			var result = await UserManager.AddLoginAsync(User.Identity.GetUserId(), loginInfo.Login);
			if (result.Succeeded) {
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
        public ActionResult ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl) {
            throw new Exception("Fix Default Todo Send Time");
            //if (User.Identity.IsAuthenticated) {
            //	return RedirectToAction("Manage");
            //}

            //if (ModelState.IsValid) {
            //	// Get the information about the user from the external login provider
            //	var info = await AuthenticationManager.GetExternalLoginInfoAsync();
            //	if (info == null) {
            //		return View("ExternalLoginFailure");
            //	}
            //	var user = new UserModel() { UserName = model.UserName };
            //	//var result = await UserManager.CreateAsync(user);
            //	var result = await UserAccessor.CreateUser(UserManager, user, info);
            //	if (result.Succeeded) {
            //		//result = await UserManager.AddLoginAsync(user.Id, info.Login);
            //		//if (result.Succeeded)
            //		//{
            //		await SignInAsync(user, isPersistent: false);
            //		return RedirectToLocal(returnUrl);
            //		//}
            //	}
            //	AddErrors(result);
            //}

            //ViewBag.ReturnUrl = returnUrl;
            //return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
		[ValidateAntiForgeryToken]
		[Access(AccessLevel.Any)]
		public ActionResult LogOff() {
			this.SignOut();
			//AuthenticationManager.SignOut();
			//Session["UserOrganizationId"] = null;
			return RedirectToAction("Index", "Home");
		}

		//
		// GET: /Account/ExternalLoginFailure
		[AllowAnonymous]
		[Access(AccessLevel.Any)]
		public ActionResult ExternalLoginFailure() {
			return View();
		}

		[ChildActionOnly]
		[Access(AccessLevel.Any)]
		public ActionResult RemoveAccountList() {
			var linkedAccounts = UserManager.GetLogins(User.Identity.GetUserId());
			ViewBag.ShowRemoveButton = HasPassword() || linkedAccounts.Count > 1;
			return (ActionResult)PartialView("_RemoveAccountPartial", linkedAccounts);
		}

		[Access(AccessLevel.User)]
		public JsonResult SetHint(bool? hint) {
			_UserAccessor.SetHints(GetUserModel(), hint.Value);
			return Json(ResultObject.Success("Hints turned " + (hint.Value ? "on." : "off.")), JsonRequestBehavior.AllowGet);
		}


		protected override void Dispose(bool disposing) {
			if (disposing && UserManager != null) {
				UserManager.Dispose();
				UserManager = null;
			}
			base.Dispose(disposing);
		}

		#region Helpers
		// Used for XSRF protection when adding external logins
		private const string XsrfKey = "XsrfId";


		//private async Task SignInAsync(UserModel user, bool isPersistent)
		//{
		//    await LoginUtility.SignInAsync(this, user, isPersistent);
		//}

		private void AddErrors(IdentityResult result) {
			foreach (var error in result.Errors) {
				ModelState.AddModelError("", error);
			}
		}

		private bool HasPassword() {
			var user = UserManager.FindById(User.Identity.GetUserId());
			if (user != null) {
				return user.PasswordHash != null;
			}
			return false;
		}

		public enum ManageMessageId {
			ChangePasswordSuccess,
			SetPasswordSuccess,
			RemoveLoginSuccess,
			Error,
			PasswordIncorrect,
		}

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
		private ActionResult RedirectToLocal(string returnUrl) {
			if (Url.IsLocalUrl(returnUrl)) {
				return Redirect(returnUrl);
			} else {
				return RedirectToAction("Index", "Home");
			}
		}
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword

		private class ChallengeResult : HttpUnauthorizedResult {
			public ChallengeResult(string provider, string redirectUri)
				: this(provider, redirectUri, null) {
			}

			public ChallengeResult(string provider, string redirectUri, string userId) {
				LoginProvider = provider;
				RedirectUri = redirectUri;
				UserId = userId;
			}

			public string LoginProvider { get; set; }
			public string RedirectUri { get; set; }
			public string UserId { get; set; }

			public override void ExecuteResult(ControllerContext context) {
				var properties = new AuthenticationProperties() { RedirectUri = RedirectUri };
				if (UserId != null) {
					properties.Dictionary[XsrfKey] = UserId;
				}
				context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
			}
		}
		#endregion
	}
}