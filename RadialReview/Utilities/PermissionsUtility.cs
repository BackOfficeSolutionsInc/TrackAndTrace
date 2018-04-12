using System.Linq.Expressions;
using Amazon.ElasticTranscoder.Model;
using Amazon.IdentityManagement.Model;
using FluentNHibernate;
using log4net;
using Microsoft.Ajax.Utilities;
using NHibernate;
using NHibernate.Linq;
using RadialReview.Accessors;
using RadialReview.Controllers;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Components;
using RadialReview.Models.Dashboard;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.Permissions;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Survey;
using RadialReview.Models.Todo;
using RadialReview.Models.UserModels;
using RadialReview.Models.Prereview;
using RadialReview.Models.UserTemplate;
using RadialReview.Models.VTO;
using Twilio;
using RadialReview.Models.Accountability;
using System.Threading;
using RadialReview.Models.Rocks;
using RadialReview.Reflection;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Areas.CoreProcess.Accessors;
using RadialReview.Areas.CoreProcess.Models;
using RadialReview.Utilities.CoreProcess;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Crosscutting.EventAnalyzers.Models;

namespace RadialReview.Utilities {
	//[Obsolete("Not really obsolete. I just want this to stick out.", false)]
	public partial class PermissionsUtility {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		protected ISession session;
		protected UserOrganizationModel caller;

		#region Cache
		public class CacheChecker {
			private PermissionsUtility p;
			private String key;

			public CacheChecker(String key, PermissionsUtility p) {
				this.key = p.caller.Id + "~" + key;
				this.p = p;
			}

			public PermissionsUtility Execute(Func<PermissionsUtility> action) {
				if (p.cache.ContainsKey(key)) {
					if (p.cache[key].Exception != null)
						throw p.cache[key].Exception;
					return p;
				} else {
					try {
						var result = action();
						p.cache[key] = new CacheResult();
						return result;
					} catch (Exception e) {
						p.cache[key] = new CacheResult() { Exception = e };
						throw;
					}
				}
			}
		}

		public class CacheResult {
			public Exception Exception { get; set; }
		}

		protected Dictionary<string, CacheResult> cache = new Dictionary<string, CacheResult>();

		protected List<PermissionOverride> Overrides { get; set; }

		public PermissionsUtility RadialAdmin(bool allowSpecialOrgs = false) {
			if (IsRadialAdmin(caller, allowSpecialOrgs))
				return this;
			throw new PermissionsException();
		}
		protected static Boolean IsRadialAdmin(UserOrganizationModel caller, bool allowSpecialOrgs = false) {

			var adminFromThread = Thread.GetData(Thread.GetNamedDataSlot("IsRadialAdmin")).NotNull(x => (bool)x);
			allowSpecialOrgs = allowSpecialOrgs || Thread.GetData(Thread.GetNamedDataSlot("AllowSpecialOrgs")).NotNull(x => (bool)x);

			if (caller != null && (caller.IsRadialAdmin || caller._IsRadialAdmin || adminFromThread || (caller.User != null && caller.User.IsRadialAdmin))) {
				if (!allowSpecialOrgs && caller.Organization != null && (caller.Organization.Id == 1795 || caller.Organization.Id == 1634)) {
					//1795 = EOSWW
					//1634 = TT
					return false;
				}
				return true;
			}
			return false;
		}


		#region Construction
		protected PermissionsUtility(ISession session, UserOrganizationModel caller) {
			this.session = session;
			this.caller = caller;

		}
		[Obsolete("Avoid using")]
		public static PermissionsUtility CreateAdmin(ISession s) {
			return Create(s, UserOrganizationModel.CreateAdmin());
		}

		public static PermissionsUtility Create(ISession session, UserOrganizationModel caller) {
			var attached = caller;
			if (!session.Contains(caller) && caller.Id != UserOrganizationModel.ADMIN_ID) {
				attached = session.Load<UserOrganizationModel>(caller.Id);
				attached._ClientTimestamp = caller._ClientTimestamp;
			}
			if (caller.DeleteTime != null && caller.DeleteTime < DateTime.UtcNow) {
				throw new PermissionsException("User has been deleted") {
					NoErrorReport = true
				};
			}

			return new PermissionsUtility(session, attached);
		}

		#endregion



		public CacheChecker CheckCacheFirst(string key, params long[] arguments) {
			key = key + "~" + String.Join("_", arguments);
			return new CacheChecker(key, this);
		}
		#endregion

		#region User
		public PermissionsUtility EditUserModel(string userId) {
			if (IsRadialAdmin(caller))
				return this;

			if (caller.User.Id == userId)
				return this;
			throw new PermissionsException();
		}

		public PermissionsUtility EditUserOrganization(long userId) {
			if (caller.Id == userId)
				return this;

			return ManagesUserOrganization(userId, false);

			/*
            if (IsRadialAdmin(caller))
                return this;

            var user = session.Get<UserOrganizationModel>(userId);
            if (IsManagingOrganization(user.Organization.Id))
                return this;

            if (caller.IsManager() && IsOwnedBelowOrEqual(caller, x => x.Id == userId))
                return this;
            //caller.AllSubordinates.Any(x => x.Id == userId) && caller.IsManager()) //IsManager may be too much
            //return this;
            //Could do some cascading here if we want.

            throw new PermissionsException("You don't manage this user.");*/
		}

		public PermissionsUtility ViewUserOrganization(long userOrganizationId, Boolean sensitive, params PermissionType[] alsoCheck) {
			return TryWithOverrides(p => {
				return CheckCacheFirst("ViewUserOrganization", userOrganizationId, sensitive.ToLong()).Execute(() => {
					if (IsRadialAdmin(caller))
						return this;
					var userOrg = session.Get<UserOrganizationModel>(userOrganizationId);
					if (userOrg == null)
						throw new PermissionsException("User does not exist.");

					if (IsManagingOrganization(userOrg.Organization.Id))
						return this;

					if (sensitive) {
						/*if (!userOrg.Organization.StrictHierarchy && userOrg.Organization.Id == caller.Organization.Id)
                            return this;*/
						if (userOrganizationId == caller.Id)
							return this;

						return ManagesUserOrganization(userOrganizationId, false);
						/*if (IsOwnedBelowOrEqual(caller, x => x.Id == userOrganizationId))
                            return this;*/
					} else {
						if (userOrg.Organization.Id == caller.Organization.Id)
							return this;
					}

					throw new PermissionsException();
				});
			}, alsoCheck);
		}

		public PermissionsUtility ManagesForModel(IForModel forModel, bool disableIfSelf) {
			if (IsRadialAdmin(caller))
				return this;

			if (forModel.Is<UserOrganizationModel>()) {
				return ManagesUserOrganization(forModel.ModelId, disableIfSelf);
			} else if (forModel.Is<AccountabilityNode>()) {
				if (disableIfSelf) {
					var ac = session.Get<AccountabilityNode>(forModel.ModelId);
					if (ac.NotNull(x => x.User.Id == caller.Id))
						throw new PermissionsException("Cannot access. You do not manage this.");
				}
				return ManagesAccountabilityNodeOrSelf(forModel.ModelId);
			} else if (forModel.Is<OrganizationModel>()) {
				return ManagingOrganization(forModel.ModelId);
			} else if (forModel.Is<SurveyUserNode>()) {
				var sun = session.Get<SurveyUserNode>(forModel.ModelId);
				return ManagesForModel(sun.AccountabilityNode, disableIfSelf);
			}

			throw new PermissionsException("Unrecognized ForModel type.");

		}

		public PermissionsUtility ManagesUserOrganization(long userOrganizationId, bool disableIfSelf, params PermissionType[] alsoCheck) {
			return TryWithOverrides(p => {
				if (IsRadialAdmin(caller))
					return this;
				//Confirm allowed if we manage organization.. was below
				//return TryWithOverrides(y =>
				//{
				var user = session.Get<UserOrganizationModel>(userOrganizationId);

				if (user == null)
					throw new PermissionsException("User does not exist.");

				if (IsManagingOrganization(user.Organization.Id, true))
					return this;

				if (caller.ManagingOrganization) {
					var subordinate = session.Get<UserOrganizationModel>(userOrganizationId);
					if (user != null && user.Organization.Id == caller.Organization.Id)
						return this;
				}

				if (disableIfSelf && caller.Id == userOrganizationId)
					throw new PermissionsException("You cannot do this to yourself.") { NoErrorReport = true };

				//..was here

				//Confirm this is correct. Do you want children to also be managers?
				if (caller.IsManager() && DeepAccessor.Users.GetSubordinatesAndSelf(session, caller, caller.Id).Any(x => x == userOrganizationId))
					return this;

				//if (caller.IsManager() && IsOwnedBelowOrEqual(caller, x => x.Id == userOrganizationId))
				//	return this;
				throw new PermissionsException("You do not manage this user.") { NoErrorReport = true };

			}, alsoCheck);
			//}, PermissionType.ManageEmployees);
		}

		public PermissionsUtility ManagesUserOrganizationOrSelf(long userOrganizationId) {
			if (userOrganizationId == caller.Id)
				return this;
			return ManagesUserOrganization(userOrganizationId, false);
		}

		public PermissionsUtility RemoveUser(long userId) {
			return TryWithOverrides(y => {
				var found = session.Get<UserOrganizationModel>(userId);
				if (caller.ManagingOrganization && caller.Organization.Id == found.Organization.Id)
					return this;

				if (caller.Organization.ManagersCanRemoveUsers)
					return ManagesUserOrganization(userId, true);

				throw new PermissionsException("You cannot remove this user.");
			}, PermissionType.EditEmployeeDetails);
		}
		#endregion

		#region Dashboard

