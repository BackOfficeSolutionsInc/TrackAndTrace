﻿using System.Security;
using Amazon.ElasticMapReduce.Model;
using Amazon.ElasticTranscoder.Model;
using FluentNHibernate.Utils;
using Microsoft.AspNet.SignalR;
using NHibernate.Linq;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.CompanyValue;
using RadialReview.Models.Angular.VTO;
using RadialReview.Models.Askables;
using RadialReview.Models.Periods;
using RadialReview.Models.Permissions;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.Scorecard;
using RadialReview.Models.VTO;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Models.Enums;
using RadialReview.Utilities.DataTypes;
using RadialReview.Models.UserModels;
using RadialReview.Utilities.Query;
using NHibernate;
using WebGrease.Css.Extensions;
using RadialReview.Hooks;
using RadialReview.Utilities.Hooks;
using RadialReview.Models.Accountability;
using RadialReview.Utilities.RealTime;
using RadialReview.Models.Angular.Users;

namespace RadialReview.Accessors {

	public class OrganizationAccessor : BaseAccessor {


		public OrganizationModel CreateOrganization(UserModel user, string name, PaymentPlanType planType, DateTime now, out UserOrganizationModel newUser, bool enableL10, bool enableReview, bool startDeactivated = false, string positionName = null) {
			UserOrganizationModel userOrgModel;
			OrganizationModel organization;
			OrganizationTeamModel allMemberTeam;
			PermissionsUtility perms;
			AccountabilityChart acChart;

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					organization = new OrganizationModel() {
						CreationTime = now,
						Name = new LocalizedStringModel() { Standard = name },
						ManagersCanEdit = false,
					};


					if (startDeactivated)
						organization.DeleteTime = new DateTime(1, 1, 1);

					organization.Settings.EnableL10 = enableL10;
					organization.Settings.EnableReview = enableReview;
					s.Save(organization);
					var paymentPlan = PaymentAccessor.GeneratePlan(planType, now);
					PaymentAccessor.AttachPlan(s, organization, paymentPlan);
					organization.PaymentPlan = paymentPlan;
					organization.Organization = organization;
					s.Update(organization);
					//db.Organizations.Add(organization);
					//db.SaveChanges();
					//db.UserModels.Attach(user);
					user = s.Get<UserModel>(user.Id);




					userOrgModel = new UserOrganizationModel() {
						Organization = organization,
						User = user,
						ManagerAtOrganization = true,
						ManagingOrganization = true,
						EmailAtOrganization = user.Email,
						AttachTime = now,
						CreateTime = now
					};


					s.Save(user);
					s.SaveOrUpdate(userOrgModel);

					//userOrgModel.ManagingOrganizations.Add(organization);
					//userOrgModel.BelongingToOrganizations.Add(organization);
					//userOrgModel.ManagerAtOrganization.Add(organization);

					user.UserOrganization.Add(userOrgModel);
					user.UserOrganizationCount += 1;

					var newArray = new List<long>();
					if (user.UserOrganizationIds != null)
						newArray = user.UserOrganizationIds.ToList();
					newArray.Add(userOrgModel.Id);
					user.UserOrganizationIds = newArray.ToArray();
					user.CurrentRole = userOrgModel.Id;

					//organization.ManagedBy.Add(userOrgModel);
					organization.Members.Add(userOrgModel);

					s.Update(user);

					s.Save(organization);

					perms = PermissionsUtility.Create(s, userOrgModel);
					acChart = AccountabilityAccessor.CreateChart(s, perms, organization.Id, false);
					organization.AccountabilityChartId = acChart.Id;

					if (positionName != null) {
						var orgPos = new OrganizationPositionModel() {
							Organization = s.Load<OrganizationModel>(organization.Id),
							CreatedBy = userOrgModel.Id,
							CustomName = positionName,
						};
						s.Save(orgPos);
						var posDur = new PositionDurationModel() {
							UserId = userOrgModel.Id,
							Position = orgPos,
							PromotedBy = userOrgModel.Id,
							Start = DateTime.UtcNow
						};
						userOrgModel.Positions.Add(posDur);
						s.Update(userOrgModel);
					}


					//Add team for every member
					allMemberTeam = new OrganizationTeamModel() {
						CreatedBy = userOrgModel.Id,
						Name = organization.Name.Translate(),
						OnlyManagersEdit = true,
						Organization = organization,
						InterReview = false,
						Type = TeamType.AllMembers
					};
					s.Save(allMemberTeam);
					//Add team for every manager
					var managerTeam = new OrganizationTeamModel() {
						CreatedBy = userOrgModel.Id,
						Name = "Managers at " + organization.Name.Translate(),
						OnlyManagersEdit = true,
						Organization = organization,
						InterReview = false,
						Type = TeamType.Managers
					};
					s.Save(managerTeam);

					if (userOrgModel != null)
						userOrgModel.UpdateCache(s);




					tx.Commit();
					//db.UserOrganizationModels.Add(userOrgModel);
					//db.SaveChanges();

					//organization.ManagedBy.Add(userOrgModel);
					//db.SaveChanges();
				}
				using (var tx = s.BeginTransaction()) {

					var year = DateTime.UtcNow.Year;
					foreach (var q in Enumerable.Range(1, 4)) {
						s.Save(new PeriodModel() {
							Name = year + " Q" + q,
							StartTime = new DateTime(year, 1, 1).AddDays((q - 1) * 13 * 7).StartOfWeek(DayOfWeek.Sunday),
							EndTime = new DateTime(year, 1, 1).AddDays(q * 13 * 7).StartOfWeek(DayOfWeek.Sunday),
							OrganizationId = organization.Id,
						});
					}



					foreach (var defaultQ in new[]{
						"What is their greatest contribution to the team?",
						"What should they start or stop doing?"
					}) {
						var r = new ResponsibilityModel() {
							Category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.FEEDBACK),
							ForOrganizationId = organization.Id,
							ForResponsibilityGroup = allMemberTeam.Id,
							CreateTime = now,
							Weight = WeightType.Normal,
							Required = true,
							Responsibility = defaultQ
						};
						r.SetQuestionType(QuestionType.Feedback);
						s.Save(r);

						allMemberTeam.Responsibilities.Add(r);
					}
					s.Update(allMemberTeam);

