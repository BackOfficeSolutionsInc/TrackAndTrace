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

namespace RadialReview
{
    public partial class Startup
    {
		public static OAuthAuthorizationServerOptions OAuthOptions { get; private set; }


        // For more information on configuring authentication, please visit http://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {
            // Enable the application to use a cookie to store information for the signed in user
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login")
            });
            // Use a cookie to temporarily store information about a user logging in with a third party login provider
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);


	        var PublicClientId = "self";
			OAuthOptions = new OAuthAuthorizationServerOptions
			{
				TokenEndpointPath = new PathString("/Token"),
				Provider = new ApplicationOAuthProvider(PublicClientId),
				AuthorizeEndpointPath = new PathString("/api/Account/ExternalLogin"),
				AccessTokenExpireTimeSpan = TimeSpan.FromDays(14),
				AllowInsecureHttp = true
			};

			// Enable the application to use bearer tokens to authenticate users
			app.UseOAuthBearerTokens(OAuthOptions);


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
        }




		public class ApplicationOAuthProvider : OAuthAuthorizationServerProvider
		{
			private readonly string _publicClientId;

			public static NHibernateUserManager UserManager = new NHibernateUserManager(new NHibernateUserStore());
			public ApplicationOAuthProvider(string publicClientId)
			{
				if (publicClientId == null)
				{
					throw new ArgumentNullException("publicClientId");
				}

				_publicClientId = publicClientId;
			}


			public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
			{

				var user = await UserManager.FindAsync(context.UserName, context.Password);

				if (user == null)
				{
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

			public override Task TokenEndpoint(OAuthTokenEndpointContext context)
			{
				foreach (KeyValuePair<string, string> property in context.Properties.Dictionary)
				{
					context.AdditionalResponseParameters.Add(property.Key, property.Value);
				}

				return Task.FromResult<object>(null);
			}

			public override Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
			{
				// Resource owner password credentials does not provide a client ID.
				if (context.ClientId == null)
				{
					context.Validated();
				}

				return Task.FromResult<object>(null);
			}

			public override Task ValidateClientRedirectUri(OAuthValidateClientRedirectUriContext context)
			{
				if (context.ClientId == _publicClientId)
				{
					Uri expectedRootUri = new Uri(context.Request.Uri, "/");
					if (expectedRootUri.AbsoluteUri == context.RedirectUri)
					{
						context.Validated();
					}
				}

				return Task.FromResult<object>(null);
			}

			public static AuthenticationProperties CreateProperties(string userName)
			{
				IDictionary<string, string> data = new Dictionary<string, string>{{ "userName", userName }};
				return new AuthenticationProperties(data);
			}
		}
    }
}