		public PermissionsUtility ViewDashboardForUser(String userid) {
			var user = session.Get<UserModel>(userid);
			if (user == null)
				throw new PermissionsException("Dashboard not found");

			if (IsRadialAdmin(caller))
				return this;

			if (userid == caller.User.Id)
				return this;

			throw new PermissionsException("Cannot view dashboard");
		}

		public PermissionsUtility EditDashboard(long dashboardId) {
			var dash = session.Get<Dashboard>(dashboardId);
			if (dash == null)
				throw new PermissionsException("Dashboard not found");

			if (IsRadialAdmin(caller))
				return this;

			if (dash.ForUser.Id == caller.User.Id)
				return this;

			throw new PermissionsException("Cannot edit dashboard");

		}

		public PermissionsUtility EditTile(long tileId) {
			var tile = session.Get<TileModel>(tileId);
			if (tile == null)
				throw new PermissionsException("Tile not found");

			if (IsRadialAdmin(caller))
				return this;

			if (tile.ForUser.Id == caller.User.Id)
				return this;
			throw new PermissionsException("Cannot edit tile");
		}
		#endregion

		#region Organization
		public PermissionsUtility EditOrganization(long organizationId) {
			if (IsRadialAdmin(caller))
				return this;

			if (caller.Organization.Id == organizationId && caller.IsManagerCanEditOrganization())
				return this;
			throw new PermissionsException();
		}

		//public static bool IsManagingOrganization(long userId,bool allowManagers=false) {
		//	if (caller.Organization.Id == orgId_DoNotUse_callerOrganizationId)
		//		return caller.ManagingOrganization || (allowManagers && caller.ManagerAtOrganization && caller.Organization.ManagersCanEdit);
		//	return false;
		//}


		//[Obsolete("should never be caller.organization.id", false)]
		private bool IsManagingOrganization(long orgId_DoNotUse_callerOrganizationId, bool allowManagers = false) {
			if (caller.Organization.Id == orgId_DoNotUse_callerOrganizationId)
				return caller.ManagingOrganization || (allowManagers && caller.ManagerAtOrganization && caller.Organization.ManagersCanEdit);
			return false;
		}

		private bool IsManager(long organizationId) {
			if (caller.Organization.Id == organizationId)
				return caller.ManagingOrganization || caller.ManagerAtOrganization;
			return false;
		}
		public PermissionsUtility ViewOrganization(long organizationId) {
			if (IsRadialAdmin(caller))
				return this;
			if (caller.Organization.Id == organizationId)
				return this;
			throw new PermissionsException("Cannot view organization: " + organizationId);
		}

		public PermissionsUtility EditCompanyValues(long organizationId) {
			return EditOrganization(organizationId);
		}

		#endregion

		#region Payment
		public PermissionsUtility EditCompanyPayment(long organizationId) {
			return CanEdit(PermItem.ResourceType.UpdatePaymentForOrganization, organizationId);
			//return EditOrganization(organizationId);
		}
		#endregion

		//#region Group
		//public PermissionsUtility EditGroup(long groupId) {
		//	if (IsRadialAdmin(caller))
		//		return this;

		//	if (caller.IsManager() && IsOwnedBelowOrEqual(caller, x => x.ManagingGroups.Any(y => y.Id == groupId)))
		//		return this;

		//	throw new PermissionsException();
		//}

		//public PermissionsUtility ViewGroup(long groupId) {
		//	if (IsRadialAdmin(caller))
		//		return this;
		//	if (caller.Groups.Any(x => x.Id == groupId))
		//		return this;
		//	if (IsOwnedBelowOrEqual(caller, x => x.ManagingGroups.Any(y => y.Id == groupId)))
		//		return this;
		//	throw new PermissionsException();
		//}
		//#endregion

		#region Application
		public PermissionsUtility EditApplication(long forId) {
			if (IsRadialAdmin(caller))
				return this;
			throw new PermissionsException();
		}
		public PermissionsUtility ViewApplication(long applicationId) {
			log.Info("ViewApplication always returns true.");
			return this;
		}
		#endregion

		#region Accountability
		public PermissionsUtility ViewHierarchy(long hierarchyId) {
			return CanView(PermItem.ResourceType.AccountabilityHierarchy, hierarchyId, x => {

				var chart = session.Get<AccountabilityChart>(hierarchyId);
				x.ViewOrganization(chart.OrganizationId);
				return x;
			});
		}

		public PermissionsUtility ManagesAccountabilityNodeOrSelf(long nodeId, params PermissionType[] alsoTry) {
			if (IsRadialAdmin(caller))
				return this;

			var node = session.Get<AccountabilityNode>(nodeId);
			try {
				return TryWithOverrides(x => {

					if (IsManagingOrganization(node.OrganizationId, true))
						return x;
					try {
						return EditHierarchy(node.AccountabilityChartId);
					} catch (PermissionsException) {
						if (node.UserId != null) {
							return EditUserOrganization(node.UserId.Value); //don't add alsoTry. It will get checked n^2 times
						}
						if (DeepAccessor.ManagesNode(session, this, caller.Id, nodeId))
							return x;
					}
					throw new PermissionsException("You do not manage this node.");
				}, alsoTry);
			} catch (PermissionsException) {
				throw new PermissionsException("You do not manage this node.") {
					NoErrorReport = true
				};
			}

		}

		public PermissionsUtility EditHierarchy(long hierarchyId) {
			return CanEdit(PermItem.ResourceType.AccountabilityHierarchy, hierarchyId, x => {

				var chart = session.Get<AccountabilityChart>(hierarchyId);
				ViewOrganization(chart.OrganizationId);

				//Both are managers at the organization
				if (!(caller.ManagerAtOrganization || caller.ManagingOrganization))
					throw new PermissionsException();

				return x;
			});
		}
		public PermissionsUtility EditRole(long roleId) {
			if (IsRadialAdmin(caller))
				return this;

			var role = session.Get<RoleModel>(roleId);
			if (IsManagingOrganization(role.OrganizationId))
				return this;

			var ordering = new[] { AttachType.User, AttachType.Position, AttachType.Team }.ToList();
			var links = session.QueryOver<RoleLink>()
				.Where(x => x.DeleteTime == null && x.RoleId == roleId)
				.List()
				.OrderBy(x => ordering.IndexOf(x.AttachType))
				.ToList();

			try {
				var ors = links.Select(x => new Func<PermissionsUtility>(() => EditAttach(x.GetAttach()))).ToList();
				ors.Add(() => {
					var org = session.Get<OrganizationModel>(role.OrganizationId);
					return EditHierarchy(org.AccountabilityChartId);
				});
				return Or(ors.ToArray());
			} catch (Exception) {
				throw new PermissionsException("Cannot edit role.") {
					NoErrorReport = true
				};
			}
		}
		//public PermissionsUtility EditAccountabilityNode(long nodeId) {
		//	var node = session.Get<AccountabilityNode>(nodeId);
		//	try {
		//		return EditHierarchy(node.AccountabilityChartId);
		//	} catch (PermissionsException) {
		//		if (node.UserId != null) {
		//			return EditUserOrganization(node.UserId.Value);
		//		}
		//		var parentId = node.ParentNodeId;
		//		while (true) {
		//			if (parentId == null)
		//				break;
		//			var parent = session.Get<AccountabilityNode>(parentId.Value);
		//			if (parent.UserId != null) {
		//				return EditUserOrganization(parent.UserId.Value);
		//			}

		//			parentId = parent.ParentNodeId;
		//		}
		//	}
		//	throw new PermissionsException("Could not edit node.");
		//}
		#endregion

		#region Industry
		public PermissionsUtility EditIndustry(long forId) {
			if (IsRadialAdmin(caller))
				return this;
			throw new PermissionsException();
		}
		public PermissionsUtility ViewIndustry(long industryId) {
			log.Info("ViewIndustry always returns true.");
			return this;
		}
		#endregion

		#region Question
		public PermissionsUtility EditQuestion(long questionId) {
			if (IsRadialAdmin(caller))
				return this;

			var question = session.Get<QuestionModel>(questionId);
			if (question.OriginType != OriginType.Invalid)
				return EditOrigin(question.OriginType, question.OriginId, true);

			//var createdById = question.CreatedById;
			//if (caller.IsManager() && IsOwnedBelowOrEqual(caller, createdById))
			//	return this;

			throw new PermissionsException();
		}

		public PermissionsUtility ViewQuestion(long questionId) {

			if (IsRadialAdmin(caller))
				return this;

			var question = session.Get<QuestionModel>(questionId);

			switch (question.OriginType) {
				//case OriginType.User: if (!IsOwnedBelowOrEqual(caller, x => x.CustomQuestions.Any(y => y.Id == question.Id))) throw new PermissionsException(); break;

				//case OriginType.Group:
				//	if (!IsOwnedBelowOrEqual(caller, x => x.Groups.Any(y => y.CustomQuestions.Any(z => z.Id == question.Id)) || x.ManagingGroups.Any(y => y.CustomQuestions.Any(z => z.Id == question.Id))))
				//		throw new PermissionsException();
				//	break;

				case OriginType.Organization:
					if (caller.Organization.Id != question.OriginId)
						throw new PermissionsException();
					break;
				case OriginType.Industry:
					break;
				case OriginType.Application:
					break;
				case OriginType.Invalid:
					throw new PermissionsException();
				default:
					throw new PermissionsException();
			}
			return this;
		}

		public PermissionsUtility EditUserDetails(long forUserId) {
			return TryWithOverrides(x => {
				try {
					return ManagesUserOrganization(forUserId, true);
				} catch (PermissionsException) {
					var foundUser = session.Get<UserOrganizationModel>(forUserId);
					if (foundUser.Id == caller.Id && ((foundUser.ManagerAtOrganization && foundUser.Organization.Settings.ManagersCanEditSelf) || foundUser.Organization.Settings.EmployeesCanEditSelf || foundUser.ManagingOrganization)) {
						return this;
					}
				}
				throw new PermissionsException("Cannot edit for user.");
			}, PermissionType.EditEmployeeDetails);
		}

