using System.Collections;
using Amazon.IdentityManagement.Model;
using FluentNHibernate.Utils;
using NHibernate;
using NHibernate.Mapping;
using NHibernate.Util;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Permissions;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Utilities.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors
{
	public class TeamAccessor : BaseAccessor
	{
		/*
		private OrganizationTeamModel Populate(ISession session, OrganizationTeamModel team)
		{
			switch (team.Type)
			{
				case TeamType.Standard:{
						return team;
					}
				case TeamType.AllMembers:{
						team.Name = team.Organization.GetName(); return team;
					}
				case TeamType.Managers:{
						team.Name = "Managers at "+team.Organization.GetName(); return team;
					}
				default: throw new NotImplementedException("Team Type Unknown");
			}
		}*/

		public OrganizationTeamModel GetSubordinateTeam(UserOrganizationModel caller, long userOrganizationId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perm = PermissionsUtility.Create(s, caller);
					return GetSubordinateTeam(s, perm, userOrganizationId);
				}
			}
		}
		public OrganizationTeamModel GetSubordinateTeam(ISession s, PermissionsUtility permissions, long userOrganizationId)
		{
			permissions.ViewUserOrganization(userOrganizationId, false);
			var team = s.QueryOver<OrganizationTeamModel>().Where(x => x.DeleteTime == null && x.Type == TeamType.Subordinates && x.ManagedBy == userOrganizationId).SingleOrDefault();
			if (team == null)
				throw new PermissionsException("No subordinate team exists.");
			return team;
		}



		public List<OrganizationTeamModel> GetTeamsDirectlyManaged(UserOrganizationModel caller1, long userOrganizationId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller1).OwnedBelowOrEqual(x => x.Id == userOrganizationId);
					var asUsers = s.QueryOver<PermissionOverride>()
						.Where(x => x.DeleteTime == null && x.ForUser.Id == userOrganizationId && x.Permissions == PermissionType.IssueReview)
						.Select(x => x.AsUser)
						.List<UserOrganizationModel>().ToList();
					asUsers.Add(s.Get<UserOrganizationModel>(userOrganizationId));
					var managingTeams = new List<OrganizationTeamModel>();
					foreach (var caller in asUsers){
						try{
							PermissionsUtility.Create(s, caller).OwnedBelowOrEqual(x => x.Id == userOrganizationId);
							var directlyManaging = s.QueryOver<OrganizationTeamModel>()
								.Where(x => x.ManagedBy == userOrganizationId)
								.List().ToList();
							var user = s.Get<UserOrganizationModel>(userOrganizationId);
							if (caller.ManagingOrganization){
								var orgId = caller.Organization.Id;
								directlyManaging.AddRange(s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == orgId && x.Type != TeamType.Standard).List().ToList());
							}
							managingTeams.AddRange(directlyManaging);
						}
						catch (Exception){
							
						}
					}
					managingTeams=managingTeams.Distinct(x => x.Id).ToList();
					return managingTeams.Where(x=>x.DeleteTime==null).ToList();
				}
			}
		}

		public List<OrganizationTeamModel> GetOrganizationTeams(UserOrganizationModel caller, long organizationId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var permissions = PermissionsUtility.Create(s, caller);
					return GetOrganizationTeams(s, permissions, organizationId);
				}
			}
		}

		public static List<OrganizationTeamModel> GetOrganizationTeams(ISession s, PermissionsUtility permissions, long organizationId)
		{
			permissions.ViewOrganization(organizationId);
			var teams = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == organizationId).List().ToListAlive();
			//teams.ForEach(x => Populate(s, x));
			return teams;
		}

		public OrganizationTeamModel GetTeam(UserOrganizationModel caller, long teamId)
		{
			if (teamId == 0)
				return new OrganizationTeamModel() { };
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, caller);
					return GetTeam(s, perms, caller, teamId);
				}
			}
		}

		public static OrganizationTeamModel GetTeam(ISession s, PermissionsUtility permissions, UserOrganizationModel caller, long teamId)
		{
			permissions.ViewTeam(teamId);
			var team = s.Get<OrganizationTeamModel>(teamId);
			//team.Members = s.QueryOver<TeamMemberModel>().Where(x => x.TeamId == teamId).List().ToList();
			return team;
		}

		public List<TeamDurationModel> GetTeamMembersAtOrganization(UserOrganizationModel caller, long orgId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perm = PermissionsUtility.Create(s, caller);
					var allTeams = GetOrganizationTeams(s, perm, orgId);


					//var queryAllTeams = "SELECT r.id,t.Type FROM OrganizationTeamModel t Inner Join ResponsibilityGroupModel r on t.ResponsibilityGroupModel_id = r.Id where Organization_id = (:orgId)";
					//var allTeams = s.CreateSQLQuery(queryAllTeams).List<object[]>();

					//var teams = allTeams.Select(x => new{id = (long) x[0], type = (string) x[1]});
					var acceptedTeams = allTeams.Where(x => false).ToList();

					foreach (var team in allTeams)
					{
						try
						{
							perm.ViewTeam(team.Id);
							acceptedTeams.Add(team);
						}
						catch (PermissionsException)
						{
						}
					}

					var members = new List<TeamDurationModel>();

					foreach (var teams in acceptedTeams.GroupBy(x => x.Type))
					{
						TeamType type = teams.Key;
						switch (type)
						{
							case TeamType.Standard:
								{
									var teamIds = teams.Select(x => x.Id).ToList();
									var teamMembers = s.QueryOver<TeamDurationModel>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.TeamId).IsIn(teamIds).List();
									members.AddRange(teamMembers);
									break;
								}
							case TeamType.AllMembers:
								{
									var users = s.QueryOver<UserOrganizationModel>().Where(x => x.DeleteTime == null && x.Organization.Id == orgId).List();
									foreach (var t in teams)
									{
										var additional = users.Select(x => new TeamDurationModel()
										{
											Id = -2,
											Start = x.AttachTime,
											Team = t,
											User = x,
											DeleteTime = x.DeleteTime ?? x.DetachTime,
											UserId = x.Id,
											TeamId = t.Id
										}).ToList();
										members.AddRange(additional);
									}
									break;

								}
							case TeamType.Managers:
								{
									var managers = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == orgId && (x.ManagerAtOrganization || x.ManagingOrganization) && x.DeleteTime == null).List();
									foreach (var tt in teams){
										var t = tt;
										var additional = managers.Select(x => new TeamDurationModel()
										{
											Id = -2,
											Start = x.AttachTime,
											Team = t,
											User = x,
											DeleteTime = x.DeleteTime ?? x.DetachTime,
											UserId = x.Id,
											TeamId = t.Id
										});
										members.AddRange(additional);
									}
									break;
								}
							case TeamType.Subordinates:
								{
									var managerIds = teams.Select(x => x.ManagedBy).ToList();
									var managerDurations = s.QueryOver<ManagerDuration>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.ManagerId).IsIn(managerIds).List();
									var lookup = teams.ToDictionary(x => x.ManagedBy, x => x);

									var additional = managerDurations.Select(x => new TeamDurationModel()
									{
										Id = -2,
										Start = x.Start,
										TeamId = lookup[x.ManagerId].Id,
										Team = lookup[x.ManagerId],
										DeleteTime = x.DeleteTime,
										UserId = x.Subordinate.Id,
										User = x.Subordinate,
									});
									members.AddRange(additional);

									var additionalManagers = managerDurations.Distinct(x=>x.ManagerId).Select(x => new TeamDurationModel()
									{
										Id = -2,
										Start = x.Start,
										TeamId = lookup[x.ManagerId].Id,
										Team = lookup[x.ManagerId],
										UserId = x.Manager.Id,
										User = x.Manager,
										DeleteTime = x.DeleteTime,
									});
									members.AddRange(additionalManagers);


									break;

									////var subordinates = caller.Hydrate(s).ManagingUsers(true).Execute().AllSubordinates;
									////permissions.OwnedBelowOrEqual(x => x.Id == team.ManagedBy);
									//var callerUnderlying = s.Get<UserOrganizationModel>(teams.ManagedBy);
									//var subs = UserAccessor.GetDirectSubordinates(s, permissions, teams.ManagedBy);
									////var subs = SubordinateUtility.GetSubordinates(callerUnderlying, false);
									//var subordinates = subs.Union(callerUnderlying.AsList(), new EqualityComparer<UserOrganizationModel>((x, y) => x.Id == y.Id, x => x.Id.GetHashCode()));
									//return subordinates.Select(x => new TeamDurationModel()
									//{
									//	Id = -2,
									//	Start = x.AttachTime,
									//	Team = teams,
									//	User = x,
									//	DeleteTime = x.DeleteTime ?? x.DetachTime,
									//	UserId = x.Id,
									//	TeamId = teams.Id
									//}).ToList();
								}
							default: throw new NotImplementedException("Team Type unknown2");
						}



					}

					return members;


					/*
					return teams.SelectMany(x =>
					{
						try
						{
							return GetTeamMembers(s.ToQueryProvider(true), perm, x.Id);
						}
						catch (PermissionsException)
						{
							return new List<TeamDurationModel>();
						}
					}).ToList();*/
				}
			}
		}

		public List<TeamDurationModel> GetSubordinateTeamMembers(UserOrganizationModel caller, long userOrganizationId, bool includeManager)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perm = PermissionsUtility.Create(s, caller);
					return GetSubordinateTeamMembers(s, perm, userOrganizationId, includeManager);
				}
			}
		}
		public List<TeamDurationModel> GetSubordinateTeamMembers(ISession s, PermissionsUtility permissions, long userOrganizationId, bool includeManager)
		{
			var team = GetSubordinateTeam(s, permissions, userOrganizationId);
			return GetTeamMembers(s.ToQueryProvider(true), permissions, team.Id, includeManager);
		}

		public List<TeamDurationModel> GetTeamMembers(UserOrganizationModel caller, long teamId, bool includeManager)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, caller);
					return GetTeamMembers(s.ToQueryProvider(true), perms, teamId, includeManager);
				}
			}
		}

		public static List<TeamDurationModel> GetTeamMembers(AbstractQuery s, PermissionsUtility permissions, long teamId, bool includeTeamManagers)
		{
			var members = GetTeamMembers(s, permissions, teamId);
			if (includeTeamManagers)
			{
				try
				{
					var team = s.Get<OrganizationTeamModel>(teamId);
					var managerId = team.ManagedBy;
					if (members.All(x => x.UserId != managerId))
					{
						var manager = s.Get<UserOrganizationModel>(managerId);
						members.Add(new TeamDurationModel(manager, team, -1)
						{
							Id = -2,
							User = manager,
							Team = team,
						});
					}
				}
				catch (Exception e)
				{
					log.Error("Error adding manager", e);
				}
			}
			return members;
		}


		/// <summary>
		/// Requires:
		///     OrganizationTeamModel
		///     TeamDurationModel
		///     UserOrganizationModel
		/// </summary>
		/// <param name="s"></param>
		/// <param name="permissions"></param>
		/// <param name="caller"></param>
		/// <param name="teamId"></param>
		/// <returns></returns>
		private static List<TeamDurationModel> GetTeamMembers(AbstractQuery s, PermissionsUtility permissions, long teamId)
		{
			permissions.ViewTeam(teamId);
			OrganizationTeamModel team;
			TeamType type;
			team = s.Get<OrganizationTeamModel>(teamId);
			type = team.Type;


			switch (type)
			{
				case TeamType.Standard:
					{
						var teamMembers = s.Where<TeamDurationModel>(x => x.Team.Id == teamId);
						return teamMembers;
					}
				case TeamType.AllMembers:
					{
						var users = s.Where<UserOrganizationModel>(x => x.Organization.Id == team.Organization.Id && x.DeleteTime == null && !x.IsClient);
						return users.Select(x => new TeamDurationModel()
						{
							Id = -2,
							Start = x.AttachTime,
							Team = team,
							User = x,
							DeleteTime = x.DeleteTime ?? x.DetachTime,
							UserId = x.Id,
							TeamId = team.Id
						}).ToList();
					}
				case TeamType.Managers:
					{
						var managers = s.Where<UserOrganizationModel>(x => x.Organization.Id == team.Organization.Id && (x.ManagerAtOrganization || x.ManagingOrganization) && x.DeleteTime == null && !x.IsClient);
						return managers.Select(x => new TeamDurationModel()
						{
							Id = -2,
							Start = x.AttachTime,
							Team = team,
							User = x,
							DeleteTime = x.DeleteTime ?? x.DetachTime,
							UserId = x.Id,
							TeamId = team.Id
						}).ToList();
					}
				case TeamType.Subordinates:
					{
						//var subordinates = caller.Hydrate(s).ManagingUsers(true).Execute().AllSubordinates;
						//permissions.OwnedBelowOrEqual(x => x.Id == team.ManagedBy);
						var callerUnderlying = s.Get<UserOrganizationModel>(team.ManagedBy);
						var subs = UserAccessor.GetDirectSubordinates(s, permissions, team.ManagedBy);
						//var subs = SubordinateUtility.GetSubordinates(callerUnderlying, false);
						var subordinates = subs.Union(callerUnderlying.AsList(), new EqualityComparer<UserOrganizationModel>((x, y) => x.Id == y.Id, x => x.Id.GetHashCode()));
						return subordinates.Select(x => new TeamDurationModel()
						{
							Id = -2,
							Start = x.AttachTime,
							Team = team,
							User = x,
							DeleteTime = x.DeleteTime ?? x.DetachTime,
							UserId = x.Id,
							TeamId = team.Id
						}).ToList();
					}
				default: throw new NotImplementedException("Team Type unknown");
			}
		}

		public List<TeamDurationModel> GetUsersTeams(UserOrganizationModel caller, long forUserId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, caller);
					return GetUsersTeams(s.ToQueryProvider(true), perms, caller, forUserId);
				}
			}
		}

		public List<TeamDurationModel> GetAllTeammembersAssociatedWithUser(UserOrganizationModel caller, long forUserId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var userTeams = s.QueryOver<TeamDurationModel>().Where(x => x.UserId == forUserId && x.DeleteTime != null).List().ToList();

					var members = new List<TeamDurationModel>();

					foreach (var team in userTeams)
					{
						var teamMembers = s.QueryOver<TeamDurationModel>().Where(x => x.TeamId == team.TeamId && x.DeleteTime != null).List().ToList();
						members.AddRange(teamMembers);
					}
					return members;
				}
			}
		}

		/*
		public static List<TeamDurationModel> GetUsersTeams(ISession s, PermissionsUtility permissions, UserOrganizationModel caller, UserOrganizationModel forUserId, List<OrganizationTeamModel> allOrganizationTeamModel, List<TeamDurationModel> allTeamDurationModels)
		{
			var forUser = s.Get<UserOrganizationModel>(forUserId);
			permissions.ViewOrganization(forUser.Organization.Id);
			var teams = forUser.Teams.ToList();
			if (forUser.IsManager())
			{
				var managerTeam = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == forUser.Organization.Id && x.Type == TeamType.Managers).SingleOrDefault();
				//Populate(s,managerTeam);
				teams.Add(new TeamDurationModel() { Start = forUser.AttachTime, Id = -2, Team = managerTeam, User = forUser });
			}
			var allMembersTeam = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == forUser.Organization.Id && x.Type == TeamType.AllMembers).SingleOrDefault();
			//Populate(s,allMembersTeam);
			teams.Add(new TeamDurationModel() { Start = forUser.AttachTime, Id = -2, Team = allMembersTeam, User = forUser });
			//teams.ForEach(x => Populate(s, x.Team));
			return teams;
		}*/

		public static List<TeamDurationModel> GetUsersTeams(AbstractQuery s, PermissionsUtility permissions, UserOrganizationModel caller, long forUserId)
		{
			var forUser = s.Get<UserOrganizationModel>(forUserId);
			permissions.ViewOrganization(forUser.Organization.Id);
			var teams = s.Where<TeamDurationModel>(x => x.DeleteTime == null && x.UserId == forUserId);

			//var teams = forUser.Teams.ToList();
			if (forUser.IsManager())
			{
				var managerTeam = s.Where<OrganizationTeamModel>(x => x.Organization.Id == forUser.Organization.Id && x.Type == TeamType.Managers).SingleOrDefault();
				//Populate(s,managerTeam);
				if (managerTeam!=null)
                    teams.Add(new TeamDurationModel() { Start = forUser.AttachTime, Id = -2, Team = managerTeam, User = forUser });
			}
			var allMembersTeam = s.Where<OrganizationTeamModel>(x => x.Organization.Id == forUser.Organization.Id && x.Type == TeamType.AllMembers).SingleOrDefault();
			//Populate(s,allMembersTeam);
            if (allMembersTeam!=null)
			    teams.Add(new TeamDurationModel() { Start = forUser.AttachTime, Id = -2, Team = allMembersTeam, User = forUser });
			//teams.ForEach(x => Populate(s, x.Team));
			return teams;
		}

		public OrganizationTeamModel EditTeam(UserOrganizationModel caller, long teamId, String name = null,
																							bool? interReview = null,
																							bool? onlyManagerCanEdit = null,
																							long? managerId = null)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).EditTeam(teamId);
					caller = s.Get<UserOrganizationModel>(caller.Id);

					var team = s.Get<OrganizationTeamModel>(teamId);

					if (teamId == 0)
					{
						if (name == null || onlyManagerCanEdit == null || managerId == null || interReview == null)
							throw new PermissionsException();

						team = new OrganizationTeamModel()
						{
							CreatedBy = caller.Id,
							Organization = caller.Organization,
							OnlyManagersEdit = onlyManagerCanEdit.Value,
							ManagedBy = managerId.Value,
						};

						s.SaveOrUpdate(team);
					}


					if (name != null && team.Type == TeamType.Standard && team.Name!=name)
					{
						team.Name = name;

						var all =s.QueryOver<TeamDurationModel>().Where(x => x.Team.Id == team.Id && x.DeleteTime == null).List().ToList();
						foreach(var a in all)
							a.User.UpdateCache(s);

					}

					if (onlyManagerCanEdit != null && onlyManagerCanEdit.Value != team.OnlyManagersEdit)
					{
						if (!caller.IsManager())
							throw new PermissionsException();
						team.OnlyManagersEdit = onlyManagerCanEdit.Value;
					}


					if (interReview != null)
					{
						team.InterReview = interReview.Value;
					}

					if (managerId != null){
						team.ManagedBy = managerId.Value;
					}

					s.Update(team);
					tx.Commit();
					s.Flush();
					return team;
				}
			}
		}

		public Boolean AddMember(UserOrganizationModel caller, long teamId, long userOrgId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).EditTeam(teamId).EditUserDetails(userOrgId);//ManagesUserOrganization(userOrgId,false);
					var team = s.Get<OrganizationTeamModel>(teamId);

					if (team.Type != TeamType.Standard)
						throw new PermissionsException("You cannot add members to an auto-generated team.");


					var uOrg = s.Get<UserOrganizationModel>(userOrgId);

					var existing = s.QueryOver<TeamDurationModel>().Where(x => x.User.Id == userOrgId && x.Team.Id == teamId && x.DeleteTime == null).SingleOrDefault();
					if (existing != null)
						throw new PermissionsException("The user is already a member of this team.");

					//team.Members.Add(new TeamMemberModel() { UserOrganization = uOrg });

					var teamDuration = new TeamDurationModel(uOrg, team, caller.Id);

					s.Save(teamDuration);
					if (uOrg!=null)
						uOrg.UpdateCache(s);

					tx.Commit();
					s.Flush();
					return true;
				}
			}
		}

		public Boolean RemoveMember(UserOrganizationModel caller, long teamDurationId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var teamDuration = s.Get<TeamDurationModel>(teamDurationId);
					if (teamDuration.Team.Type != TeamType.Standard)
						throw new PermissionsException("You cannot remove members from an auto-generated team.");

					if (teamDuration.DeleteTime != null)
						throw new PermissionsException();

					PermissionsUtility.Create(s, caller).EditTeam(teamDuration.Team.Id);
					//var team = s.Get<OrganizationTeamModel>(teamId);
					//team.Members.FirstOrDefault(x => x.UserOrganization.Id == userOrgId).DeleteTime = DateTime.UtcNow;

					teamDuration.DeleteTime = DateTime.UtcNow;
					teamDuration.DeletedBy = caller.Id;

					s.Update(teamDuration);

					teamDuration.User.UpdateCache(s);

					tx.Commit();
					s.Flush();
					return true;
				}
			}
		}
	}
}