using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models;
using System.Threading.Tasks;
using RadialReview.Models.L10;
using RadialReview.Utilities.DataTypes;
using RadialReview.Accessors;
using RadialReview.Models.Dashboard;

namespace RadialReview.Hooks.CrossCutting {
	public class SwapScorecardOnRegister : ICreateUserOrganizationHook {
		public bool CanRunRemotely() {
			return false;
		}

		public HookPriority GetHookPriority() {
			return HookPriority.Database;
		}

		public async Task CreateUserOrganization(ISession s, UserOrganizationModel user) {
			//Noop
		}

		public async Task OnUserOrganizationAttach(ISession s, UserOrganizationModel user) {

			var best = new DiscreteDistribution<L10Recurrence>(0, 5);
			var meetings = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.User.Id == user.Id && x.DeleteTime == null).List().ToList();

			if (meetings.Any()) {
				var scorecardPages = s.QueryOver<L10Recurrence.L10Recurrence_Page>()
					.Where(x => x.DeleteTime == null && x.PageType == L10Recurrence.L10PageType.Scorecard)
					.WhereRestrictionOn(x => x.L10RecurrenceId).IsIn(meetings.Select(x => x.L10Recurrence.Id).ToList())
					.List().ToList();

				foreach (var m in meetings) {
					var teamScore = 0;
					switch (m.L10Recurrence.TeamType) {
						case L10TeamType.LeadershipTeam:
							teamScore = 3;
							break;
						case L10TeamType.DepartmentalTeam:
							teamScore = 2;
							break;
						case L10TeamType.Other:
							teamScore = 1;
							break;
						case L10TeamType.SamePageMeeting:
							teamScore = 0;
							break;
						default:
							break;
					}

					var score = teamScore;

					if (score > 0 && scorecardPages.Any(x => x.L10RecurrenceId == m.L10Recurrence.Id))
						best.Add(m.L10Recurrence, teamScore);
				}

			}

			var bestMeeting = best.ResolveOne();
			if (bestMeeting != null) {				
				var dashId = DashboardAccessor.GetPrimaryDashboardForUser(s, user, user.Id).NotNull(x => x.Id);				
				if (dashId == 0) {
					dashId = DashboardAccessor.CreateDashboard(s,user, null, false, true).Id;
				}

				var tiles = DashboardAccessor.GetTiles(s, dashId);

				var scoreTile = tiles.Where(x => x.Type == TileType.Scorecard && (x.DataUrl ?? "").Contains("UserScorecard")).FirstOrDefault();

				if (scoreTile != null) {
					scoreTile.Type = TileType.L10Scorecard;
					scoreTile.DataUrl = "/TileData/L10Scorecard/"+bestMeeting.Id;
					scoreTile.KeyId = ""+bestMeeting.Id;
					//scoreTile.Title = bestMeeting.Name + " Scorecard";
					s.Update(scoreTile);
				}
			}

		}

		public async Task OnUserRegister(ISession s, UserModel user) {
			//Noop
		}
	}
}