		public PermissionsUtility EditQuestionForUser(long forUserId) {
			return EditUserDetails(forUserId);
		}

		public PermissionsUtility EditOrganizationQuestions(long orgId) {
			return EditOrganization(orgId);
		}
		#endregion

		#region Origin
		public PermissionsUtility EditOrigin(Origin origin, bool manageOnly = false) {
			return EditOrigin(origin.OriginType, origin.OriginId, manageOnly);
		}

		public PermissionsUtility EditOrigin(OriginType origin, long originId, bool manageOnly = false) {
			switch (origin) {
				case OriginType.User:
					if (manageOnly)
						return ManagesUserOrganization(originId, true);
					return EditUserOrganization(originId);
				//case OriginType.Group:
				//	return EditGroup(originId);
				case OriginType.Organization:
					return EditOrganization(originId);
				case OriginType.Industry:
					return EditIndustry(originId);
				case OriginType.Application:
					return EditApplication(originId);
				case OriginType.Invalid:
					throw new PermissionsException();
				default:
					throw new PermissionsException();
			}
		}
		public PermissionsUtility ViewOrigin(OriginType originType, long originId) {
			switch (originType) {
				case OriginType.User:
					return ViewUserOrganization(originId, false);
				//case OriginType.Group:
				//	return ViewGroup(originId);
				case OriginType.Organization:
					return ViewOrganization(originId);
				case OriginType.Industry:
					return ViewIndustry(originId);
				case OriginType.Application:
					return ViewApplication(originId);
				case OriginType.Invalid:
					throw new PermissionsException();
				default:
					throw new PermissionsException();
			}
		}
		#endregion

		#region Category
		public PermissionsUtility ViewCategory(long id) {
			if (IsRadialAdmin(caller))
				return this;

			var category = session.Get<QuestionCategoryModel>(id);
			if (category.OriginType == OriginType.Application)
				return this;

			if (category.OriginType == OriginType.Organization && IsOwnedBelowOrEqualOrganizational(caller.Organization, new Origin(category.OriginType, category.OriginId)))
				return this;

			throw new PermissionsException();
		}
		public PermissionsUtility PairCategoryToQuestion(long categoryId, long questionId) {
			if (IsRadialAdmin(caller))
				return this;

			var category = session.Get<QuestionCategoryModel>(categoryId);
			if (questionId == 0 && category.OriginType == OriginType.Organization)
				return this;

			var question = session.Get<QuestionModel>(questionId);

			//Cant attach questions to application categories
			if (category.OriginType == OriginType.Application && !caller.IsRadialAdmin)
				throw new PermissionsException();
			//Belongs to the same organization
			if (category.OriginType == OriginType.Organization && question.OriginType == OriginType.Organization && question.OriginId == category.OriginId)
				return this;

			//TODO any other special permissions here.

			throw new PermissionsException();
		}
		#endregion

		#region Managers
		public PermissionsUtility ManagerAtOrganization(long userOrganizationId, long organizationId) {
			var user = session.Get<UserOrganizationModel>(userOrganizationId);
			//var org = session.Get<OrganizationModel>(organizationId);

			if (caller.Organization.Id != organizationId)
				throw new PermissionsException();

			if (user.Organization.Id == organizationId && (user.ManagerAtOrganization || user.ManagingOrganization))
				return this;

			throw new PermissionsException();
		}
		public PermissionsUtility ManagingOrganization(long organizationId) {
			if (IsRadialAdmin(caller))
				return this;

			var org = session.Get<OrganizationModel>(organizationId);

			if (IsManagingOrganization(organizationId, org.ManagersCanEdit))
				return this;

			throw new PermissionsException();
		}



		#endregion

		#region Teams
		public PermissionsUtility ViewTeam(long teamId) {
			// Subordinates Team
			//if (teamId == -5 && caller.IsManager()) 
			//    return this;

			if (IsRadialAdmin(caller))
				return this;

			var team = session.Get<OrganizationTeamModel>(teamId);


			if (team == null)
				throw new PermissionsException();


			if (!team.Secret && team.Organization.Id == caller.Organization.Id)//&& team.Members.Any(x => x.UserOrganization.Organization.Id == caller.Organization.Id))
				return this;


			if (team.Secret && (team.CreatedBy == caller.Id || team.ManagedBy == caller.Id))
				return this;

			var members = session.QueryOver<TeamDurationModel>().Where(x => x.TeamId == teamId && x.UserId == caller.Id).List().ToList();
			if (team.Secret && members.Any())
				return this;

			//if (team.Secret && IsOwnedBelowOrEqual(caller, x => (team.CreatedBy == x.Id || team.ManagedBy == x.Id)))
			//   return this;


			throw new PermissionsException();
		}

		public PermissionsUtility EditL10Note(long id) {
			if (IsRadialAdmin(caller))
				return this;
			var note = session.Get<L10Note>(id);
			return EditL10Recurrence(note.Recurrence.Id);
		}

		public PermissionsUtility EditTeam(long teamId) {
			if (IsRadialAdmin(caller))
				return this;

			//Creating
			if (teamId == 0 && caller.IsManager())
				return this;

			//if (teamId == -5 && caller.IsManager()) // Subordinates Team
			//    return this;

			var team = session.Get<OrganizationTeamModel>(teamId);
			if (IsManagingOrganization(team.Organization.Id, true))
				return this;

			if (team.Type != TeamType.Standard)
				throw new PermissionsException("Cannot edit auto-populated team.");

			if (caller.IsManager() || !team.OnlyManagersEdit) {
				if (team.Organization.Id == caller.Organization.Id) {
					if (!team.Secret)// && team.Members.Any(x => x.UserOrganization.Organization.Id == caller.Organization.Id))
						return this;


					if (team.Secret && (team.CreatedBy == caller.Id || team.ManagedBy == caller.Id))
						return this;

					if (!team.OnlyManagersEdit) {
						var members = session.QueryOver<TeamDurationModel>().Where(x => x.TeamId == teamId && x.UserId == caller.Id).List().ToList();
						if (team.Secret && members.Any())
							return this;
					}
					/*return this;*/
				}
			}

			throw new PermissionsException();
		}

		public PermissionsUtility IssueForTeam(long forTeamId) {
			return TryWithOverrides(p => {
				return ManagingTeam(forTeamId);
			}, PermissionType.IssueReview);
		}

		public PermissionsUtility ManagingTeam(long teamId) {
			if (IsRadialAdmin(caller))
				return this;

			//if (teamId == -5 && caller.IsManager())
			//    return this;
			var team = session.Get<OrganizationTeamModel>(teamId);

			if (IsManagingOrganization(team.Organization.Id, true))
				return this;

			if (team.OnlyManagersEdit && team.ManagedBy == caller.Id)
				return this;

			var members = session.QueryOver<TeamDurationModel>().Where(x => x.Team.Id == teamId).List().ToListAlive();

			if (!team.OnlyManagersEdit && members.Any(x => x.User.Id == caller.Id))
				return this;

			throw new PermissionsException();
		}
		#endregion

		#region Review
		public PermissionsUtility AdminReviewContainer(long reviewContainerId) {
			//TODO more permissions here?
			if (IsRadialAdmin(caller))
				return this;

			var review = session.Get<ReviewsModel>(reviewContainerId);
			if (review.CreatedById == caller.Id)
				return this;

			var team = session.Get<OrganizationTeamModel>(review.ForTeamId);
			if (team.ManagedBy == caller.Id)
				return this;

			ManagingOrganization(caller.Organization.Id);

			return this;

			//ManagerAtOrganization(caller.Id, caller.Organization.Id);
			//return this;
		}

		public PermissionsUtility EditReview(long reviewId) {
			return CheckCacheFirst("EditReview", reviewId).Execute(() => {
				//TODO more permissions here?
				if (IsRadialAdmin(caller))
					return this;
				var review = session.Get<ReviewModel>(reviewId);
				if (review.DueDate < DateTime.UtcNow)
					throw new PermissionsException("Review period has expired.");
				if (review.ReviewerUserId == caller.Id)
					return this;

				throw new PermissionsException();
			});
		}

		public PermissionsUtility ViewReviews(long reviewContainerId, bool sensitive) {
			if (IsRadialAdmin(caller))
				return this;
			var review = session.Get<ReviewsModel>(reviewContainerId);
			var orgId = review.Organization.Id;
			if (sensitive)
				ManagerAtOrganization(caller.Id, orgId);
			if (orgId == caller.Organization.Id)
				return this;



			/*
            if(IsOwnedBelowOrEqual(caller,x=>x.Id==review.CreatedById))
                return this;*/

			throw new PermissionsException();
		}

		public PermissionsUtility ViewReview(long reviewId) {
			return TryWithOverrides(y => {
				if (IsRadialAdmin(caller))
					return this;

				var review = session.Get<ReviewModel>(reviewId);
				var reviewUserId = review.ReviewerUserId;

				//Is this correct?
				if (IsManagingOrganization(review.ReviewerUser.Organization.Id))
					return this;

				//Cannot be viewed by the user
				if (reviewUserId == caller.Id)
					return this;

				if (DeepAccessor.Users.ManagesUser(session, this, caller.Id, reviewUserId))
					return this;

				//if (IsOwnedBelowOrEqual(caller, x => x.Id == reviewUserId))
				//	return this;

				throw new PermissionsException();
			}, PermissionType.ViewReviews);

		}

