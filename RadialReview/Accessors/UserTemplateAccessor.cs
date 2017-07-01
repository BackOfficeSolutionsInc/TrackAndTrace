using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using System.Security;
using System.Web;
using Amazon.IdentityManagement.Model;
using Mandrill.Models;
using NHibernate.Linq;
using NHibernate.SqlCommand;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.Periods;
using RadialReview.Models.Scorecard;
using RadialReview.Models.UserTemplate;
using RadialReview.Utilities;
using WebGrease.Css.Extensions;
using NHibernate;
using RadialReview.Hooks;
using RadialReview.Utilities.Hooks;
using System.Threading.Tasks;

namespace RadialReview.Accessors {
	public class UserTemplateAccessor {
		public static void CreateTemplate(UserOrganizationModel caller, UserTemplate template) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					CreateTemplate(s, perms, caller.Organization, template);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static void CreateTemplate(ISession s, PermissionsUtility perms, OrganizationModel org, UserTemplate template) {
			if (template.Id != 0)
				throw new PermissionsException("Id was not zero");
			if (template.AttachId == 0)
				throw new PermissionsException("AttachId was zero");
			if (template.AttachType == AttachType.Invalid)
				throw new PermissionsException("AttachType was invalid");

			var found = s.QueryOver<UserTemplate>().Where(x => x.DeleteTime == null && x.AttachId == template.AttachId && x.AttachType == template.AttachType).SingleOrDefault();

			if (found != null)
				throw new PermissionsException("Template already exists.");

			perms.ConfirmAndFix(template,
				x => x.OrganizationId,
				x => x.Organization,
				x => x.CreateTemplates);

			s.Save(template);
			var a = new Attach(template.AttachType, template.AttachId);

			AttachAccessor.SetTemplateUnsafe(s, a, template.Id);
			var members = AttachAccessor.GetMemberIdsUnsafe(s, a);
			foreach (var member in members) {
				_AddUserToTemplateUnsafe(s, org, template.Id, member, false);
			}
		}

