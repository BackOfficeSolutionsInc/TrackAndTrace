using NHibernate;
using RadialReview.Models.Onboard;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors {
    public class OnboardingAccessor : BaseAccessor {

        public static OnboardingUser GetOrCreate(HttpRequestBase request, HttpResponseBase response,string page=null)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var cookie = request.Cookies["Onboarding"];
                    if (cookie != null && !string.IsNullOrWhiteSpace(cookie.Value)) {
                        var found = s.QueryOver<OnboardingUser>().Where(x => x.Guid == cookie.Value && x.DeleteTime == null).SingleOrDefault();
                        if (found != null && found.DeleteTime == null) {
                            if (page != null) {
                                found.CurrentPage = page;
                                s.Update(found);
                                tx.Commit();
                                s.Flush();
                            }
                            return found;
                        }
                    }

                    var f = Create(s, request, response);

                    tx.Commit();
                    s.Flush();
                    return f;

                }
            }

        }


        public static OnboardingUser Create(ISession s, HttpRequestBase request, HttpResponseBase response)
        {
            var u = new OnboardingUser() {
                Guid = Guid.NewGuid().ToString(),
                StartTime = DateTime.UtcNow,
                CurrentPage = "TheBasics",
                UserAgent = request.UserAgent,
                Languages = string.Join(",", (request.UserLanguages ?? new string[] { }))
            };

            s.Save(u);

            HttpCookie appCookie = new HttpCookie("Onboarding");
            appCookie.Value = u.Guid;
            appCookie.Expires = DateTime.Now.AddDays(100);
            response.Cookies.Add(appCookie);

            return u;
        }
    }
}