		public PermissionsUtility ManageUserReview(long reviewId, bool userCanManageOwnReview) {
			ViewReview(reviewId);
			var review = session.Get<ReviewModel>(reviewId);
			var userId = review.ReviewerUserId;

			if (userCanManageOwnReview && review.ReviewerUserId == caller.Id)
				return this;

			return ManagesUserOrganization(userId, false);
		}
		public PermissionsUtility ManageUserReview_Answer(long answerId, bool userCanManageOwnReview) {
			var answer = session.Get<AnswerModel>(answerId);

			if (answer == null)
				throw new PermissionsException("Answer does not exist");
			var reviews = session.QueryOver<ReviewModel>()
				.Where(x => x.DeleteTime == null && x.ForReviewContainerId == answer.ForReviewContainerId && x.ReviewerUserId == answer.RevieweeUserId)
				.List();
			if (!reviews.Any())
				throw new PermissionsException("Review does not exist");

			foreach (var review in reviews) {
				ManageUserReview(review.Id, userCanManageOwnReview);
			}


			return this;
		}
		#endregion

		#region Responsbility
		public PermissionsUtility EditResponsibility(long responsibilityId) {
			if (IsRadialAdmin(caller))
				return this;

			var r = session.Get<ResponsibilityModel>(responsibilityId);
			var rGroupId = r.ForResponsibilityGroup;
			ResponsibilityGroupModel rGroup = session.Get<ResponsibilityGroupModel>(rGroupId);

			if (rGroup is OrganizationModel) {
				return EditOrganization(rGroupId);
			} else if (rGroup is OrganizationTeamModel) {
				return EditTeam(rGroupId);
			} else if (rGroup is UserOrganizationModel) {
				return EditUserOrganization(rGroupId);
			} else {
				throw new PermissionsException("Unknown responsibility group type.");
			}
		}

		public PermissionsUtility ViewRGM(long id) {
			var rgm = session.Get<ResponsibilityGroupModel>(id);

			if (rgm is OrganizationModel) {
				return ViewOrganization(rgm.Id);
			} else if (rgm is OrganizationTeamModel) {
				return ViewTeam(rgm.Id);
			} else if (rgm is UserOrganizationModel) {
				return ViewUserOrganization(rgm.Id, false);
			} else if (rgm is OrganizationPositionModel) {
				return ViewOrganizationPosition(rgm.Id);
			}

			throw new PermissionsException("Unknown responsibility group type.");
		}

		#endregion

		#region Position
		public PermissionsUtility ManagingPosition(long positionId) {
			if (IsRadialAdmin(caller))
				return this;

			if (positionId == 0)
				return this;

			var position = session.Get<OrganizationPositionModel>(positionId);

			if (IsManagingOrganization(position.Organization.Id, true))
				return this;

			if (caller.Organization.ManagersCanEditPositions && caller.ManagerAtOrganization && position.Organization.Id == caller.Organization.Id)
				return this;

			throw new PermissionsException();
		}
		public PermissionsUtility EditPositions(long organizationId) {
			if (IsRadialAdmin(caller))
				return this;

			var org = session.Get<OrganizationModel>(organizationId);

			if (IsManagingOrganization(organizationId, org.ManagersCanEditPositions))
				return this;

			if (caller.Organization.ManagersCanEditPositions && caller.ManagerAtOrganization)
				return this;

			throw new PermissionsException();
		}

		#endregion

		#region Templates
		public PermissionsUtility CreateTemplates(long organizationId) {
			return ManagerAtOrganization(caller.Id, organizationId);
		}
		public PermissionsUtility ViewTemplate(long templateId) {
			return ViewOrganization(session.Get<UserTemplate>(templateId).OrganizationId);
		}
		public PermissionsUtility EditTemplate(long templateId) {
			return CreateTemplates(session.Get<UserTemplate>(templateId).OrganizationId);
		}
		#endregion

		#region Prereview
		public PermissionsUtility ViewPrereview(long prereviewId) {
			if (IsRadialAdmin(caller))
				return this;

			var prereview = session.Get<PrereviewModel>(prereviewId);
			var prereviewOrgId = session.Get<ReviewsModel>(prereview.ReviewContainerId).OrganizationId;

			if (IsManagingOrganization(prereviewOrgId))
				return this;

			if (IsOwnedBelowOrEqual(caller, prereview.ManagerId))
				return this;

			throw new PermissionsException();
		}
		#endregion

		#region Scorecard

		public PermissionsUtility EditUserScorecard(long userId) {
			return EditUserOrganization(userId);
		}

		public PermissionsUtility ViewOrganizationScorecard(long organizationId) {
			if (IsRadialAdmin(caller))
				return this;

			if (IsManagingOrganization(organizationId))
				return this;

			var organization = session.Get<OrganizationModel>(organizationId);
			if (organization.Settings.EmployeesCanViewScorecard && caller.Organization.Id == organizationId)
				return this;
			if (organization.Settings.ManagersCanViewScorecard && IsManager(organizationId))
				return this;

			throw new PermissionsException();
		}

		public PermissionsUtility EditAttach(Attach attachTo) {


			if (IsRadialAdmin(caller))
				return this;

			var orgId = AttachAccessor.GetOrganizationId(session, attachTo);


			ViewOrganization(orgId);

			if (IsManagingOrganization(orgId))
				return this;

			switch (attachTo.Type) {
				case AttachType.Position:
					return EditPositions(orgId);
				case AttachType.Team:
					return EditTeam(attachTo.Id);
				case AttachType.User:
					return EditUserOrganization(attachTo.Id);
				default:
					throw new PermissionsException("Invalid attach type (" + attachTo.Type + ")");
			}


		}
		public PermissionsUtility ViewMeasurable(long measurableId) {
			return CheckCacheFirst("ViewMeasurable", measurableId).Execute(() => {
				if (IsRadialAdmin(caller))
					return this;

				var m = session.Get<MeasurableModel>(measurableId);
				if (IsManagingOrganization(m.OrganizationId))
					return this;
				if (m.AccountableUserId == caller.Id)
					return this;
				if (m.AdminUserId == caller.Id)
					return this;
				try {
					ManagesUserOrganization(m.AccountableUserId, false);
					return this;
				} catch (PermissionsException) {
				}


				var measurableRecurs = session.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
						.Where(x => x.DeleteTime == null && x.Measurable.Id == measurableId)
						.Select(x => x.L10Recurrence.Id)
						.List<long>().ToList();

				foreach (var recur in measurableRecurs) {
					try {
						ViewL10Recurrence(recur);
						return this;
					} catch (PermissionsException) {
					}
				}
				try {
					CanAdminMeetingWithUser(m.AccountableUserId, onError: new PermissionsException().Message);
					return this;
				} catch (PermissionsException) {
				}

				throw new PermissionsException("Cannot view measurable");
			});
		}
		public PermissionsUtility EditMeasurable(long measurableId) {
			return CheckCacheFirst("EditMeasurable", measurableId).Execute(() => {
				if (IsRadialAdmin(caller))
					return this;

				var m = session.Get<MeasurableModel>(measurableId);

				if (m.AccountableUserId == caller.Id)
					return this;
				if (m.AdminUserId == caller.Id)
					return this;

				if (IsManagingOrganization(m.OrganizationId))
					return this;

				var measurableRecurs = session.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
						.Where(x => x.DeleteTime == null && x.Measurable.Id == measurableId)
						.Select(x => x.L10Recurrence.Id)
						.List<long>().ToList();

				foreach (var recur in measurableRecurs) {
					try {
						EditL10Recurrence(recur);
						return this;
					} catch (PermissionsException) {
					}
				}

				throw new PermissionsException();
			});
		}

		public PermissionsUtility EditScore(long scoreId) {
			var score = session.Get<ScoreModel>(scoreId);

			return EditMeasurable(score.MeasurableId);


			//if (IsRadialAdmin(caller))
			//    return this;

			//var score = session.Get<ScoreModel>(scoreId);
			//if (IsManagingOrganization(score.OrganizationId))
			//    return this;

			//if (score.AccountableUserId == caller.Id)
			//    return this;
			//if (score.Measurable.AdminUserId == caller.Id)
			//    return this;


			//var possibleRecurrences = session.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
			//            .Where(x => x.DeleteTime == null && x.Measurable.Id == score.MeasurableId)
			//            .Select(x => x.L10Recurrence.Id)
			//            .List<long>().ToList();

			//foreach (var p in possibleRecurrences) {
			//    try {
			//        return EditL10Recurrence(p);
			//    } catch (PermissionsException) {
			//        //try next one..
			//    }
			//}
			//throw new PermissionsException();
		}

		public PermissionsUtility CanViewUserMeasurables(long userId) {
			return CanViewUserRocks(userId);
		}

		public PermissionsUtility CreateMeasurableForUser(long userId, long? recurrenceId = null) {
			return Or(x => x.EditUserDetails(userId), x => x.CanAdminMeetingWithUser(userId, recurrenceId, "Cannot create measurable"));
		}

		#endregion

		#region VTO
		public PermissionsUtility CreateVTO(long organizationId) {
			if (IsRadialAdmin(caller))
				return this;

			if (IsManager(organizationId))
				return this;
			throw new PermissionsException("Cannot create a VTO");
		}