					var permsAdmin = PermissionsUtility.Create(s, UserOrganizationModel.ADMIN);
					using (var rt = RealTimeUtility.Create()) {
						var node = AccountabilityAccessor.AppendNode(s, permsAdmin, rt, acChart.RootId, userId: userOrgModel.Id);
						//AccountabilityAccessor.UpdateAccountabilityNode(s, RealTimeUtility.Create(false), permsAdmin, node.Id, null, );
					}


					//s.Save(new DeepAccountability {
					//                   CreateTime = now,
					//                   Links = 1,
					//                   SubordinateId = userOrgModel.Id,
					//                   ManagerId = userOrgModel.Id,
					//                   OrganizationId = organization.Id,
					//               });
					newUser = userOrgModel;

					userOrgModel.UpdateCache(s);

					tx.Commit();
				}
				using (var tx = s.BeginTransaction()) {

					HooksRegistry.Each<ICreateUserOrganizationHook>(x => x.CreateUser(s, userOrgModel));


					tx.Commit();
					s.Flush();
					return organization;
				}
			}


		}

		public static IEnumerable<AngularUser> GetAngularUsers(UserOrganizationModel caller, long id) {
			using (var db = HibernateSession.GetCurrentSession()) {
				using (var tx = db.BeginTransaction()) {
					return GetAllUserOrganizations(db, PermissionsUtility.Create(db, caller), id).Select(x => AngularUser.CreateUser(x));
				}
			}
		}

		public UserOrganizationModel JoinOrganization(UserModel user, long managerId, long userOrgPlaceholder) {
			using (var db = HibernateSession.GetCurrentSession()) {
				using (var tx = db.BeginTransaction()) {
					var manager = db.Get<UserOrganizationModel>(managerId);
					var orgId = manager.Organization.Id;
					var organization = db.Get<OrganizationModel>(orgId);
					user = db.Get<UserModel>(user.Id);
					var userOrg = db.Get<UserOrganizationModel>(userOrgPlaceholder);

					userOrg.AttachTime = DateTime.UtcNow;
					userOrg.User = user;
					userOrg.Organization = organization;
					user.CurrentRole = userOrgPlaceholder;

					user.UserOrganization.Add(userOrg);
					user.UserOrganizationCount += 1;

					var newArray = user.UserOrganizationIds.NotNull(x => x.ToList()) ?? new List<long>();
					newArray.Add(userOrg.Id);
					user.UserOrganizationIds = newArray.ToArray();

					db.Delete(userOrg.TempUser);

					userOrg.TempUser = null;

					//manager.ManagingUsers.Add(userOrg);
					//organization.Members.Add(userOrg);

					db.SaveOrUpdate(user);

					userOrg.UpdateCache(db);

					tx.Commit();
					db.Flush();
					return userOrg;
				}
			}
		}

		public static List<OrganizationPositionModel> GetOrganizationPositions(ISession s, PermissionsUtility perms, long organizationId) {
			perms.ViewOrganization(organizationId);
			var positions = s.QueryOver<OrganizationPositionModel>().Where(x => x.Organization.Id == organizationId).List().ToList();
			return positions;
		}

		public List<OrganizationPositionModel> GetOrganizationPositions(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetOrganizationPositions(s, perms, organizationId);
				}
			}
		}

		public OrganizationPositionModel GetOrganizationPosition(UserOrganizationModel caller, long positionId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var position = s.Get<OrganizationPositionModel>(positionId);
					PermissionsUtility.Create(s, caller).ViewOrganization(position.Organization.Id);
					return position;
				}
			}
		}


		public List<ManagerDuration> GetOrganizationManagerLinks(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
					return s.QueryOver<ManagerDuration>().JoinQueryOver(x => x.Manager).Where(x => x.Organization.Id == organizationId).List().ToList();
				}
			}
		}

		public static List<TinyUser> GetMembers_Tiny(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetMembers_Tiny(s, perms, organizationId);
				}
			}
		}

		public static List<TinyUser> GetMembers_Tiny(ISession s, PermissionsUtility perms, long organizationId) {
			perms.ViewOrganization(organizationId);
			UserOrganizationModel uo = null;
			UserModel u = null;
			TempUserModel t = null;
			return s.QueryOver<UserOrganizationModel>(() => uo)
				.Left.JoinAlias(x => x.User, () => u)
				.Left.JoinAlias(x => x.TempUser, () => t)
				.Where(x => x.Organization.Id == organizationId && x.DeleteTime == null)
				.Select(x => u.FirstName, x => u.LastName, x => x.Id, x => t.FirstName, x => t.LastName, x => u.UserName, x => t.Email)
				.List<object[]>()
				.Select(x => {
					var fname = (string)x[0];
					var lname = (string)x[1];
					var email = (string)x[5];
					var uoId = (long)x[2];
					if (fname == null && lname == null) {
						fname = (string)x[3];
						lname = (string)x[4];
						email = (string)x[6];
					}
					return new TinyUser() {
						FirstName = fname,
						LastName = lname,
						Email = email,
						UserOrgId = uoId
					};// Tuple.Create(fname, lname, (long)x[2], email);
				}).ToList();

		}

		public List<long> GetAllOrganizationMemberIdsAcrossTime(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
					return s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == organizationId).Select(x => x.Id).List<long>().ToList();
				}
			}

		}

		public List<UserOrganizationModel> GetOrganizationMembers(UserOrganizationModel caller, long organizationId, bool teams, bool managers) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
					var users = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == organizationId && x.DeleteTime == null).List().ToList();

					if (managers) {
						var allManagers = s.QueryOver<ManagerDuration>().JoinQueryOver(x => x.Manager).Where(x => x.Organization.Id == organizationId && x.DeleteTime == null).List().ToList();
						foreach (var user in users)
							user.PopulateManagers(allManagers);
					}

					if (teams) {
						var allTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == organizationId).List().ToList();
						var allTeamDurations = s.QueryOver<TeamDurationModel>().JoinQueryOver(x => x.Team).Where(x => x.Organization.Id == organizationId).List().ToList();
						foreach (var user in users) {
							user.PopulateTeams(allTeams, allTeamDurations);
						}
					}
					return users;
				}
			}
		}

		public OrganizationPositionModel EditOrganizationPosition(UserOrganizationModel caller, long orgPositionId, long organizationId,
			/*long? positionId = null,*/ String customName = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).EditPositions(organizationId).ManagingPosition(orgPositionId);

					/*var existing = s.QueryOver<OrganizationPositionModel>()
                        .Where(x=>x.Organization.Id==organizationId && positionId==x.Position.Id)
                        .List().ToList().FirstOrDefault();
                    if (existing!=null)
                        throw new PermissionsException();*/


					OrganizationPositionModel orgPos;
					if (orgPositionId == 0) {
						var org = s.Get<OrganizationModel>(organizationId);
						if ( /*positionId == null ||*/ String.IsNullOrWhiteSpace(customName))
							throw new PermissionsException();

						orgPos = new OrganizationPositionModel() { Organization = org, CreatedBy = caller.Id };
						s.Save(orgPos);
					} else {
						orgPos = s.Get<OrganizationPositionModel>(orgPositionId);
					}

					/*
                    if (positionId != null)
                    {
                        var position = s.Get<PositionModel>(positionId);
                        orgPos.Position = position;
                    }
                    */

					if (customName != null && orgPos.CustomName != customName) {
						orgPos.CustomName = customName;

						var aa = s.QueryOver<PositionDurationModel>().Where(x => x.Position.Id == orgPos.Id && x.DeleteTime == null).Select(x => x.UserId).List<long>().ToList();
						var all = s.QueryOver<UserOrganizationModel>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(aa).List().ToList();
						foreach (var a in all)
							a.UpdateCache(s);
					}


					s.SaveOrUpdate(orgPos);
					tx.Commit();
					s.Flush();

					return orgPos;
				}
			}
		}
		public OrganizationTeamModel AddOrganizationTeam(UserOrganizationModel caller, long organizationId, string teamName, bool onlyManagersEdit, bool secret) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).EditTeam(0).ViewOrganization(organizationId);

					/*var existing = s.QueryOver<OrganizationPositionModel>()
                        .Where(x => x.Organization.Id == organizationId && positionId == x.Position.Id)
                        .List().ToList().FirstOrDefault();
                    if (existing!=null)
                        throw new PermissionsException();*/

					var org = s.Get<OrganizationModel>(organizationId);

					var orgTeam = new OrganizationTeamModel() {
						Organization = org,
						CreatedBy = caller.Id,
						Name = teamName,
						OnlyManagersEdit = onlyManagersEdit,
						Secret = secret,
					};

					s.Save(orgTeam);
					tx.Commit();
					s.Flush();

					return orgTeam;
				}
			}
		}

		public void Edit(UserOrganizationModel caller, long organizationId, string organizationName = null,
																			bool? managersHaveAdmin = null,
																			bool? strictHierarchy = null,
																			bool? managersCanEditPositions = null,
																			bool? sendEmailImmediately = null,
																			bool? managersCanRemoveUsers = null,
																			bool? managersCanEditSelf = null,
																			bool? employeesCanEditSelf = null,
																			bool? managersCanCreateSurvey = null,
																			bool? employeesCanCreateSurvey = null,
																			string rockName = null,
																			bool? onlySeeRockAndScorecardBelowYou = null,
																			string timeZoneId = null,
																			DayOfWeek? weekStart = null,
																			ScorecardPeriod? scorecardPeriod = null,
																			Month? startOfYearMonth = null,
																			DateOffset? startOfYearOffset = null,
																			string dateFormat = null,
																			NumberFormat? numberFormat = null
			) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).EditOrganization(organizationId).ManagingOrganization(caller.Organization.Id);
					var org = s.Get<OrganizationModel>(organizationId);
					if (managersHaveAdmin != null && managersHaveAdmin.Value != org.ManagersCanEdit) {
						if (caller.ManagingOrganization)
							org.ManagersCanEdit = managersHaveAdmin.Value;
						else
							throw new PermissionsException("You cannot change whether managers are admins at the organization.");
					}
					if (!String.IsNullOrWhiteSpace(organizationName) && org.Name.Standard != organizationName) {
						org.Name.UpdateDefault(organizationName);
						var managers = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == org.Id && x.Type == TeamType.Managers).List().FirstOrDefault();
						if (managers != null) {
							managers.Name = "Managers at " + organizationName;
							s.Update(managers);
						}
						var allTeam = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == org.Id && x.Type == TeamType.AllMembers).List().FirstOrDefault();
						if (allTeam != null) {
							allTeam.Name = organizationName;
							s.Update(allTeam);
						}
					}
					if (strictHierarchy != null)
						org.StrictHierarchy = strictHierarchy.Value;

					if (managersCanEditPositions != null)
						org.ManagersCanEditPositions = managersCanEditPositions.Value;

					if (sendEmailImmediately != null)
						org.SendEmailImmediately = sendEmailImmediately.Value;

					if (managersCanRemoveUsers != null)
						org.ManagersCanRemoveUsers = managersCanRemoveUsers.Value;


					if (managersCanEditSelf != null)
						org.Settings.ManagersCanEditSelf = managersCanEditSelf.Value;

					if (employeesCanEditSelf != null)
						org.Settings.EmployeesCanEditSelf = employeesCanEditSelf.Value;

					if (employeesCanCreateSurvey != null)
						org.Settings.EmployeesCanCreateSurvey = employeesCanCreateSurvey.Value;

					if (onlySeeRockAndScorecardBelowYou != null)
						org.Settings.OnlySeeRocksAndScorecardBelowYou = onlySeeRockAndScorecardBelowYou.Value;

					if (scorecardPeriod != null)
						org.Settings.ScorecardPeriod = scorecardPeriod.Value;

					if (dateFormat != null)
						org.Settings.DateFormat = dateFormat;

					if (managersCanCreateSurvey != null)
						org.Settings.ManagersCanCreateSurvey = managersCanCreateSurvey.Value;

					if (!String.IsNullOrWhiteSpace(rockName))
						org.Settings.RockName = rockName;

					if (!String.IsNullOrWhiteSpace(timeZoneId) && TimeZoneInfo.GetSystemTimeZones().Any(x => x.Id == timeZoneId))
						org.Settings.TimeZoneId = timeZoneId;

					if (weekStart != null)
						org.Settings.WeekStart = weekStart.Value;
					if (startOfYearMonth != null)
						org.Settings.StartOfYearMonth = startOfYearMonth.Value;

					if (startOfYearOffset != null)
						org.Settings.StartOfYearOffset = startOfYearOffset.Value;

					if (numberFormat != null)
						org.Settings.NumberFormat = numberFormat.Value;

					s.Update(org);

					var all = OrganizationAccessor.GetAllUserOrganizations(s, perms, organizationId);
					var cache = new Cache();
					foreach (var u in all) {
						cache.InvalidateForUser(u, CacheKeys.USERORGANIZATION);
					}


					tx.Commit();
					s.Flush();
				}
			}
		}

		public List<UserOrganizationModel> GetOrganizationManagers(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
					var managers = s.QueryOver<UserOrganizationModel>()
											.Where(x =>
												x.Organization.Id == organizationId &&
												(x.ManagerAtOrganization || x.ManagingOrganization) &&
												x.DeleteTime == null
											).List()
											.OrderBy(x => x.GetName())
											.ToList();
					return managers;
				}
			}
		}

		public static Tree GetOrganizationTree(ISession s, PermissionsUtility perms, long orgId, long? parentId = null, bool includeTeams = false, bool includeRoles = false) {
			perms.ViewOrganization(orgId);

			var org = s.Get<OrganizationModel>(orgId);

			List<UserOrganizationModel> managers;

			if (parentId == null) {
				managers = s.QueryOver<UserOrganizationModel>()
							.Where(x => x.Organization.Id == orgId && x.ManagingOrganization)
							//.Fetch(x => x.Teams).Default
							.List()
							.ToListAlive();
			} else {
				var parent = s.Get<UserOrganizationModel>(parentId);

				if (orgId != parent.Organization.Id)
					throw new PermissionsException("Organizations do not match");

				perms.ViewOrganization(parent.Organization.Id);
				managers = parent.AsList();
			}

			var managerIds = managers.Select(x => x.Id).ToList();

			if (includeTeams) {
				var managerTeams = s.QueryOver<TeamDurationModel>().Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.UserId).IsIn(managerIds).List().ToList();

				foreach (var t in managerTeams) {
					managers.First(x => x.Id == t.UserId).Teams.Add(t);
				}
			}

			var caller = perms.GetCaller();

			var deep = DeepAccessor.Users.GetSubordinatesAndSelf(s, caller, caller.Id);

			//var classes = "organizations".AsList("admin");

			var managingOrg = caller.ManagingOrganization && orgId == caller.Organization.Id;

			var tree = new Tree() {
				name = org.Name.Translate(),
				@class = "organizations",
				id = -1 * orgId,
				children = managers.Select(x => x.GetTree(s, perms,deep, caller.Id, force: managingOrg, includeRoles: includeRoles)).ToList()
			};

			return tree;
		}

		public Tree GetOrganizationTree(UserOrganizationModel caller, long orgId, bool includeRoles = false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetOrganizationTree(s, perms, orgId, null, true, includeRoles);
				}
			}
		}
		/*
        private Tree Children(String name, String subtext, String classStr,bool manager, long id, List<UserOrganizationModel> users,List<long> deep,long youId)
        {

            /*var newClasses = classes.ToList();
            if (classes.Count > 0)
                newClasses.RemoveAt(0);*/
		/*var managing =deep.Any(x=>x==id);

        if (managing)
            classStr += " managing";
        if (id == youId)
            classStr += " you";

        return new Tree()
        {
            name = name,
            id = id,
            subtext = subtext,
            @class = classStr,
            managing = managing,
            manager = manager,
            children = users.ToListAlive().Select(x =>{
                var selfClasses = x.Teams.ToListAlive().Select(y=>y.Team.Name).ToList();
                selfClasses.Add("employee");
                if (x.ManagingOrganization)
                    selfClasses.Add("admin");
                if(x.ManagerAtOrganization)
                    selfClasses.Add("manager");


                return Children(
                        x.GetName(),
                        x.GetTitles(),
                        String.Join(" ", selfClasses.Select(y => Regex.Replace(y, "[^a-zA-Z0-9]", "_"))),
                        x.IsManager(),
                        x.Id,
                        x.ManagingUsers.ToListAlive().Select(y => y.Subordinate).ToList(),
                        deep,
                        youId
                    );
            }
                ).ToList()
        };*
    }*/

		public List<QuestionCategoryModel> GetOrganizationCategories(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
					var orgCategories = s.QueryOver<QuestionCategoryModel>()
									.Where(x => (x.OriginId == organizationId && x.OriginType == OriginType.Organization))
									.List()
									.ToList();

					var appCategories = ApplicationAccessor.GetApplicationCategories(s);

					return orgCategories.Union(appCategories).ToList();
				}
			}
		}

		//Gets all users and populates direct subordinates
		/*
        public List<UserOrganizationModel> GetOrganizationMembersAndSubordinates(UserOrganizationModel caller, long forUserId, bool allSubordinates)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    if (allSubordinates == false)
                        throw new NotImplementedException("All subordinates not implemented. Only direct subordinates.");

                    var perms=PermissionsUtility.Create(s, caller).ViewUserOrganization(forUserId,false);

                    var user=s.Get<UserOrganizationModel>(forUserId);
                    var allOrgUsers=s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == user.Organization.Id).List().ToList();

                    var directReports = s.QueryOver<ManagerDuration>().Where(x => x.ManagerId == forUserId).List().ToList();

                    foreach (var u in allOrgUsers)
                    {
                        u.SetPersonallyManaging(directReports.Any(x => x.SubordinateId == u.Id));
                    }

                    return allOrgUsers;
                }
            }
        }*/

		public OrganizationModel GetOrganization(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
					return s.Get<OrganizationModel>(organizationId);
				}
			}
		}

		public static List<CompanyValueModel> GetCompanyValues(AbstractQuery query, PermissionsUtility perms, long organizationId, DateRange range) {
			perms.ViewOrganization(organizationId);
			return query.Where<CompanyValueModel>(x => x.OrganizationId == organizationId).FilterRange(range).ToList();
		}

		public List<CompanyValueModel> GetCompanyValues(UserOrganizationModel caller, long organizationId, DateRange range = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetCompanyValues(s.ToQueryProvider(true), perms, organizationId, range);
				}
			}
		}

		public static void EditCompanyValues(ISession s, PermissionsUtility perms, long organizationId, List<CompanyValueModel> companyValues) {
			perms.EditCompanyValues(organizationId);
			var category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.EVALUATION);


			foreach (var r in companyValues) {
				if (r.OrganizationId != organizationId)
					throw new PermissionsException("You do not have access to this value.");

				r.Category = category;
				s.SaveOrUpdate(r);
			}

			var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
			var vtoIds = s.QueryOver<VtoModel>().Where(x => x.Organization.Id == organizationId).Select(x => x.Id).List<long>();
			foreach (var vtoId in vtoIds) {
				var group = hub.Clients.Group(VtoHub.GenerateVtoGroupId(vtoId));
				group.update(new AngularVTO(vtoId) {
					Values = AngularList.Create(AngularListType.ReplaceAll, AngularCompanyValue.Create(companyValues))
				});
			}


		}
		public void EditCompanyValues(UserOrganizationModel caller, long organizationId, List<CompanyValueModel> companyValues) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					EditCompanyValues(s, perms, organizationId, companyValues);
					tx.Commit();
					s.Flush();
				}
			}
		}
		public static List<RockModel> GetCompanyRocks(ISession s, PermissionsUtility perms, long organizationId) {
			perms.ViewOrganization(organizationId);
			return s.QueryOver<RockModel>().Where(x => x.DeleteTime == null && x.OrganizationId == organizationId && x.CompanyRock).List().ToList();
		}

		public List<RockModel> GetCompanyRocks(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetCompanyRocks(s, perms, organizationId);
				}
			}
		}

		public static List<long> GetAllUserOrganizationIds(ISession s, PermissionsUtility perm, long organizationId) {
			perm.ViewOrganization(organizationId);

			return s.QueryOver<UserOrganizationModel>()
				.Where(x => x.DeleteTime == null && x.Organization.Id == organizationId)
				.Select(x => x.Id)
				.List<long>().ToList();
		}
		[Obsolete("Dangerous")]
		public static List<UserOrganizationModel> GetAllUserOrganizations(ISession s, PermissionsUtility perm, long organizationId) {
			perm.ViewOrganization(organizationId);
			return s.QueryOver<UserOrganizationModel>()
				.Where(x => x.DeleteTime == null && x.Organization.Id == organizationId)
				.List().ToList();
		}

		public void UpdateProducts(UserOrganizationModel caller, bool enableReview, bool enableL10, bool enableSurvey, BrandingType branding) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).ManagingOrganization(caller.Organization.Id);

					var org = s.Get<OrganizationModel>(caller.Organization.Id);

					org.Settings.EnableL10 = enableL10;
					org.Settings.EnableReview = enableReview;
					org.Settings.Branding = branding;
					org.Settings.EnableSurvey = enableSurvey;


					s.Update(org);

					tx.Commit();
					s.Flush();

					var all = OrganizationAccessor.GetAllUserOrganizations(s, perms, caller.Organization.Id);
					var cache = new Cache();
					foreach (var u in all) {
						cache.InvalidateForUser(u, CacheKeys.USERORGANIZATION);
					}
				}
			}
		}

		public static IEnumerable<Askable> AskablesAboutOrganization(AbstractQuery query, PermissionsUtility perms, long orgId, DateRange range) {
			perms.ViewOrganization(orgId);
			return query.Where<AboutCompanyAskable>(x => x.DeleteTime == null && x.OrganizationId == orgId)
				.FilterRange(range)
				.ToList();
		}

		public static List<AboutCompanyAskable> GetQuestionsAboutCompany(UserOrganizationModel caller, long orgId, DateRange range) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller);
					var q = s.ToQueryProvider(false);
					return AskablesAboutOrganization(q, perm, orgId, range).Cast<AboutCompanyAskable>().ToList();
				}
			}
		}

		public static void EditQuestionsAboutCompany(UserOrganizationModel caller, List<AboutCompanyAskable> questions) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller);
					questions.Select(x => x.OrganizationId)
						.Distinct()
						.ForEach(x =>
							perm.EditOrganizationQuestions(x)
						);

					var cat = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.COMPANY_QUESTION);
					foreach (var q in questions) {
						q.Organization = s.Load<OrganizationModel>(q.OrganizationId);
						q.Category = cat;
						s.SaveOrUpdate(q);
					}

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static List<UserLookup> GetOrganizationMembersLookup(ISession s, PermissionsUtility perms, long organizationId, bool populatePersonallyManaging, PermissionType? type = null) {
			var caller = perms.GetCaller();
			perms.ViewOrganization(organizationId);
			var users = s.QueryOver<UserLookup>().Where(x => x.OrganizationId == organizationId && x.DeleteTime == null).List().ToList();
			if (populatePersonallyManaging) {
				var subs = DeepAccessor.Users.GetSubordinatesAndSelf(s, caller, caller.Id, type);

				var orgManager = PermissionsAccessor.AnyTrue(s, caller, type, x => x.ManagingOrganization);


				var isRadialAdmin = perms.IsPermitted(x => x.RadialAdmin());
				users.ForEach(u =>
					u._PersonallyManaging = (isRadialAdmin || (orgManager && u.OrganizationId == organizationId) || subs.Contains(u.UserId)));
			}

			return users;
		}

		public List<UserLookup> GetOrganizationMembersLookup(UserOrganizationModel caller, long organizationId, bool populatePersonallyManaging, PermissionType? type = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetOrganizationMembersLookup(s, perms, organizationId, populatePersonallyManaging, type);
				}
			}
		}

		public static List<PositionDurationModel> GetOrganizationUserPositions(ISession s, PermissionsUtility perm, long orgId) {
			perm.ViewOrganization(orgId);
			return s.QueryOver<PositionDurationModel>().JoinQueryOver(x => x.Position).Where(x => x.Organization.Id == orgId && x.DeleteTime == null).List().ToList();
		}

		public static List<UserOrganizationModel> GetUsersWithOrganizationPositions(ISession s, PermissionsUtility perm, long orgId, long orgPositionId) {
			perm.ViewOrganization(orgId);
			var ids = s.QueryOver<PositionDurationModel>().Where(x => x.DeleteTime == null).JoinQueryOver(x => x.Position).Where(x => x.Organization.Id == orgId && x.DeleteTime == null && x.Id == orgPositionId).Select(x => x.UserId).List<long>().ToList();

			return s.QueryOver<UserOrganizationModel>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(ids).List().ToList();
		}

		public static List<ResponsibilityGroupModel> GetOrganizationResponsibilityGroupModels(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
					return s.QueryOver<ResponsibilityGroupModel>().Where(x => x.DeleteTime == null && x.Organization.Id == organizationId).List().ToList();
				}
			}
		}

		public void EnsureAllAtOrganization(UserOrganizationModel caller, long organizationId, List<long> userIds, bool includedDeleted = false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
					var q = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == organizationId);
					if (!includedDeleted) {
						q = q.Where(x => x.DeleteTime == null);
					}

					var foundIds = q.WhereRestrictionOn(x => x.Id).IsIn(userIds).Select(x => x.Id).List<long>().ToList();

					foreach (var id in userIds) {
						if (!foundIds.Any(x => x == id))
							throw new PermissionsException("User not part of organization");
					}

				}
			}
		}
	}
}