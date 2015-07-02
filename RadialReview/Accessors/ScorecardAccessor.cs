using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using FluentNHibernate.Utils;
using Microsoft.AspNet.SignalR;
using NHibernate;
using NHibernate.Linq;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.L10;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities;

namespace RadialReview.Accessors
{
	public class ScorecardAccessor
	{

		public static List<ScoreModel> GetScores(UserOrganizationModel caller, long organizationId, DateTime start, DateTime end, bool loadUsers)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ViewOrganizationScorecard(organizationId);
					var scores = s.QueryOver<ScoreModel>();
					if (loadUsers)
						scores = scores.Fetch(x => x.AccountableUser).Eager;
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

		public static List<MeasurableModel> GetPotentialMeetingMeasurables(UserOrganizationModel caller, long recurrenceId, bool loadUsers)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms =PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					var measurables = s.QueryOver<MeasurableModel>();
					if (loadUsers)
						measurables = measurables.Fetch(x => x.AccountableUser).Eager;

					var userIds = L10Accessor.GetL10Recurrence(s,perms,recurrenceId, true)._DefaultAttendees.Select(x => x.User.Id).ToList();

					return measurables.Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.AccountableUserId).IsIn(userIds).List().ToList();
				}
			}
		} 


		public static List<MeasurableModel> GetUserMeasurables(UserOrganizationModel caller, long userId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ViewUserOrganization(userId, false);
					var found = s.QueryOver<MeasurableModel>()
						.Where(x => x.AccountableUserId == userId && x.DeleteTime == null)
						.Fetch(x=>x.AdminUser).Eager
						.List().ToList();
					foreach (var f in found){
						var a= f.AccountableUser;
					}
					return found;
				}
			}
		}

		public static List<ScoreModel> GetMeasurableScores(UserOrganizationModel caller, long measurableId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var measureable = s.Get<MeasurableModel>(measurableId);

					PermissionsUtility.Create(s, caller).ViewOrganizationScorecard(measureable.OrganizationId);
					return s.QueryOver<ScoreModel>().Where(x => x.MeasurableId == measurableId && x.DeleteTime == null).List().ToList();
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

					PermissionsUtility.Create(s, caller).EditQuestionForUser(userId);
					var user = s.Get<UserOrganizationModel>(userId);
					var orgId = user.Organization.Id;

					foreach (var r in measurables)
					{
						r.OrganizationId = orgId;
						s.SaveOrUpdate(r);
					}

					s.SaveOrUpdate(user);
					user.UpdateCache(s);

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static List<ScoreModel> GetUserScoresIncomplete(UserOrganizationModel caller, long userId,DateTime? now=null)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ViewUserOrganization(userId, false);
					var nowPlus = (now ?? DateTime.UtcNow).Add(TimeSpan.FromDays(1));
					var scorecards = s.QueryOver<ScoreModel>().Where(x => x.AccountableUserId == userId && x.DateDue < nowPlus && x.DateEntered == null && x.DeleteTime==null).List().ToList();

					scorecards = scorecards.Where(x => x.Measurable.DeleteTime == null).ToList();

					return scorecards;
				}
			}


		}

		public static List<ScoreModel> GetUserScores(UserOrganizationModel caller, long userId, DateTime sd, DateTime ed)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ViewUserOrganization(userId, false);
					return s.QueryOver<ScoreModel>().Where(x => x.AccountableUserId == userId && x.DeleteTime == null && x.DateDue >= sd && x.DateDue <= ed).List().ToList();
				}
			}
		}

		public static void EditUserScores(UserOrganizationModel caller, List<ScoreModel> scores)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){

					var oldScores = scores.ToList();
					
					scores =s.QueryOver<ScoreModel>().WhereRestrictionOn(x => x.Id).IsIn(scores.Select(x => x.Id).ToArray()).List().ToList();

					var now = DateTime.UtcNow;

					var uid = scores.EnsureAllSame(x => x.AccountableUserId);
					PermissionsUtility.Create(s, caller).EditUserScorecard(uid);
					foreach (var x in scores){
						x.Measured = oldScores.FirstOrDefault(y => y.Id == x.Id).NotNull(y => y.Measured);
						if (x.Measured == null){
							x.DateEntered = null;
							x.DeleteTime = now;
						}else{
							x.DeleteTime = null;
							x.DateEntered = now;
						}

						s.Update(x);
					}
					tx.Commit();
					s.Flush();
				}
			}
		}


		public static ScoreModel UpdateScoreInMeeting(ISession s,PermissionsUtility perms, long recurrenceId, long scoreId, DateTime week, long measurableId, decimal? value, string dom)
		{
			var now = DateTime.UtcNow;
			DateTime? nowQ = now;

			var meeting = L10Accessor._GetCurrentL10Meeting(s, perms, recurrenceId);
			var score = s.Get<ScoreModel>(scoreId);


			if (score != null && score.DeleteTime != null)
			{
				//Editable in this meeting?
				var ms = s.QueryOver<L10Meeting.L10Meeting_Measurable>()
					.Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id && x.Measurable.Id == score.MeasurableId)
					.SingleOrDefault<L10Meeting.L10Meeting_Measurable>();
				if (ms == null)
					throw new PermissionsException("You do not have permission to edit this score.");
				score.Measured = value;
				score.DateEntered = (value == null) ? null : nowQ;
				s.Update(score);
			}
			else
			{
				var meetingMeasurables = s.QueryOver<L10Meeting.L10Meeting_Measurable>()
					.Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id && x.Measurable.Id == measurableId)
					.SingleOrDefault<L10Meeting.L10Meeting_Measurable>();

				if (meetingMeasurables == null)
					throw new PermissionsException("You do not have permission to edit this score.");
				var m = meetingMeasurables.Measurable;

				//var SoW = m.Organization.Settings.WeekStart;

				var existingScores = s.QueryOver<ScoreModel>()
					.Where(x => x.DeleteTime == null && x.Measurable.Id == measurableId)
					.List().ToList();

				//adjust week..
				week = week.StartOfWeek(DayOfWeek.Sunday);

				//See if we can find it given week.
				score = existingScores.OrderBy(x=>x.Id).FirstOrDefault(x => (x.ForWeek == week));

				if (score != null)
				{
					//Found it with false id
					score.Measured = value;
					score.DateEntered = (value == null) ? null : nowQ;
					s.Update(score);
				}
				else
				{
					var ordered = existingScores.OrderBy(x => x.DateDue);
					var minDate = ordered.FirstOrDefault().NotNull(x => (DateTime?)x.ForWeek) ?? now;
					var maxDate = ordered.LastOrDefault().NotNull(x => (DateTime?)x.ForWeek) ?? now;

					minDate = minDate.StartOfWeek(DayOfWeek.Sunday);
					maxDate = maxDate.StartOfWeek(DayOfWeek.Sunday);


					DateTime start, end;

					if (week > maxDate)
					{
						//Create going up until sufficient
						var n = maxDate;
						ScoreModel curr = null;
						while (n < week)
						{
							var nextDue = n.StartOfWeek(DayOfWeek.Sunday).AddDays(7).AddDays((int)m.DueDate).Add(m.DueTime);
							curr = new ScoreModel()
							{
								AccountableUserId = m.AccountableUserId,
								DateDue = nextDue,
								MeasurableId = m.Id,
								OrganizationId = m.OrganizationId,
								ForWeek = nextDue.StartOfWeek(DayOfWeek.Sunday)
							};
							s.Save(curr);
							m.NextGeneration = nextDue;
							n = nextDue.StartOfWeek(DayOfWeek.Sunday);
						}
						curr.DateEntered = (value == null) ? null : nowQ;
						curr.Measured = value;
					}
					else if (week < minDate)
					{
						var n = week;
						var first = true;
						while (n < minDate)
						{
							var nextDue = n.StartOfWeek(DayOfWeek.Sunday).AddDays((int)m.DueDate).Add(m.DueTime);
							var curr = new ScoreModel()
							{
								AccountableUserId = m.AccountableUserId,
								DateDue = nextDue,
								MeasurableId = m.Id,
								OrganizationId = m.OrganizationId,
								ForWeek = nextDue.StartOfWeek(DayOfWeek.Sunday)
							};
							if (first)
							{
								curr.Measured = value;
								curr.DateEntered = (value == null) ? null : nowQ;
								first = false;
								s.Save(curr);
							}

							//m.NextGeneration = nextDue;
							n = nextDue.AddDays(7).StartOfWeek(DayOfWeek.Sunday);
						}
					}
					else
					{
						// cant create scores between these dates..
						var curr = new ScoreModel()
						{
							AccountableUserId = m.AccountableUserId,
							DateDue = week.StartOfWeek(DayOfWeek.Sunday).AddDays((int)m.DueDate).Add(m.DueTime),
							MeasurableId = m.Id,
							OrganizationId = m.OrganizationId,
							ForWeek = week.StartOfWeek(DayOfWeek.Sunday),
							Measured = value,
							DateEntered = (value == null) ? null : nowQ
						};
						s.Save(curr);
					}
					s.Update(m);
				}
			}
			var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
			hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(meeting)).updateTextContents(dom, value);

			Audit.L10Log(s, perms.GetCaller(), recurrenceId, "UpdateScoreInMeeting", score.NotNull(x => x.Measurable.NotNull(y => y.Title)) + " updated to " + value);
			return score;
		}
		
		public static ScoreModel UpdateScoreInMeeting(UserOrganizationModel caller, long recurrenceId, long scoreId, DateTime week, long measurableId, decimal? value,string dom)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){

					var perms = PermissionsUtility.Create(s, caller);
					var output = UpdateScoreInMeeting(s, perms, recurrenceId, scoreId, week, measurableId, value, dom);

					tx.Commit();
					s.Flush();
					return output;
				}
			}
		}


		public static ScoreModel GetScoreInMeeting(UserOrganizationModel caller, long scoreId,long recurrenceId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					var perms = PermissionsUtility.Create(s, caller);
					var meeting = L10Accessor._GetCurrentL10Meeting(s, perms, recurrenceId);
					var score = s.Get<ScoreModel>(scoreId);


					if (score != null && score.DeleteTime == null)
					{
						//Editable in this meeting?
						var ms = s.QueryOver<L10Meeting.L10Meeting_Measurable>()
							.Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id && x.Measurable.Id == score.MeasurableId)
							.SingleOrDefault<L10Meeting.L10Meeting_Measurable>();
						if (ms == null)
							throw new PermissionsException("You do not have permission to edit this score.");

						var a = score.Measurable.AccountableUser.GetName();
						var b = score.Measurable.AdminUser.GetName();
						var c = score.AccountableUser.GetName();

						return score;
					}
					return null;
				}
			}
		}
	}
}