		public PermissionsUtility ViewVTO(long vtoId) {
			if (IsRadialAdmin(caller))
				return this;
			var vto = session.Get<VtoModel>(vtoId);
			if (vto.L10Recurrence.HasValue && vto.L10Recurrence.Value > 0) {
				var l10 = session.Get<L10Recurrence>(vto.L10Recurrence.Value);
				if (l10.ShareVto)
					return Or(() => ViewOrganization(l10.OrganizationId).ViewOrganization(vto.Organization.Id), () => ViewL10Recurrence(vto.L10Recurrence.Value));
				return ViewL10Recurrence(vto.L10Recurrence.Value);
				//return CanView(PermItem.ResourceType.L10Recurrence, vto.L10Recurrence.Value);
			} else {
				return CanView(PermItem.ResourceType.VTO, vtoId, @this => {
					if (IsManagingOrganization(vto.Organization.Id))
						return this;
					if (vto.L10Recurrence != null)
						return @this.ViewL10Recurrence(vto.L10Recurrence.Value);
					throw new PermissionsException("Cannot view V/TO");
				});
			}

		}
		public PermissionsUtility EditVTO(long vtoId) {
			if (IsRadialAdmin(caller))
				return this;

			return CanEdit(PermItem.ResourceType.VTO, vtoId, @this => {
				var vto = session.Get<VtoModel>(vtoId);
				if (IsManagingOrganization(vto.Organization.Id))
					return this;
				if (vto.L10Recurrence != null)
					return @this.EditL10Recurrence(vto.L10Recurrence.Value);

				throw new PermissionsException("Cannot edit V/TO");
			});


		}
		#endregion

		#region L10
		/// <summary>
		/// RecurrenceId is there for speedup.
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="optionalRecurrenceId"></param>
		/// <param name="onError"></param>
		/// <returns></returns>
		private PermissionsUtility CanAdminMeetingWithUser(long userId, long? optionalRecurrenceId = null, string onError = "Cannot create. User is not an attendee of the meeting.") {
			if (IsRadialAdmin(caller))
				return this;

			var owner = session.Get<UserOrganizationModel>(userId);
			if (owner.Organization.Settings.EmployeesCanEditSelf && IsSelf(userId))
				return this;

			try {
				return EditUserDetails(userId);
			} catch (Exception) {
			}

			if (optionalRecurrenceId == null) {
				//Get caller's Editable meetings
				var adminRecurrenceIds = GetAdminMeetingForUser(caller.Id);
				//Get Attendees in that meeting.
				var inAnyAdminMeetings = session.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
					.Where(x => x.DeleteTime == null && x.User.Id == userId)
					.WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(adminRecurrenceIds.ToList())
					.RowCount();
				//is user id an attendee?
				if (inAnyAdminMeetings > 0)
					return this;
			} else {
				CanAdmin(PermItem.ResourceType.L10Recurrence, optionalRecurrenceId.Value);
				var inMeeting = session.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
					.Where(x => x.DeleteTime == null && x.User.Id == userId && x.L10Recurrence.Id == optionalRecurrenceId.Value)
					.RowCount();
				if (inMeeting > 0)
					return this;
			}

			throw new PermissionsException(onError, true);
		}

		public PermissionsUtility CreateL10Recurrence(long organizationId) {
			if (IsRadialAdmin(caller))
				return this;

			var organization = session.Get<OrganizationModel>(organizationId);
			if (IsManagingOrganization(organizationId))
				return this;
			if (organization.Settings.EmployeeCanCreateL10 && caller.Organization.Id == organizationId)
				return this;
			if (organization.Settings.ManagersCanCreateL10 && IsManager(organizationId))
				return this;
			throw new PermissionsException("Cannot create meeting.");
		}

		public PermissionsUtility AdminL10Recurrence(long recurrenceId) {
			return TryWithAllUsers(p => {
				if (IsRadialAdmin(caller))
					return this;

				if (recurrenceId == 0) {
					throw new PermissionsException("Meeting does not exist.");
				}

				return CanAdmin(PermItem.ResourceType.L10Recurrence, recurrenceId);
			});
		}

		public PermissionsUtility EditL10Recurrence(long recurrenceId) {
			return TryWithAllUsers(p => {
				return CheckCacheFirst("EditL10Recurrence", recurrenceId).Execute(() => {
					if (IsRadialAdmin(caller))
						return this;

					if (recurrenceId == 0) {
						throw new PermissionsException("Meeting does not exist.");
					} else {
						var recur = session.Get<L10Recurrence>(recurrenceId);

						return CanEdit(PermItem.ResourceType.L10Recurrence, recurrenceId, (@this) => {
							var availUserIds = new[] { caller.Id };

							if (recur.CreatedById == caller.Id)
								return this;

							if (IsManagingOrganization(recur.OrganizationId))
								return this;

							if (caller.Organization.Settings.ManagersCanEditSubordinateL10) {
								availUserIds = DeepAccessor.Users.GetSubordinatesAndSelf(session, caller, caller.Id).ToArray();
							}

							var exists = session.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
								.Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
								.WhereRestrictionOn(x => x.User.Id).IsIn(availUserIds)
								.RowCount();
							if (exists > 0)
								return @this;
							throw new PermissionsException();
						});
					}
				});
			});
		}

		public PermissionsUtility EditIssueRecurrence(long issueRecurrenceId) {
			var issueRecurrence = session.Get<IssueModel.IssueModel_Recurrence>(issueRecurrenceId);
			return EditIssue(issueRecurrence.Issue.Id);
		}

		public PermissionsUtility EditIssue(long issueId) {
			if (IsRadialAdmin(caller))
				return this;

			var possibleRecurrences = session.QueryOver<IssueModel.IssueModel_Recurrence>()
				.Where(x => x.DeleteTime == null && x.Issue.Id == issueId)
				.Select(x => x.Recurrence.Id).List<long>()
				.ToList();

			foreach (var p in possibleRecurrences) {
				try {
					return EditL10Recurrence(p);
				} catch (PermissionsException) {
					//try next one..
				}
			}
			throw new PermissionsException();
		}

		public PermissionsUtility ViewIssue(long issueId) {
			if (IsRadialAdmin(caller))
				return this;

			var possibleRecurrences = session.QueryOver<IssueModel.IssueModel_Recurrence>()
				.Where(x => x.DeleteTime == null && x.Issue.Id == issueId)
				.Select(x => x.Recurrence.Id).List<long>()
				.ToList();

			foreach (var p in possibleRecurrences) {
				try {
					return ViewL10Recurrence(p);
				} catch (PermissionsException) {
					//try next one..
				}
			}
			throw new PermissionsException();
		}

		public PermissionsUtility ViewL10Recurrence(long recurrenceId) {
			return TryWithAllUsers(p => {
				if (IsRadialAdmin(caller))
					return this;

				var recurrences_OrgId = session.Get<L10Recurrence>(recurrenceId).OrganizationId;

				// if (IsManagingOrganization(recurrences_OrgId))
				//     return this;

				return CanView(PermItem.ResourceType.L10Recurrence, recurrenceId, (@this) => {
					var possibleUsers = session.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
				.Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
				.Select(x => x.User.Id).List<long>()
				.ToList();

					if (possibleUsers.Contains(caller.Id))
						return @this;

					if (caller.Organization.Settings.ManagersCanViewSubordinateL10) {
						var subIds = DeepAccessor.Users.GetSubordinatesAndSelf(session, caller, caller.Id);
						if (possibleUsers.ContainsAny(subIds))
							return @this;
					}
					throw new PermissionsException();
				});
			});
		}

		private IEnumerable<long> GetEditableMeetingForCaller() {
			return GetEditableMeetingForUser(caller.Id);
		}
		private IEnumerable<long> GetEditableMeetingForUser(long userId) {
			return GetAllPermItemsForUser(PermItem.ResourceType.L10Recurrence, userId).Where(x => x.CanEdit).Select(x => x.ResId);
		}
		private IEnumerable<long> GetAdminMeetingForCaller() {
			return GetAdminMeetingForUser(caller.Id);
		}
		private IEnumerable<long> GetAdminMeetingForUser(long userId) {
			return GetAllPermItemsForUser(PermItem.ResourceType.L10Recurrence, userId).Where(x => x.CanAdmin).Select(x => x.ResId);
		}

		public PermissionsUtility AssignTodo(long userId, long? recurrenceId) {
			if (userId <= 0)
				throw new PermissionsException("Invalid UserId");

			if (IsRadialAdmin(caller))
				return this;

			if (recurrenceId == null) {
				if (userId == caller.Id)
					return this;
				try {
					return ManagesUserOrganization(userId, false);
				} catch (Exception e) {
				}
			}

			ViewUserOrganization(userId, false);
			if (recurrenceId != null) {
				var isAttendee = session.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.L10Recurrence.Id == recurrenceId && x.DeleteTime == null && x.User.Id == userId).RowCount() > 0;
				if (isAttendee && (IsPermitted((x) => x.CanEdit(PermItem.ResourceType.L10Recurrence, recurrenceId.Value)) || IsPermitted((x) => x.CanAdmin(PermItem.ResourceType.L10Recurrence, recurrenceId.Value)))) {
					return this;
				}
			}


			throw new PermissionsException("Cannot assign this user a to-do");
		}

		//public PermissionsUtility AssignTodoTo(long userId, long? todoId = null) {
		//    if (userId <= 0)
		//        throw new PermissionsException("UserId was negative");

		//    if (IsRadialAdmin(caller))
		//        return this;
		//    if (userId == caller.Id)
		//        return this;
		//    var user = session.Get<UserOrganizationModel>(userId);

		//    var toCheck = new List<Func<PermissionsUtility>> {
		//        ()=>ManagingOrganization(user.Organization.Id),
		//        ()=>ManagesUserOrganization(userId, false, PermissionType.EditEmployeeDetails),
		//    };

		//    if (todoId != null) {
		//        var todo = session.Get<TodoModel>(todoId.Value);
		//        if (todo.ForRecurrenceId != null && todo.ForRecurrenceId != 0) {
		//            toCheck.Insert(0, () => EditL10Recurrence(todo.ForRecurrenceId.Value));
		//        }
		//    }

		//    return ViewUserOrganization(userId, false, PermissionType.EditEmployeeDetails).Or(toCheck.ToArray());
		//}


