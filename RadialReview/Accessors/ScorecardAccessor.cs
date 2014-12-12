using System;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities;

namespace RadialReview.Accessors
{
	public class ScorecardAccessor
	{

		public static List<ScoreModel> GetScores(UserOrganizationModel caller, long organizationId, DateTime start, DateTime end,bool loadUsers)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					PermissionsUtility.Create(s, caller).ViewOrganizationScorecard(organizationId);
					var scores= s.QueryOver<ScoreModel>();
					if (loadUsers)
						scores = scores.Fetch(x=>x.AccountableUser).Eager;
					return scores.Where(x => x.OrganizationId == organizationId && x.DateDue >= start && x.DateDue <= end).List().ToList();
				}
			}
		}

		public static List<MeasurableModel> GetOrganizationMeasurables(UserOrganizationModel caller, long organizationId, bool loadUsers)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ViewOrganizationScorecard(organizationId);
					var measurables = s.QueryOver<MeasurableModel>();
					if (loadUsers)
						measurables = measurables.Fetch(x => x.AccountableUser).Eager;
					return measurables.Where(x => x.OrganizationId == organizationId && x.DeleteTime == null).List().ToList();
				}
			}
		}

		public static List<MeasurableModel> GetUserMeasurables(UserOrganizationModel caller, long userId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ViewUserOrganization(userId,false);
					return s.QueryOver<MeasurableModel>().Where(x => x.AccountableUserId== userId && x.DeleteTime == null).List().ToList();
				}
			}
		}

		/*public static List<ScoreModel> GetUnfinishedScores(UserOrganizationModel caller, long organizationId, DateTime start, DateTime end)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ViewOrganizationScorecard(organizationId);
					return scores;
				}
			}
		}*/



		public static void EditMeasurables(UserOrganizationModel caller, long userId, List<MeasurableModel> measurables)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					if (measurables.Any(x => x.AccountableUserId != userId))
						throw new PermissionsException("Measurable UserId does not match UserId");

					PermissionsUtility.Create(s, caller).ManagesUserOrganization(userId, false);
					var user = s.Get<UserOrganizationModel>(userId);
					var orgId = user.Organization.Id;

					foreach (var r in measurables){
						r.OrganizationId = orgId;
						s.SaveOrUpdate(r);
					}

					user.NumMeasurables = measurables.Count(x => x.DeleteTime == null);
					s.SaveOrUpdate(user);

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static List<ScoreModel> GetUserScores(UserOrganizationModel caller, long userId, DateTime sd, DateTime ed)
		{
			using (var s = HibernateSession.GetCurrentSession()){
				using (var tx = s.BeginTransaction()){
					PermissionsUtility.Create(s, caller).ViewUserOrganization(userId, false);
					return s.QueryOver<ScoreModel>().Where(x => x.AccountableUserId == userId && x.DeleteTime == null && x.DateDue>=sd && x.DateDue<=ed).List().ToList();
				}
			}
		}

		public static void EditUserScores(UserOrganizationModel caller, List<ScoreModel> scores)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					var uid = scores.EnsureAllSame(x => x.AccountableUserId);
					PermissionsUtility.Create(s, caller).EditUserScorecard(uid);
					foreach (var x in scores){
						s.Update(x);
					}
					tx.Commit();
					s.Flush();
				}
			}
		}
	}
}