		public static UserTemplate GetUserTemplate(UserOrganizationModel caller, long utId, bool loadRocks = false, bool loadRoles = false, bool loadMeasurables = false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var p = PermissionsUtility.Create(s, caller);
					var found = s.Get<UserTemplate>(utId);
					if (found == null)
						throw new PermissionsException("Template does not exist");
					p.ViewTemplate(found.Id);

					found._Attach = AttachAccessor.PopulateAttachUnsafe(s, found.AttachId, found.AttachType);

					if (loadRocks) {
						found._Rocks = s.QueryOver<UserTemplate.UT_Rock>()
							.Where(x => x.DeleteTime == null && x.TemplateId == utId)
							.Fetch(x => x.Period).Eager
							.List().ToList();
					}
					if (loadRoles) {
						found._Roles = RoleAccessor.GetRolesForAttach_Unsafe(s, new Attach(found.AttachType, found.AttachId));
							//s.QueryOver<UserTemplate.UT_Role>()
							//.Where(x => x.DeleteTime == null && x.TemplateId == utId)
							//.List().ToList();
					}
					if (loadMeasurables) {
						found._Measurables = s.QueryOver<UserTemplate.UT_Measurable>()
							.Where(x => x.DeleteTime == null && x.TemplateId == utId)
							.List().ToList();
					}

					return found;
				}
			}
		}
		public static UserTemplate GetAttachedUserTemplate(UserOrganizationModel caller, long attachId, AttachType attachType) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var p = PermissionsUtility.Create(s, caller);
					if (attachId == 0)
						throw new PermissionsException("AttachId was zero");
					if (attachType == AttachType.Invalid)
						throw new PermissionsException("AttachType was invalid");

					var found = _GetAttachedUserTemplateUnsafe(s, attachId, attachType);
					if (found == null)
						throw new PermissionsException("Template does not exist");

					p.ViewTemplate(found.Id);

					return found;
				}
			}
		}
		public static UserTemplate _GetAttachedUserTemplateUnsafe(ISession s, long attachId, AttachType attachType) {
			var found = s.QueryOver<UserTemplate>()
				.Where(x => x.DeleteTime == null && x.AttachId == attachId && x.AttachType == attachType)
				.SingleOrDefault();
			return found;
		}

		public static void AddMeasurableToTemplate(UserOrganizationModel caller, long templateId, String measurable, LessGreater goalDirection, decimal goal) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var p = PermissionsUtility.Create(s, caller);

					var utm = new UserTemplate.UT_Measurable {
						Goal = goal,
						GoalDirection = goalDirection,
						TemplateId = templateId,
						Measurable = measurable
					};
					p.ConfirmAndFix(utm, x => x.TemplateId, x => x.Template, x => x.EditTemplate);
					s.Save(utm);

					var users = s.QueryOver<UserTemplate.UT_User>()
						.Where(x => x.DeleteTime == null && x.TemplateId == templateId)
						.Fetch(x => x.User).Eager
						.List().ToList();

					//Todo confirm users are at organization

					foreach (var u in users) {
						var user = u.User;
						if (user.Organization.Id != caller.Organization.Id)
							throw new PermissionsException("Organization ids do not match");
						s.Save(new MeasurableModel(caller.Organization) {
							AccountableUser = user,
							AccountableUserId = user.Id,
							AdminUser = user,
							AdminUserId = user.Id,
							GoalDirection = goalDirection,
							Goal = goal,
							Title = measurable,
							FromTemplateItemId = utm.Id,
						});
						//user.NumMeasurables += 1;
						s.Update(user);
						s.Flush();
						user.UpdateCache(s);
					}


					tx.Commit();
					s.Flush();
				}
			}
		}
		public static void AddRoleToAttach_Unsafe(ISession s, PermissionsUtility p,long organizationId, Attach attach, RoleModel role) {


			//var template = s.QueryOver<UserTemplate>().Where(x => x.AttachId == attach.Id && x.AttachType == attach.Type && x.DeleteTime == null).Take(1).SingleOrDefault();
			//if (template == null) {
			//	var org = s.Get<OrganizationModel>(organizationId);
			//	template = new UserTemplate() {
			//		OrganizationId = org.Id,
			//		Organization = org,
			//		AttachId = attach.Id,
			//		AttachType = attach.Type,
			//	};
			//	CreateTemplate(s, p, org, template);
			//}


			s.Save(new RoleLink() {
				AttachId = attach.Id,
				AttachType = attach.Type,
				OrganizationId = organizationId,
				RoleId = role.Id
			});



			//AddRoleToTemplate(s, p, template.Id, organizationId, role);


		}

		public static async Task AddRoleToTemplate(ISession s, PermissionsUtility p, long templateId, long orgId, String role) {
			var utm = new UserTemplate.UT_Role() {
				//Role = role,
				TemplateId = templateId,
			};
			p.ConfirmAndFix(utm, x => x.TemplateId, x => x.Template, x => x.EditTemplate);
			s.Save(utm);


			var category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.EVALUATION);

			//foreach (var uid in users)
			//{
			//var user = uid.User;
			//if (user.Organization.Id != caller.Organization.Id)
			//	throw new PermissionsException("Organization ids do not match");

			var template = s.Get<UserTemplate>(templateId);

			var rm = new RoleModel() {
				//ForUserId = user.Id,
				OrganizationId = orgId,
				//FromTemplateItemId = utm.Id,
				Role = role,
				Category = category,
			};
			s.Save(rm);
			await HooksRegistry.Each<IRolesHook>(x => x.CreateRole(s, rm));

			s.Save(new RoleLink() {
				AttachId = template.AttachId,
				AttachType = template.AttachType,
				OrganizationId = orgId,
				RoleId = rm.Id
			});

			utm.RoleId = rm.Id;
			s.Flush();

			var users = s.QueryOver<UserTemplate.UT_User>()
					.Where(x => x.DeleteTime == null && x.TemplateId == templateId)
					.Fetch(x => x.User).Eager
					.List().ToList();
			foreach (var utu in users) {
				var user = utu.User;
				//user.NumRoles += 1;
				s.Update(user);
				user.UpdateCache(s);
			}

		}
		public static async Task AddRoleToTemplate(UserOrganizationModel caller, long templateId, String role) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var p = PermissionsUtility.Create(s, caller);

					await AddRoleToTemplate(s, p, templateId, caller.Organization.Id, role);
					
					tx.Commit();
					s.Flush();
				}
			}
		}
		public static void AddRockToTemplate(UserOrganizationModel caller, long templateId, String rock, long periodId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var p = PermissionsUtility.Create(s, caller);

					var period = s.Get<PeriodModel>(periodId);
					if (period.Organization.Id != caller.Organization.Id) {
						throw new PermissionsException("PeriodId");
					}

					var utm = new UserTemplate.UT_Rock() {
						Rock = rock,
						TemplateId = templateId,
						PeriodId = period.Id,
						Period = period
					};
					p.ConfirmAndFix(utm, x => x.TemplateId, x => x.Template, x => x.EditTemplate);
					s.Save(utm);


					var users = s.QueryOver<UserTemplate.UT_User>()
							.Where(x => x.DeleteTime == null && x.TemplateId == templateId)
							.Fetch(x => x.User).Eager
							.List().ToList();
					var category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.EVALUATION);

					foreach (var uid in users) {
						var user = uid.User;
						if (user.Organization.Id != caller.Organization.Id)
							throw new PermissionsException("Organization ids do not match");

						s.Save(new RockModel() {
							OnlyAsk = AboutType.Self,
							Category = category,
							ForUserId = user.Id,
							OrganizationId = caller.Organization.Id,
							FromTemplateItemId = utm.Id,
							Rock = rock,
							Period = period,
							PeriodId = periodId,
						});

						//user.NumRocks += 1;
						s.Update(user);
						s.Flush();
						user.UpdateCache(s);
					}

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static void UpdateRockTemplate(UserOrganizationModel caller, long utRockId, String rock, long periodId, DateTime? deleteTime) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var p = PermissionsUtility.Create(s, caller);

					var period = s.Get<PeriodModel>(periodId);
					if (period.Organization.Id != caller.Organization.Id)
						throw new PermissionsException("PeriodId");

					var utRock = s.Get<UserTemplate.UT_Rock>(utRockId);//.Where(x=>x.DeleteTime==null && x.TemplateId==templateId)
					p.EditTemplate(utRock.TemplateId);

					utRock.Rock = rock;
					utRock.PeriodId = periodId;
					utRock.DeleteTime = deleteTime;
					s.Update(utRock);

					var rocks = s.QueryOver<RockModel>()
						.Where(x => x.DeleteTime == null && x.FromTemplateItemId == utRock.Id)
						.List().ToList();

					foreach (var r in rocks) {
						r.Rock = rock;
						r.PeriodId = periodId;
						r.Period = period;
						r.DeleteTime = deleteTime;
						s.Update(r);
						if (deleteTime.HasValue) {
							var u = s.Get<UserOrganizationModel>(r.ForUserId);
							//u.NumRocks -= 1;
							if (u != null) {
								s.Update(u);
								s.Flush();
								u.UpdateCache(s);
							}
						}
					}

					tx.Commit();
					s.Flush();
				}
			}
		}


		//public static async Task UpdateRoleTemplate(UserOrganizationModel caller, long utRoleId, string role, DateTime? deleteTime) {
		//	RoleModel r;
		//	using (var s = HibernateSession.GetCurrentSession()) {
		//		using (var tx = s.BeginTransaction()) {
		//			var p = PermissionsUtility.Create(s, caller);
		//			var utRole = s.Get<UserTemplate.UT_Role>(utRoleId);
		//			p.EditTemplate(utRole.TemplateId);
		//			utRole.DeleteTime = deleteTime;
		//			s.Update(utRole);
		//			r = s.Get<RoleModel>(utRole.RoleId);
		//			r.Role = role;
		//			r.DeleteTime = deleteTime;
		//			s.Update(r);					
		//			tx.Commit();
		//			s.Flush();
		//		}
		//	}
		//	using (var s = HibernateSession.GetCurrentSession()) {
		//		using (var tx = s.BeginTransaction()) {
		//			await HooksRegistry.Each<IRolesHook>(x => x.UpdateRole(s, r));
		//			tx.Commit();
		//			s.Flush();
		//		}
		//	}
		//}

		public static void UpdateMeasurableTemplate(UserOrganizationModel caller, long utMeasurableId, String measurable, LessGreater goalDirection, decimal goal, DateTime? deleteTime) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var p = PermissionsUtility.Create(s, caller);


					var utMeasurable = s.Get<UserTemplate.UT_Measurable>(utMeasurableId);//.Where(x=>x.DeleteTime==null && x.TemplateId==templateId)
					p.EditTemplate(utMeasurable.TemplateId);


					utMeasurable.Measurable = measurable;
					utMeasurable.Goal = goal;
					utMeasurable.GoalDirection = goalDirection;
					utMeasurable.DeleteTime = deleteTime;
					s.Update(utMeasurable);

					var measurables = s.QueryOver<MeasurableModel>()
						.Where(x => x.DeleteTime == null && x.FromTemplateItemId == utMeasurable.Id)
						.List().ToList();

					foreach (var m in measurables) {
						m.Title = measurable;
						m.Goal = goal;
						m.GoalDirection = goalDirection;
						m.DeleteTime = deleteTime;
						s.Update(m);
						if (deleteTime.HasValue) {
							var u = s.Get<UserOrganizationModel>(m.AccountableUserId);
							//u.NumMeasurables -= 1;

							if (u != null) {
								s.Update(u);
								s.Flush();
								u.UpdateCache(s);
							}
						}
					}
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static void AddUserToTemplate(UserOrganizationModel caller, long templateId, long userId, bool forceJobDescription) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var p = PermissionsUtility.Create(s, caller)
						.EditTemplate(templateId)
						.ManagesUserOrganization(userId, false);
					_AddUserToTemplateUnsafe(s, caller.Organization, templateId, userId, forceJobDescription);

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static void _AddUserToTemplateUnsafe(ISession s, OrganizationModel organization, long templateId, long userId, bool forceJobDescription) {

			var user = s.Get<UserOrganizationModel>(userId);
			var template = s.Get<UserTemplate>(templateId);


			var category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.EVALUATION);
			#region Measurables
			var newMeasurables = s.QueryOver<UserTemplate.UT_Measurable>()
				.Where(x => x.DeleteTime == null && x.TemplateId == templateId)
				.List().ToList();
			var existingMeasurables = s.QueryOver<MeasurableModel>()
				.Where(x => x.DeleteTime == null && x.AccountableUser.Id == userId)
				.List().ToList();

			var toAddMeasurables = newMeasurables.Where(x => existingMeasurables.All(y => y.FromTemplateItemId != x.Id));
			foreach (var a in toAddMeasurables) {
				s.Save(new MeasurableModel(organization) {
					AccountableUser = user,
					AccountableUserId = user.Id,
					AdminUser = user,
					AdminUserId = user.Id,
					GoalDirection = a.GoalDirection,
					Goal = a.Goal,
					Title = a.Measurable,
					FromTemplateItemId = a.Id,
				});
				//user.NumMeasurables += 1;
			}
			#endregion
			#region Rocks
			var newRocks = s.QueryOver<UserTemplate.UT_Rock>()
				.Where(x => x.DeleteTime == null && x.TemplateId == templateId)
				.List().ToList();
			var existingRocks = s.QueryOver<RockModel>()
				.Where(x => x.DeleteTime == null && x.AccountableUser.Id == userId)
				.List().ToList();

			var toAddRocks = newRocks.Where(x => existingRocks.All(y => y.FromTemplateItemId != x.Id));
			foreach (var a in toAddRocks) {
				s.Save(new RockModel() {
					OnlyAsk = AboutType.Self,
					ForUserId = user.Id,
					OrganizationId = organization.Id,
					FromTemplateItemId = a.Id,
					Rock = a.Rock,
					Period = s.Load<PeriodModel>(a.PeriodId),
					PeriodId = a.PeriodId,
					Category = category
				});
				//user.NumRocks += 1;
			}
			#endregion
			#region Roles
			/*var newRoles = s.QueryOver<UserTemplate.UT_Role>()
				.Where(x => x.DeleteTime == null && x.TemplateId == templateId)
				.List().ToList();
			var existingRoles = s.QueryOver<RoleModel>()
				.Where(x => x.DeleteTime == null && x.ForUserId == userId)
				.List().ToList();

			var toAddRoles = newRoles.Where(x => existingRoles.All(y => y.FromTemplateItemId != x.Id));
			foreach (var a in toAddRoles) {
				s.Save(new RoleModel() {
					ForUserId = user.Id,
					OrganizationId = organization.Id,
					FromTemplateItemId = a.Id,
					Role = a.Role,
					Category = category
				});
				//user.NumRoles += 1;
			}*/
			#endregion
			#region Job Description
			if (String.IsNullOrWhiteSpace(user.JobDescription) || forceJobDescription) {
				user.JobDescription = template.JobDescription;
				user.JobDescriptionFromTemplateId = templateId;
			}
			#endregion

			var utUser = new UserTemplate.UT_User {
				Template = template,
				TemplateId = templateId,
				User = user,

			};
			s.Save(utUser);
			s.Update(user);
			s.Flush();
			user.UpdateCache(s);
		}
		public static void _RemoveUserToTemplateUnsafe(ISession s, long templateId, long userId) {
			var now = DateTime.UtcNow;
			var found = s.QueryOver<UserTemplate.UT_User>().Where(x => x.DeleteTime == null && x.TemplateId == templateId && x.User.Id == userId).SingleOrDefault();
			if (found != null) {
				found.DeleteTime = now;
				s.Update(found);

				var potentialTemplateRocks = s.QueryOver<RockModel>()
					.Where(x => x.DeleteTime == null && x.ForUserId == userId && x.FromTemplateItemId != null)
					.List().ToList();
				//var potentialTemplateRoles = s.QueryOver<RoleModel>()
				//	.Where(x => x.DeleteTime == null && x.ForUserId == userId && x.FromTemplateItemId != null)
				//	.List().ToList();
				var potentialTemplateMeasurables = s.QueryOver<MeasurableModel>()
					.Where(x => x.DeleteTime == null && x.AccountableUserId == userId && x.FromTemplateItemId != null)
					.List().ToList();

				var deleteRockIds = s.QueryOver<UserTemplate.UT_Rock>().Where(x => x.DeleteTime == null && x.TemplateId == templateId)
					.WhereRestrictionOn(x => x.Id).IsIn(potentialTemplateRocks.Select(x => x.FromTemplateItemId).ToList())
					.Select(x => x.Id).List<long>().ToList();
				//var deleteRoleIds = s.QueryOver<UserTemplate.UT_Role>().Where(x => x.DeleteTime == null && x.TemplateId == templateId)
				//					.WhereRestrictionOn(x => x.Id).IsIn(potentialTemplateRoles.Select(x => x.FromTemplateItemId).ToList())
				//					.Select(x => x.Id).List<long>().ToList();
				var deleteMeasurableIds = s.QueryOver<UserTemplate.UT_Measurable>().Where(x => x.DeleteTime == null && x.TemplateId == templateId)
									 .WhereRestrictionOn(x => x.Id).IsIn(potentialTemplateMeasurables.Select(x => x.FromTemplateItemId).ToList())
									 .Select(x => x.Id).List<long>().ToList();

				foreach (var x in potentialTemplateRocks) {
					if (deleteRockIds.Contains(x.FromTemplateItemId.Value)) {
						x.DeleteTime = now;
						s.Update(x);
					}
				}
				//foreach (var x in potentialTemplateRoles) {
				//	if (deleteRoleIds.Contains(x.FromTemplateItemId.Value)) {
				//		x.DeleteTime = now;
				//		s.Update(x);
				//	}
				//}
				foreach (var x in potentialTemplateMeasurables) {
					if (deleteMeasurableIds.Contains(x.FromTemplateItemId.Value)) {
						x.DeleteTime = now;
						s.Update(x);
					}
				}

				var user = s.Get<UserOrganizationModel>(userId);
				if (user.JobDescriptionFromTemplateId == templateId) {
					user.JobDescriptionFromTemplateId = null;
					s.Update(user);
				}
				s.Flush();
				user.UpdateCache(s);
			}
		}


		public static void UpdateJobDescription(UserOrganizationModel caller, long templateId, string jobDescription, bool overrideExisting) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {


					var p = PermissionsUtility.Create(s, caller).EditTemplate(templateId);
					var template = s.Get<UserTemplate>(templateId);

					if (String.IsNullOrWhiteSpace(jobDescription))
						jobDescription = null;

					template.JobDescription = jobDescription;
					s.Update(template);
					var users = s.QueryOver<UserTemplate.UT_User>().Where(x => x.DeleteTime == null && x.TemplateId == templateId)
							.Fetch(x => x.User).Eager
							.List().Select(x => x.User).ToList();


					if (!overrideExisting) {
						users = users.Where(x => x.JobDescriptionFromTemplateId.HasValue).ToList();
					}


					foreach (var u in users) {
						u.JobDescription = template.JobDescription;
						u.JobDescriptionFromTemplateId = templateId;
						s.Update(u);
						s.Flush();
						u.UpdateCache(s);
					}

					tx.Commit();
					s.Flush();
				}
			}
		}


	}
}