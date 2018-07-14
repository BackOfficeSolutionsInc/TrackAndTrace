using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using Owin;
using System;
using RadialReview.NHibernate;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Security.DataHandler.Encoder;
using RadialReview.Utilities.Encrypt;
using RadialReview.Utilities;
using RadialReview.Areas.CoreProcess.Models;
using Hangfire;
using Hangfire.MySql;
using Hangfire.Dashboard;
using RadialReview.Models;
using RadialReview.Accessors;

namespace RadialReview {
	public partial class Startup {
		public static OAuthAuthorizationServerOptions OAuthOptions { get; private set; }


		// For more information on configuring authentication, please visit http://go.microsoft.com/fwlink/?LinkId=301864
		public void ConfigureAuth(IAppBuilder app) {
			// Enable the application to use a cookie to store information for the signed in user

			var cookie = new CookieAuthenticationOptions {
				AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
				LoginPath = new PathString("/Account/Login"),
			};

			app.UseCookieAuthentication(cookie);

			//if (!Config.IsLocal() && Config.GetCookieDomains()!=null) {
			//	var subDomainCookie = new CookieAuthenticationOptions {
			//		AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
			//		LoginPath = new PathString("/Account/Login"),
			//		CookieName = ".AspNet.Subcookies",
			//	};
			//	subDomainCookie.CookieDomain = Config.GetCookieDomains();// "traction.tools";
			//	app.UseCookieAuthentication(subDomainCookie);
			//}

			// Use a cookie to temporarily store information about a user logging in with a third party login provider
			app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);


			var PublicClientId = "self";
			OAuthOptions = new OAuthAuthorizationServerOptions {
				TokenEndpointPath = new PathString("/Token"),
				Provider = new ApplicationOAuthProvider(PublicClientId),
				AuthorizeEndpointPath = new PathString("/api/Account/ExternalLogin"),
				AccessTokenExpireTimeSpan = TimeSpan.FromDays(14),
				AllowInsecureHttp = true
			};

			// Enable the application to use bearer tokens to authenticate users
			app.UseOAuthBearerTokens(OAuthOptions);


			GlobalConfiguration.Configuration.UseRedisStorage(Config.GetHangfireConnectionString());
			//GlobalConfiguration.Configuration.UseStorage(new MySqlStorage(Config.GetHangfireConnectionString(), new MySqlStorageOptions()));

			app.UseHangfireDashboard("/hangfire", new DashboardOptions {
				Authorization = new[] { new HangfireAuth() }
			});

			app.UseHangfireServer(new BackgroundJobServerOptions() {
				//WorkerCount = 1,
				Queues = new[] { "critical", "conclusionemail", "generateqc" ,"default", "admin" }
			});

			// Uncomment the following lines to enable logging in with third party login providers
			//app.UseMicrosoftAccountAuthentication(
			//    clientId: "",
			//    clientSecret: "");

			//app.UseTwitterAuthentication(
			//   consumerKey: "",
			//   consumerSecret: "");

			//app.UseFacebookAuthentication(
			//   appId: "",
			//   appSecret: "");

			//app.UseGoogleAuthentication();



			////Added from https://dev.outlook.com/restapi/tutorial/dotnet

			//app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

			//app.UseCookieAuthentication(new CookieAuthenticationOptions());

			//app.UseOpenIdConnectAuthentication(
			//  new OpenIdConnectAuthenticationOptions {
			//	  ClientId = Config.Office365.AppId(),
			//	  Authority = "https://login.microsoftonline.com/common/v2.0",
			//	  Scope = "openid offline_access profile email " + string.Join(" ", Config.Office365.Scopes()),
			//	  RedirectUri = Config.Office365.RedirectUrl(),
			//	  PostLogoutRedirectUri = "/",
			//	  TokenValidationParameters = new TokenValidationParameters {
			//		  // For demo purposes only, see below
			//		  ValidateIssuer = false

			//		  // In a real multitenant app, you would add logic to determine whether the
			//		  // issuer was from an authorized tenant
			//		  //ValidateIssuer = true,
			//		  //IssuerValidator = (issuer, token, tvp) =>
			//		  //{
			//		  //  if (MyCustomTenantValidation(issuer))
			//		  //  {
			//		  //    return issuer;
			//		  //  }
			//		  //  else
			//		  //  {
			//		  //    throw new SecurityTokenInvalidIssuerException("Invalid issuer");
			//		  //  }
			//		  //}
			//	  },
			//	  Notifications = new OpenIdConnectAuthenticationNotifications {
			//		  AuthenticationFailed = OnAuthenticationFailed,
			//		  AuthorizationCodeReceived = OnAuthorizationCodeReceived
			//	  }
			//  }
			//);
		}

