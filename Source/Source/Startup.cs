using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google;
using Owin;
using System;
using System.Configuration;
using System.Threading.Tasks;

[assembly: OwinStartup(typeof(Source.Startup))]

namespace Source
{
    public class Startup
    {
        public const string ApplicationCookieAuthenticationType = "ApplicationCookie";
        public const string ExternalCookieAuthenticationType = "ExternalCookie";

        public void Configuration(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(
                ApplicationCookieAuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = ApplicationCookieAuthenticationType,
                LoginPath = new PathString("/Authentication/Login"),
                CookieName = "NewGen.Application",
                ExpireTimeSpan = TimeSpan.FromMinutes(60),
                SlidingExpiration = true,
                AuthenticationMode = AuthenticationMode.Active
            });

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = ExternalCookieAuthenticationType,
                CookieName = "NewGen.External",
                AuthenticationMode = AuthenticationMode.Passive,
                ExpireTimeSpan = TimeSpan.FromMinutes(10)
            });

            var googleClientId =
                ConfigurationManager.AppSettings["GoogleClientId"];

            var googleClientSecret =
                ConfigurationManager.AppSettings["GoogleClientSecret"];

            if (!string.IsNullOrWhiteSpace(googleClientId) &&
                !string.IsNullOrWhiteSpace(googleClientSecret))
            {
                app.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions
                {
                    AuthenticationType = "Google",
                    SignInAsAuthenticationType =
                        ExternalCookieAuthenticationType,

                    ClientId = googleClientId,
                    ClientSecret = googleClientSecret,

                    Scope =
                    {
                        "openid",
                        "profile",
                        "email"
                    },
                    Provider = new GoogleOAuth2AuthenticationProvider
                    {
                        OnAuthenticated = context =>
                        {
                            string picture = context.User?["picture"]?.ToString();

                            if (!string.IsNullOrWhiteSpace(picture))
                            {
                                context.Identity.AddClaim(
                                    new System.Security.Claims.Claim(
                                        "urn:google:picture",
                                        picture));
                            }

                            return Task.CompletedTask;
                        }
                    }
                });
            }
        }
    }
}