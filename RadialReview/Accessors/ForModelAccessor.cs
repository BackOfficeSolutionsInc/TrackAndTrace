using NHibernate;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors {
	public class ForModelAccessor {

        public static long GetOrganizationId(ISession s,IForModel forModel) {
            if (forModel.Is<UserOrganizationModel>()) {
                return s.Get<UserOrganizationModel>(forModel.ModelId).Organization.Id;
            }else if (forModel.Is<SurveyUserNode>()) {
                var sun = s.Get<SurveyUserNode>(forModel.ModelId);
                return sun.User.Organization.Id;

            }

        }

		public static string GetEmail_Unsafe(IForModel forModel) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return GetEmail_Unsafe(s, forModel);
				}
			}
		}

		public static TinyUser GetTinyUser_Unsafe(IForModel forModel) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return GetTinyUser_Unsafe(s, forModel);
				}
			}
		}


		public static string GetEmail_Unsafe(ISession s, IForModel forModel) {
			if (forModel.Is<UserOrganizationModel>()) {
				return s.Get<UserOrganizationModel>(forModel.ModelId).NotNull(x => x.GetEmail());
			} else if (forModel.Is<AccountabilityNode>()) {
				return s.Get<AccountabilityNode>(forModel.ModelId).NotNull(x => x.User.GetEmail());
			} else if (forModel.Is<SurveyUserNode>()) {
				return s.Get<SurveyUserNode>(forModel.ModelId).NotNull(x => s.Get<UserOrganizationModel>(x.UserOrganizationId)).GetEmail();
			} else {
				Console.WriteLine("Unhandled type:" + forModel.ModelType);
			}
			return null;
		}

		public static TinyUser GetTinyUser_Unsafe(ISession s, IForModel forModel) {
			if (forModel.Is<UserOrganizationModel>()) {
				return s.Get<UserOrganizationModel>(forModel.ModelId).NotNull(x => TinyUser.FromUserOrganization(x));
			} else if (forModel.Is<AccountabilityNode>()) {
				return s.Get<AccountabilityNode>(forModel.ModelId).NotNull(x => TinyUser.FromUserOrganization(s.Get<UserOrganizationModel>(x.User)));
			} else if (forModel.Is<SurveyUserNode>()) {
				return s.Get<SurveyUserNode>(forModel.ModelId).NotNull(x => TinyUser.FromUserOrganization(s.Get<UserOrganizationModel>(x.UserOrganizationId)));
			} else {
				Console.WriteLine("Unhandled type:" + forModel.ModelType);
			}
			return null;
		}
	}
}