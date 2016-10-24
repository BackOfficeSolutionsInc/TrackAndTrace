﻿using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Askables;

namespace RadialReview.Hooks {
	public class UpdateUserModel_TeamNames : IUpdateUserModelHook {

		public void UpdateUserModel(ISession s, UserModel user) {
			var uos = s.QueryOver<UserOrganizationModel>().Where(x => x.User.Id == user.Id && x.ManagerAtOrganization).List().ToList();
			var uoIds = uos.Select(y => y.Id).Distinct().ToArray();
			if (uoIds.Any()) {
				var teams = s.QueryOver<OrganizationTeamModel>().Where(x => x.Type == Models.Enums.TeamType.Subordinates)
					.WhereRestrictionOn(x => x.ManagedBy).IsIn(uoIds)
					.List().ToList();

				foreach (var t in teams) {
					var name = uos.FirstOrDefault(x => x.Id == t.ManagedBy).NotNull(x => x.GetNameAndTitle().Possessive() + " Direct Reports");
					t.Name = name ?? t.Name;
					s.Update(t);
				}
			}

		}
	}
}