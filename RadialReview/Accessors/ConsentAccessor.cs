using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors {
	public class ConsentAccessor {
		public static string GetConsentMessage(UserOrganizationModel caller) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var found = s.QueryOver<ConsentModel>().Where(x => x.UserId == caller.User.Id).Take(1).SingleOrDefault();

					if (found == null) {
						found = new ConsentModel() {
							UserId = caller.User.Id,
						};
						s.Save(found);
					}
					
					var message = s.GetSettingOrDefault(Variable.Names.CONSENT_MESSAGE,
						"To create your experience, we record information that you or your organization has supplied. This information includes your name, profile picture, phone number, and corporate email address. " +
						"<br/>We use this information for your login and to send you meeting summaries, to-dos, feature updates, best practices, and other information relavent to your use of Traction Tools (you can shut these off). " +
						"<br/>Traction Tools uses cookies to store your login. We need to collect your billing address. We log all IP addresses to protect the service from non-user actors. " +
						"<br/>It should go without saying: We will never give away or sell your data. Your data is yours. " +
						"<br/><br/>Please see our <a href='/privacy'>Privacy Policy</a> and <a href='/tos'>Terms of Service </a> for a complete list of your data privacy rights." +
						"<br/><br/>Our service can't work without this data. So in order to use Traction Tools you must agree to us storing it." );

					tx.Commit();
					s.Flush();
					return message;
				}
			}
		}

		public static void ApplyConsent(UserOrganizationModel caller,bool affirmative) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var found = s.QueryOver<ConsentModel>().Where(x => x.UserId == caller.User.Id).Take(1).SingleOrDefault();

					if (found == null) {
						found = new ConsentModel() { UserId = caller.User.Id };
						s.Save(found);
					}
					if (affirmative) {
						found.ConsentTime = DateTime.UtcNow;
						found.DenyTime = null;
					} else {
						found.DenyTime = DateTime.UtcNow;
					}
					s.Update(found);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static bool HasConsented(UserOrganizationModel caller) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var found = s.QueryOver<ConsentModel>().Where(x => x.UserId == caller.User.Id && x.ConsentTime!=null).Take(1).SingleOrDefault();
					return found != null;
				}
			}
		}
	}
}