		public PermissionsUtility EditTodo(long todoId) {
			if (IsRadialAdmin(caller))
				return this;
			var todo = session.Get<TodoModel>(todoId);

			if (IsSelf(todo.AccountableUserId))
				return this;

			if (todo.ForRecurrenceId != null && todo.ForRecurrenceId != 0) {
				try {
					EditL10Recurrence(todo.ForRecurrenceId.Value);
					return this;
				} catch (PermissionsException) {
				}
			} else {
				if (IsManagingOrganization(todo.OrganizationId))
					return this;
			}
			throw new PermissionsException();
		}

		public PermissionsUtility ViewTodo(long todoId) {
			if (IsRadialAdmin(caller))
				return this;
			var todo = session.Get<TodoModel>(todoId);

			if (IsSelf(todo.AccountableUserId))
				return this;

			if (todo.ForRecurrenceId != null && todo.ForRecurrenceId != 0) {
				try {
					ViewL10Recurrence(todo.ForRecurrenceId.Value);
					return this;
				} catch (PermissionsException) {
				}
			} else {
				if (IsManagingOrganization(todo.OrganizationId))
					return this;
			}
			throw new PermissionsException();
		}

		[Obsolete("Avoid using")]
		public PermissionsUtility ViewUsersL10Meetings(long userId) {
			if (IsRadialAdmin(caller))
				return this;

			if (caller.Id == userId)
				return this;

			var users_OrgId = session.Get<UserOrganizationModel>(userId).Organization.Id;
			if (IsManagingOrganization(users_OrgId))
				return this;

			if (caller.Organization.Settings.ManagersCanViewSubordinateL10) {
				var subIds = DeepAccessor.Users.GetSubordinatesAndSelf(session, caller, caller.Id);
				if (subIds.Contains(userId))
					return this;
			}

			throw new PermissionsException();
		}