		//private Task OnAuthenticationFailed(AuthenticationFailedNotification<OpenIdConnectMessage,OpenIdConnectAuthenticationOptions> notification) {
		//	notification.HandleResponse();
		//	string redirect = "/Home/Error?message=" + notification.Exception.Message;
		//	if (notification.ProtocolMessage != null && !string.IsNullOrEmpty(notification.ProtocolMessage.ErrorDescription)) {
		//		redirect += "&debug=" + notification.ProtocolMessage.ErrorDescription;
		//	}
		//	notification.Response.Redirect(redirect);
		//	return Task.FromResult(0);
		//}

		//private Task OnAuthorizationCodeReceived(AuthorizationCodeReceivedNotification notification) {
		//	notification.HandleResponse();
		//	notification.Response.Redirect("/Home/Error?message=See Auth Code Below&debug=" + notification.Code);
		//	return Task.FromResult(0);
		//}


		public class HangfireAuth : IDashboardAuthorizationFilter {
			public bool Authorize(DashboardContext context) {
				// In case you need an OWIN context, use the next line, `OwinContext` class
				// is the part of the `Microsoft.Owin` package.
				var owinContext = new OwinContext(context.GetOwinEnvironment());

				// Allow all authenticated users to see the Dashboard (potentially dangerous).
				try {
					var user = new UserAccessor().GetUserById(owinContext.Authentication.User.Identity.GetUserId());
					if (user != null) {
						return user.IsRadialAdmin;
					}
				} catch (Exception e) {
					int a = 0;
				}
				return false;
			}
		}

		public class ApplicationOAuthProvider : OAuthAuthorizationServerProvider {
			private readonly string _publicClientId;

			public static NHibernateUserManager UserManager = new NHibernateUserManager(new NHibernateUserStore());
			public ApplicationOAuthProvider(string publicClientId) {
				if (publicClientId == null) {
					throw new ArgumentNullException("publicClientId");
				}

				_publicClientId = publicClientId;
			}


			public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context) {
				bool encrypt_key = false;
				try {
					using (var s = HibernateSession.GetCurrentSession()) {
						using (var tx = s.BeginTransaction()) {
							var getKey = s.Get<TokenIdentifier>(context.Password);
							if (getKey != null) {
								string decrypt_key = Crypto.DecryptStringAES(getKey.TokenKey, Config.SchedulerSecretKey());
								string userName = decrypt_key.Split('_')[1];
								if (userName == context.UserName) {
									s.Delete(getKey);
									tx.Commit();
									s.Flush();
									encrypt_key = true;
								}
							}
						}
					}
				} catch (Exception ex) {
				}


				//var user = ((key == encrypt_key) ?
				var user = ((encrypt_key) ?
			   await UserManager.FindByNameAsync(context.UserName) :
			   await UserManager.FindAsync(context.UserName, context.Password));
				//await UserManager.FindAsync(context.UserName, context.Password);

				if (user == null) {
					context.SetError("invalid_grant", "The user name or password is incorrect.");
					return;
				}

				var oAuthIdentity = await user.GenerateUserIdentityAsync(UserManager, OAuthDefaults.AuthenticationType);
				var cookiesIdentity = await user.GenerateUserIdentityAsync(UserManager, CookieAuthenticationDefaults.AuthenticationType);

				var properties = CreateProperties(user.UserName);
				var ticket = new AuthenticationTicket(oAuthIdentity, properties);
				context.Validated(ticket);
				context.Request.Context.Authentication.SignIn(cookiesIdentity);
			}

			public override Task TokenEndpoint(OAuthTokenEndpointContext context) {
				foreach (KeyValuePair<string, string> property in context.Properties.Dictionary) {
					context.AdditionalResponseParameters.Add(property.Key, property.Value);
				}

				return Task.FromResult<object>(null);
			}

			public override Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context) {
				// Resource owner password credentials does not provide a client ID.
				if (context.ClientId == null) {
					context.Validated();
				}

				return Task.FromResult<object>(null);
			}

			public override Task ValidateClientRedirectUri(OAuthValidateClientRedirectUriContext context) {
				if (context.ClientId == _publicClientId) {
					Uri expectedRootUri = new Uri(context.Request.Uri, "/");
					if (expectedRootUri.AbsoluteUri == context.RedirectUri) {
						context.Validated();
					}
				}

				return Task.FromResult<object>(null);
			}

			public static AuthenticationProperties CreateProperties(string userName) {
				IDictionary<string, string> data = new Dictionary<string, string> { { "userName", userName } };
				return new AuthenticationProperties(data);
			}
		}
	}
}