		public PermissionsUtility ViewL10Meeting(long meetingId) {
			return CheckCacheFirst("ViewL10Meeting", meetingId).Execute(() => {
				if (IsRadialAdmin(caller))
					return this;

				var meeting = session.Get<L10Meeting>(meetingId);
				var meeting_OrgId = meeting.OrganizationId;
				//if (IsManagingOrganization(meeting_OrgId))
				//	return this;

				return CanView(PermItem.ResourceType.L10Recurrence, meeting.L10RecurrenceId, (@this) => {
					var meetingIds = session.QueryOver<L10Meeting.L10Meeting_Attendee>().Where(x =>
						x.L10Meeting.Id == meetingId &&
						x.DeleteTime == null).List().Select(x => x.UserId).ToList();
					if (caller.UserIds.ContainsAny(meetingIds))
						return @this;
					if (caller.Organization.Settings.ManagersCanViewSubordinateL10) {
						var subIds = DeepAccessor.Users.GetSubordinatesAndSelf(session, caller, caller.Id);
						if (subIds.ContainsAny(meetingIds))
							return @this;
					}

					var recurId = meeting.L10RecurrenceId;
					var defaultIds = session.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x =>
						x.L10Recurrence.Id == recurId &&
						x.DeleteTime == null).List().Select(x => x.User.Id).ToList();

					if (caller.UserIds.ContainsAny(defaultIds))
						return @this;
					if (caller.Organization.Settings.ManagersCanViewSubordinateL10) {
						var subIds = DeepAccessor.Users.GetSubordinatesAndSelf(session, caller, caller.Id);
						if (subIds.ContainsAny(defaultIds))
							return @this;
					}

					throw new PermissionsException();
				});
			});
		}
		public PermissionsUtility ViewL10Note(long noteId) {
			var note = session.Get<L10Note>(noteId);
			if (note == null)
				throw new PermissionsException("Note does not exist");

			return ViewL10Recurrence(note.Recurrence.Id);
		}
		#endregion

		#region Headlines

		public PermissionsUtility ViewHeadline(long headlineId) {
			var h = session.Get<PeopleHeadline>(headlineId);
			return ViewL10Recurrence(h.RecurrenceId);
		}
		public PermissionsUtility EditHeadline(long headlineId) {
			var h = session.Get<PeopleHeadline>(headlineId);
			return EditL10Recurrence(h.RecurrenceId);
		}
		#endregion

		#region Rocks
		public PermissionsUtility ViewRock(long rockId) {
			var rock = session.Get<RockModel>(rockId);
			return ViewUserOrganization(rock.ForUserId, false);
		}
		public PermissionsUtility EditRock(long rockId) {
			return CheckCacheFirst("EditRock", rockId).Execute(() => {

				if (IsRadialAdmin(caller))
					return this;

				var rock = session.Get<RockModel>(rockId);

				var recurrenceIds = session.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
					.Where(x => x.DeleteTime == null && x.ForRock.Id == rock.Id)
					.Select(x => x.L10Recurrence.Id).List<long>();

				var acceptedRecurrenceIds = recurrenceIds.Where(rid => {
					try {
						EditL10Recurrence(rid);
						return true;
					} catch (Exception) {
						return false;
					}
				});

				if (acceptedRecurrenceIds.Any())
					return this;

				if (rock.OrganizationId != caller.Organization.Id)
					throw new PermissionsException() { NoErrorReport = true };


				if (caller.Organization.Settings.EmployeesCanEditSelf && rock.ForUserId == caller.Id)
					return this;

				var editSelf = caller.Organization.Settings.ManagersCanEditSelf;
				return ManagesUserOrganization(rock.ForUserId, !editSelf, PermissionType.EditEmployeeDetails);
			});
		}


		public PermissionsUtility EditRock_UnArchive(long rockId) {
			return CheckCacheFirst("EditRock", rockId).Execute(() => {

				if (IsRadialAdmin(caller))
					return this;

				var rock = session.Get<RockModel>(rockId);

				var recurrenceIds = session.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
					.Where(x => x.DeleteTime != null && x.ForRock.Id == rock.Id)
					.Select(x => x.L10Recurrence.Id).List<long>();

				var acceptedRecurrenceIds = recurrenceIds.Where(rid => {
					try {
						EditL10Recurrence(rid);
						return true;
					} catch (Exception) {
						return false;
					}
				});

				if (acceptedRecurrenceIds.Any())
					return this;

				if (rock.OrganizationId != caller.Organization.Id)
					throw new PermissionsException() { NoErrorReport = true };


				if (caller.Organization.Settings.EmployeesCanEditSelf && rock.ForUserId == caller.Id)
					return this;

				var editSelf = caller.Organization.Settings.ManagersCanEditSelf;
				return ManagesUserOrganization(rock.ForUserId, !editSelf, PermissionType.EditEmployeeDetails);
			});
		}

		public PermissionsUtility EditMilestone(long milestoneId) {
			var rockId = session.Get<Milestone>(milestoneId).RockId;
			return EditRock(rockId);
		}

		public PermissionsUtility CanAdminMeetingItemsForUser(long userId, long recurrenceId) {
			if (IsRadialAdmin(caller))
				return this;

			var user = session.Get<UserOrganizationModel>(userId);

			ViewUserOrganization(userId, false);

			var obj = session.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x =>
			x.DeleteTime == null
			&& x.L10Recurrence.Id == recurrenceId
			&& x.User.Id == userId).Take(1).SingleOrDefault();

			if (obj == null) {
				throw new PermissionsException("User is not attendee.");
			}

			CanAdmin(PermItem.ResourceType.L10Recurrence, recurrenceId);

			var canEditSelf = user.Organization.Settings.EmployeesCanEditSelf
				|| (user.IsManager() && user.Organization.Settings.ManagersCanEditSelf);

			if (IsPermitted(x => x.ManagesUserOrganization(userId, !canEditSelf))) {
				return this;
			}

			if (!user.Organization.Settings.OnlySeeRocksAndScorecardBelowYou) {
				return this;
			}

			throw new PermissionsException("You do not manage this user.") { NoErrorReport = true };
		}

		public PermissionsUtility CanViewUserRocks(long userId) {
			if (IsRadialAdmin(caller))
				return this;

			var user = session.Get<UserOrganizationModel>(userId);

			if (caller.Id == userId)
				return this;

			if (IsManagingOrganization(user.Organization.Id))
				return this;

			if (IsManager(user.Organization.Id) && !user.Organization.Settings.OnlySeeRocksAndScorecardBelowYou)
				return this;

			if (user.Organization.Settings.OnlySeeRocksAndScorecardBelowYou)
				return ManagesUserOrganizationOrSelf(userId);

			return CanAdminMeetingWithUser(userId, onError: new PermissionsException().Message);


			//throw new PermissionsException();
		}

		public PermissionsUtility CreateRocksForUser(long userId, long? recurrenceId = null) {

			return CanAdminMeetingWithUser(userId, recurrenceId, "Cannot create rock");
			//if (IsRadialAdmin(caller))
			//	return this;

			//var owner = session.Get<UserOrganizationModel>(userId);
			//if (owner.Organization.Settings.EmployeesCanEditSelf && IsSelf(userId))
			//	return this;

			//try {
			//	return EditUserDetails(userId);
			//} catch (Exception) {
			//}

			////Get caller's Editable meetings
			//var adminRecurrenceIds = GetAdminMeetingForUser(caller.Id);
			////Get Attendees in that meeting.
			//var inAnyAdminMeetings = session.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
			//	.Where(x => x.DeleteTime == null && x.User.Id == userId)
			//	.WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(adminRecurrenceIds.ToList())
			//	.RowCount();
			////is user id an attendee?
			//if (inAnyAdminMeetings > 0)
			//	return this;

			//throw new PermissionsException("Cannot create rock",true);
		}


		#endregion

		#region NewSurvey

		public PermissionsUtility CreateQuarterlyConversation(long orgId) {
			return ManagerAtOrganization(caller.Id, orgId);
		}
		public PermissionsUtility ViewSurvey(long surveyId) {
			if (IsRadialAdmin(caller))
				return this;
			var survey = session.Get<Survey>(surveyId);
			ViewSurveyContainer(survey.SurveyContainerId);
			return CanView(PermItem.ResourceType.Survey, surveyId);
		}
		public PermissionsUtility ViewSurveyResultsAbout(IForModel about) {
			var orgId = ForModelAccessor.GetOrganizationId(session, about);
			var allowSelf = (IsManager(orgId));
			return ManagesForModel(about, !allowSelf);
		}

		public PermissionsUtility ViewSurveyContainer(long surveyContainerId) {
			if (IsRadialAdmin(caller))
				return this;
			return CanView(PermItem.ResourceType.SurveyContainer, surveyContainerId);
		}
		//public PermissionsUtility EditSurveyContainer(long surveyContainerId) {
		//	if (IsRadialAdmin(caller))
		//		return this;
		//	var container = session.Get<SurveyContainer>(surveyContainerId);
		//	container
		//	return CanView(PermItem.ResourceType.SurveyContainer, surveyContainerId);
		//}

		public PermissionsUtility EditSurveyResponse(long surveyResponseId) {
			if (IsRadialAdmin(caller))
				return this;

			var response = session.Get<SurveyResponse>(surveyResponseId);

			if (response == null || response.DeleteTime != null)
				throw new PermissionsException("Response does not exist.");

			if (response.By.Is<UserOrganizationModel>()) {
				if (caller.Id == response.By.ModelId)
					return this;
				else
					throw new PermissionsException("Cannot edit this response");
			}
			throw new PermissionsException("Unknown 'by' type");
		}

		#endregion

		#region OldSurvey
		public PermissionsUtility ViewOldSurveyContainer(long surveyId) {

			if (IsRadialAdmin(caller))
				return this;

			var survey = session.Get<SurveyContainerModel>(surveyId);

			if (IsManagingOrganization(survey.OrgId))
				return this;
			if (survey.CreatorId == caller.Id)
				return this;
			throw new PermissionsException("Cannot view this survey");
		}

		public PermissionsUtility CreateOldSurvey() {
			if (IsManagingOrganization(caller.Organization.Id)) {
				return this;
			}

			if (caller.Organization.Settings.EmployeesCanCreateSurvey)
				return this;

			if (caller.Organization.Settings.ManagersCanCreateSurvey && IsManager(caller.Organization.Id)) {
				return this;
			}

			throw new PermissionsException("Cannot create survey");

		}


		public PermissionsUtility EditOldSurvey(long surveyId) {
			var survey = session.Get<SurveyContainerModel>(surveyId);

			if (survey != null) {
				session.Evict(survey);
				if (survey.QuestionGroup != null)
					session.Evict(survey.QuestionGroup);
				if (survey.RespondentGroup != null)
					session.Evict(survey.RespondentGroup);
			}
			if (surveyId == 0)
				return CreateOldSurvey();



			if (IsRadialAdmin(caller))
				return this;

			if (survey.IssueDate != null)
				throw new PermissionsException("Cannot edit survey.");

			if (IsManagingOrganization(survey.OrgId))
				return this;
			if (survey.CreatorId == caller.Id)
				return this;
			throw new PermissionsException("Cannot view this survey");
		}
		#endregion

		#region PermissionOverride
		public PermissionsUtility EditPermissionOverride(long permissionOverrideId) {
			if (IsRadialAdmin(caller))
				return this;

			if (caller.ManagingOrganization && permissionOverrideId == 0)
				return this;

			var p = session.Get<PermissionOverride>(permissionOverrideId);

			if (IsManagingOrganization(p.Organization.Id, true))
				return this;

			throw new PermissionsException("Cannot edit this permission override.");

		}
		#endregion

		#region ForModel
		public PermissionsUtility EditForModel(ForModel model) {
			if (model.ModelType == ForModel.GetModelType<L10Recurrence>())
				return EditL10Recurrence(model.ModelId);
			if (model.ModelType == ForModel.GetModelType<UserOrganizationModel>())
				return EditUserOrganization(model.ModelId);
			throw new PermissionsException("ModelType unhandled");
		}
		public PermissionsUtility ViewForModel(ForModel model) {
			if (model.ModelType == ForModel.GetModelType<L10Recurrence>())
				return ViewL10Recurrence(model.ModelId);
			if (model.ModelType == ForModel.GetModelType<UserOrganizationModel>())
				return ViewUserOrganization(model.ModelId, false);
			if (model.ModelType == ForModel.GetModelType<OrganizationModel>())
				return ViewOrganization(model.ModelId);
			throw new PermissionsException("ModelType unhandled");
		}
		#endregion

		#region Recursions
		//public PermissionsUtility OwnedBelowOrEqual(Predicate<UserOrganizationModel> visiblility) {
		//	if (IsOwnedBelowOrEqual(caller, visiblility))
		//		return this;
		//	throw new PermissionsException();
		//}

		public PermissionsUtility OwnedBelowOrEqual(long userId) {
			if (IsOwnedBelowOrEqual(caller, userId))
				return this;
			throw new PermissionsException();
		}

		protected bool IsOwnedBelowOrEqual(UserOrganizationModel caller, long userId) {
			if (userId == caller.Id)
				return true;

			return DeepAccessor.Users.ManagesUser(session, this, caller.Id, userId);
		}

		//protected bool IsOwnedBelowOrEqual(UserOrganizationModel caller, Predicate<UserOrganizationModel> visibility) {

		//	var allSubs = DeepAccessor.Users.GetSubordinatesAndSelfModels(caller, caller.Id);

		//	return allSubs.Any(x => visibility(x));


		//	//if (visibility(caller))
		//	//	return true;
		//	//foreach (var manager in caller.ManagingUsers.ToListAlive().Select(x => x.Subordinate)) {
		//	//	if (IsOwnedBelowOrEqual(manager, visibility))
		//	//		return true;
		//	//}
		//	//return false;
		//}

		//protected bool IsOwnedAboveOrEqual(UserOrganizationModel caller, Predicate<UserOrganizationModel> visibility) {
		//	if (visibility(caller))
		//		return true;
		//	foreach (var subordinate in caller.ManagedBy.ToListAlive().Select(x => x.Manager)) {
		//		if (IsOwnedAboveOrEqual(subordinate, visibility))
		//			return true;
		//	}
		//	return false;
		//}

		protected bool IsOwnedBelowOrEqualOrganizational<T>(T start, Origin origin) where T : IOrigin {
			if (origin.AreEqual(start))
				return true;

			foreach (var sub in start.OwnsOrigins()) {
				if (IsOwnedBelowOrEqualOrganizational(sub, origin))
					return true;
			}

			return false;
		}
		#endregion

		#region Upgrades
		public PermissionsUtility CanUpgradeUser(long userId) {
			ViewUserOrganization(userId, false);
			var user = session.Get<UserOrganizationModel>(userId);
			return CanEdit(PermItem.ResourceType.UpgradeUsersForOrganization, user.Organization.Id, exceptionMessage: "This user is '" + Config.ReviewName().ToLower() + " only' and cannot be added to an L10. You are not permitted to upgrade them.");
		}

		public PermissionsUtility CanUpgradeUsersAtOrganization(long orgId) {
			return CanEdit(PermItem.ResourceType.UpgradeUsersForOrganization, orgId, exceptionMessage: "You're not permitted to increase the user count.");
		}
		#endregion

		#region Overrides

		public PermissionsUtility TryWithAllUsers(Func<PermissionsUtility, PermissionsUtility> p) {
			try {
				return p(this);
			} catch (PermissionsException e) {
				var originalCaller = caller;
				if (caller.User != null && caller.User.UserOrganizationIds != null) {
					foreach (var id in caller.User.UserOrganizationIds) {
						if (id != caller.Id) {
							try {
								var tempcaller = session.Get<UserOrganizationModel>(id);
								if (tempcaller == null || tempcaller.DeleteTime != null)
									continue;
								var perm = new PermissionsUtility(session, tempcaller);
								caller = tempcaller;
								return p(perm);
							} catch (PermissionsException) {
							} finally {
								caller = originalCaller;
							}
						}
					}
				}
				throw e;
			}
		}

		public PermissionsUtility TryWithOverrides(Func<PermissionsUtility, PermissionsUtility> p, params PermissionType[] types) {
			try {
				return p(this);
			} catch (PermissionsException) {

				if (Overrides == null)
					Overrides = session.QueryOver<PermissionOverride>().Where(x => x.DeleteTime == null && x.ForUser.Id == caller.Id).List().ToList();


				var tryWith = Overrides.Where(x => types.Any(y => y == x.Permissions)).Where(x => x.AsUser.DeleteTime == null).Select(x => x.AsUser);
				var originalCaller = caller;
				foreach (var x in tryWith) {
					try {
						caller = x;
						return p(this);
					} catch (PermissionsException) {
						//Pass through;
					} finally {
						caller = originalCaller;
					}
				}
				caller = originalCaller;
			}
			throw new PermissionsException();
		}

		protected bool TryWithOverrides(Func<PermissionsUtility, bool> p, params PermissionType[] types) {
			try {
				return p(this);
			} catch (PermissionsException) {
				if (Overrides == null)
					Overrides = session.QueryOver<PermissionOverride>().Where(x => x.DeleteTime == null && x.ForUser.Id == caller.Id).List().ToList();


				var tryWith = Overrides.Where(x => types.Any(y => y == x.Permissions)).Where(x => x.AsUser.DeleteTime != null).Select(x => x.AsUser);
				var originalCaller = caller;
				foreach (var x in tryWith) {
					try {
						caller = x;
						return p(this);
					} catch (PermissionsException) {
						//Pass through;
					} finally {
						caller = originalCaller;
					}
				}
				caller = originalCaller;
			}
			throw new PermissionsException();
		}


		#endregion

		#region Issues

		public PermissionsUtility ViewRecurrenceIssuesForUser(long userId, long recurrenceId) // recurrence
		{
			if (IsRadialAdmin(caller))
				return this;
			return ViewL10Recurrence(recurrenceId).OwnedBelowOrEqual(userId);
			//if (userId == caller.Id)
			//    return this;
			//if (IsOwnedBelowOrEqual(caller, userId))
			//    return this;
			//return ViewL10Recurrence(recurrenceId);            
		}

		#endregion

		#region CoreProcess

		public PermissionsUtility CanCreateProcessDef() {
			return ManagerAtOrganization(caller.Id, caller.Organization.Id);
		}
		public PermissionsUtility CanViewProcessDef(long localId) {
			return CanView(PermItem.ResourceType.CoreProcess, localId);
		}

		public PermissionsUtility CanEditProcessDef(long localId) {
			return CanEdit(PermItem.ResourceType.CoreProcess, localId);
		}

		public PermissionsUtility CanAdminProcessDef(long localId) {
			return CanAdmin(PermItem.ResourceType.CoreProcess, localId);
		}

		public PermissionsUtility CanEditTask(string taskId) {
			if (IsRadialAdmin(caller))
				return this;

			ProcessDefAccessor accessor = new ProcessDefAccessor();
			var task = AsyncHelper.RunSync(() => accessor.GetTaskById_Unsafe(taskId));
			if (task.Assignee != null) {
				if (task.Assignee.Value == caller.Id)
					return this;
			} else {

				var arr = AsyncHelper.RunSync<long[]>(() => accessor.GetCandidateGroupIdsForTask_UnSafe(session, taskId));

				if (arr.Any(x => x == caller.Id)) {
					return this;
				}

				var groupIds = ResponsibilitiesAccessor.GetGroupIdsForUser(session, this, caller.Id);

				if (arr.Any(x => groupIds.Contains(x))) {
					return this;
				}
			}
			throw new PermissionsException();
		}
		public PermissionsUtility CanViewTasksForCandidateGroup(long groupId) {
			if (IsRadialAdmin(caller))
				return this;

			// groupId can be userId too.
			var ids = ResponsibilitiesAccessor.GetMemberIds(session, this, groupId);

			if (ids.Any(x => x == this.GetCaller().Id)) {
				return this;
			}

			throw new PermissionsException();
		}

		#endregion

		#region Events
		public PermissionsUtility SubscribeToEvent(long subscriberUserId, IEventAnalyzerGenerator analyzer) {
			var permChecked = false;
			Self(subscriberUserId);
			if (analyzer is IEventAnalyzerGenerator) {
				ViewL10Recurrence(((IRecurrenceEventAnalyerGenerator)analyzer).RecurrenceId);
				permChecked = true;
			}

			if (permChecked)
				return this;
			throw new PermissionsException("no permissions were checked");
		}
		public PermissionsUtility ViewEvent(long eventId, IEventAnalyzerGenerator analyzer) {
			var evt = session.Get<EventSubscription>(eventId);
			return SubscribeToEvent(evt.SubscriberId, analyzer);
		}
		#endregion

		public PermissionsUtility InValidPermission() {
			if (Config.IsLocal()) {
				return this;
			}

			throw new PermissionsException("Invalid Permissions");
		}



		public PermissionsUtility Or(params Func<PermissionsUtility>[] or) {
			foreach (var o in or) {
				try {
					return o();
				} catch (PermissionsException) { } catch (Exception) { }
			}
			throw new PermissionsException();
		}

		public PermissionsUtility Or(params Func<PermissionsUtility, PermissionsUtility>[] or) {
			foreach (var o in or) {
				try {
					return o(this);
				} catch (PermissionsException) { } catch (Exception) { }
			}
			throw new PermissionsException();
		}


		public static bool IsAdmin(UserOrganizationModel caller) {
			return IsRadialAdmin(caller);
		}

		public delegate PermissionsUtility LongFunc(long id);

		private PermissionsUtility _ConfirmPermissions<T, M>(T model, bool fixRefs, Expression<Func<T, long?>> idSelector, Expression<Func<T, M>> modelSelector, Func<PermissionsUtility, LongFunc> permissionsSelector) where M : ILongIdentifiable {
			var id = model.Get(idSelector);
			var m = model.Get(modelSelector);
			if (id == null) {
				if (m == null)
					return this; //No error.. looks like its optional
				if (m.Id == 0)
					throw new PermissionsException("Model uninitialized [1]");
				if (fixRefs)
					model.Set(idSelector, m.Id);
				return permissionsSelector(this)(m.Id);
			} else if (m == null) {
				if (id == 0)
					throw new PermissionsException();
				if (fixRefs) {
					var mLoaded = session.Get<M>(id.Value);
					model.Set(modelSelector, mLoaded);
				}

				return permissionsSelector(this)(id.Value);
			} else {
				if (id == 0) {
					if (m.Id == 0)
						throw new PermissionsException("Model uninitialized [2]");
					if (fixRefs)
						model.Set(idSelector, m.Id);
					return permissionsSelector(this)(m.Id);
				} else {
					if (m.Id == 0)
						throw new PermissionsException("Model uninitialized [3]");
					if (id != m.Id)
						throw new PermissionsException("Model Id != Id");
					return permissionsSelector(this)(id.Value);
				}
			}
		}

		private PermissionsUtility _ConfirmPermissions<T, M>(T model, bool fixRefs, Expression<Func<T, long>> idSelector, Expression<Func<T, M>> modelSelector, Func<PermissionsUtility, LongFunc> permissionsSelector) where M : ILongIdentifiable {
			var id = model.Get(idSelector);
			var m = model.Get(modelSelector);
			if (m == null) {
				if (id == 0)
					throw new PermissionsException();

				if (fixRefs) {
					var mLoaded = session.Load<M>(id);
					model.Set(modelSelector, mLoaded);
				}

				return permissionsSelector(this)(id);
			} else {
				if (id == 0) {
					if (m.Id == 0)
						throw new PermissionsException("Model uninitialized [2]");

					if (fixRefs)
						model.Set(idSelector, m.Id);
					return permissionsSelector(this)(m.Id);
				} else {
					if (m.Id == 0)
						throw new PermissionsException("Model uninitialized [3]");

					if (id != m.Id)
						throw new PermissionsException("Model Id != Id");
					return permissionsSelector(this)(id);
				}
			}
		}

		public PermissionsUtility Confirm<T, M>(T model, Expression<Func<T, long?>> idSelector, Expression<Func<T, M>> modelSelector, Func<PermissionsUtility, LongFunc> permissionsSelector) where M : ILongIdentifiable {
			return _ConfirmPermissions(model, false, idSelector, modelSelector, permissionsSelector);
		}
		public PermissionsUtility Confirm<T, M>(T model, Expression<Func<T, long>> idSelector, Expression<Func<T, M>> modelSelector, Func<PermissionsUtility, LongFunc> permissionsSelector) where M : ILongIdentifiable {
			return _ConfirmPermissions(model, false, idSelector, modelSelector, permissionsSelector);
		}
		public PermissionsUtility ConfirmAndFix<T, M>(T model, Expression<Func<T, long>> idSelector, Expression<Func<T, M>> modelSelector, Func<PermissionsUtility, LongFunc> permissionsSelector) where M : ILongIdentifiable {
			return _ConfirmPermissions(model, true, idSelector, modelSelector, permissionsSelector);
		}
		public PermissionsUtility ConfirmAndFix<T, M>(T model, Expression<Func<T, long?>> idSelector, Expression<Func<T, M>> modelSelector, Func<PermissionsUtility, LongFunc> permissionsSelector) where M : ILongIdentifiable {
			return _ConfirmPermissions(model, true, idSelector, modelSelector, permissionsSelector);
		}


		public PermissionsUtility Noop(long id) {
			return this;
		}

		public bool IsPermitted(Action<PermissionsUtility> ensurePermitted) {
			try {
				ensurePermitted(this);
				return true;
			} catch (Exception) {
				return false;
			}
		}

		public PermissionsUtility CanUpload() {
			if (caller == null || caller.DeleteTime != null)
				throw new PermissionsException("You cannot upload documents");
			return this;
		}



		public PermissionsUtility ViewOrganizationPosition(long positionId) {
			var orgId = session.Get<OrganizationPositionModel>(positionId).Organization.Id;
			return ViewOrganization(orgId);
		}


		public PermissionsUtility ViewVideoL10Recurrence(long recurrenceId) {
			return ViewL10Recurrence(recurrenceId);
		}

		public PermissionsUtility Self(IForModel forModel) {
			if (IsRadialAdmin(caller))
				return this;
			if (forModel.ModelType == ForModel.GetModelType<UserOrganizationModel>())
				return Self(forModel.ModelId);
			throw new PermissionsException();
		}

		public PermissionsUtility Self(long userId) {
			if (IsRadialAdmin(caller))
				return this;
			if (userId == caller.Id)
				return this;
			if (caller.UserIds != null && caller.UserIds.Any(x => x == userId))
				return this;
			throw new PermissionsException();
		}

		protected bool IsSelf(long id_DONT_USE_CALLER_ID) {

			if (id_DONT_USE_CALLER_ID == caller.Id)
				return true;
			if (caller.User != null && caller.User.UserOrganizationIds != null && caller.User.UserOrganizationIds.Contains(id_DONT_USE_CALLER_ID)) {
				var found = session.Get<UserOrganizationModel>(id_DONT_USE_CALLER_ID);
				if (found.DeleteTime != null)
					return false;
				return true;
			}
			return false;

		}


		public UserOrganizationModel GetCaller() {
			return caller;
		}

	}
}
