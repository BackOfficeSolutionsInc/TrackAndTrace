using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Web;
using Amazon.Auth.AccessControlPolicy;
using Amazon.EC2;
using Amazon.ElasticTranscoder.Model;
using Amazon.IdentityManagement.Model;
using Microsoft.Ajax.Utilities;
using RadialReview.Accessors;
using RadialReview.Controllers;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.L10;
using RadialReview.Models.Accountability;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Areas.CoreProcess.Models.MapModel;
using RadialReview.Models.Components;
using RadialReview.Areas.CoreProcess.Accessors;
using RadialReview.Areas.CoreProcess.Models;
using RadialReview.Models.Interfaces;

namespace RadialReview.Utilities {
	public partial class PermissionsUtility {

		public void UnsafeAllow(PermItem.AccessLevel level, PermItem.ResourceType resourceType, long id) {
			string key;
			switch (level) {
				case PermItem.AccessLevel.View:
					key = "CanView_" + resourceType + "~" + id;
					break;
				case PermItem.AccessLevel.Edit:
					key = "CanEdit_" + resourceType + "~" + id;
					break;
				case PermItem.AccessLevel.Admin:
					key = "CanAdmin_" + resourceType + "~" + id;
					break;
				default:
					throw new ArgumentOutOfRangeException("" + level.ToString());
			}
			new CacheChecker(key, this).Execute(() => this);
			//this.cache[key] = new CacheResult() { };
		}

		public void EnsureAdminExists(PermItem.ResourceType resourceType, long resourceId) {
			var items = session.QueryOver<PermItem>().Where(x => x.DeleteTime == null && x.CanAdmin && x.ResId == resourceId && x.ResType == resourceType).List();
			if (!items.Any())
				throw new PermissionsException("You must have an admin. Reverting setting change.") {
					NoErrorReport = true
				};
			//Cheapest first..
			foreach (var i in items.OrderBy(x => (int)x.AccessorType)) {
				switch (i.AccessorType) {
					case PermItem.AccessType.RGM:
						var users = ResponsibilitiesAccessor.GetResponsibilityGroupMembers(session, this, i.AccessorId);
						if (users.Any())
							return;
						break;
					case PermItem.AccessType.Members:
						var ids = GetMyMemeberUserIds(resourceType, resourceId);
						var idsAlive = session.QueryOver<UserOrganizationModel>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(ids).Select(x => x.Id).List<long>();
						if (idsAlive.Any())
							return;
						break;
					case PermItem.AccessType.Creator:
						var creator = session.Get<UserOrganizationModel>(i.AccessorId);
						if (creator.DeleteTime == null)
							return;
						break;
					case PermItem.AccessType.Admins:
						var orgId = GetOrganizationId(resourceType, resourceId);
						var org = session.Get<OrganizationModel>(orgId);
						var canEdit = org.ManagersCanEdit;
						var orgAdminsQ = session.QueryOver<UserOrganizationModel>().Where(x => x.DeleteTime == null && x.Organization.Id == orgId);
						if (canEdit) {
							orgAdminsQ = orgAdminsQ.Where(x => x.ManagingOrganization || x.ManagerAtOrganization);
						} else {
							orgAdminsQ = orgAdminsQ.Where(x => x.ManagingOrganization);
						}
						var orgAdmins = orgAdminsQ.Select(x => x.Id).List<long>();
						if (orgAdmins.Any())
							return;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			throw new PermissionsException("You must have an admin. Reverting setting change.") {
				NoErrorReport = true
			};
		}
		public PermissionsUtility CanViewPermissions(PermItem.ResourceType resourceType, long resourceId) {
			return CheckCacheFirst("CanViewPermissions_" + resourceType, resourceId).Execute(() => {
				var result = this;
				if (CanAccessItem(PermItem.AccessLevel.View, resourceType, resourceId, null, ref result))
					return result;
				throw new PermissionsException("Can not view this item.") {
					NoErrorReport = true
				};
			});
		}


		public PermissionsUtility CanView(PermItem.ResourceType resourceType, long resourceId, Func<PermissionsUtility, PermissionsUtility> defaultAction = null, string exceptionMessage = null) {
			return CheckCacheFirst("CanView_" + resourceType, resourceId).Execute(() => {
				var result = this;
				if (CanAccessItem(PermItem.AccessLevel.View, resourceType, resourceId, defaultAction, ref result))
					return result;
				throw new PermissionsException(exceptionMessage ?? "Can not view this item.") {
					NoErrorReport = true
				};
			});
		}

		public PermissionsUtility CanEdit(PermItem.ResourceType resourceType, long resourceId, Func<PermissionsUtility, PermissionsUtility> defaultAction = null, string exceptionMessage = null) {
			return CheckCacheFirst("CanEdit_" + resourceType, resourceId).Execute(() => {
				var result = this;
				if (CanAccessItem(PermItem.AccessLevel.Edit, resourceType, resourceId, defaultAction, ref result))
					return result;

				throw new PermissionsException(exceptionMessage ?? "Can not edit this item.") {
					NoErrorReport = true
				};
			});
		}
		public PermissionsUtility CanAdmin(PermItem.ResourceType resourceType, long resourceId, Func<PermissionsUtility, PermissionsUtility> defaultAction = null, string exceptionMessage = null) {
			return CheckCacheFirst("CanAdmin_" + resourceType, resourceId).Execute(() => {
				var result = this;
				if (CanAccessItem(PermItem.AccessLevel.Admin, resourceType, resourceId, defaultAction, ref result))
					return result;

				throw new PermissionsException(exceptionMessage ?? "Can not administrate this item.") {
					NoErrorReport = true
				};
			});
		}


		public List<PermItem> GetAdmins(PermItem.ResourceType resourceType, long resourceId) {
			CanView(resourceType, resourceId);
			CanViewPermissions(resourceType, resourceId);
			//var admin = false;
			var items = session.QueryOver<PermItem>().Where(x => x.DeleteTime == null && x.ResId == resourceId && x.ResType == resourceType).List().ToList();
			PermissionsAccessor.LoadPermItem(session, items);
			return items.Where(x => x.CanAdmin).ToList();
		}

		//public List<PermItem> GetPermissionsForUser(long userId, PermItem.ResourceType resourceType) {
		//	var allowedAccessors = ResponsibilitiesAccessor.GetResponsibilityGroupsForRgm(session, this, userId);
		//	var permItemsUnfiltered = session.QueryOver<PermItem>()
		//						   .Where(x => x.ResType == resourceType && x.DeleteTime == null)
		//						   .WhereRestrictionOn(x=>x.AccessorId).IsIn(allowedAccessors.Select(x=>x.Id).ToArray())
		//						   .List().ToList();
		//	var permItems = permItemsUnfiltered.Where(x => {
		//		return allowedAccessors.Any(a => {
		//			if (a is OrganizationModel && x.AccessorType && a.Id == x.AccessorId) 
		//		});
		//	});
		//}



		protected bool CanAccessItem(PermItem.AccessLevel level, PermItem.ResourceType resourceType, long resourceId, Func<PermissionsUtility, PermissionsUtility> defaultAction, ref PermissionsUtility result, bool and = true) {
			if (IsRadialAdmin(caller))
				return true;

			var permItems = session.QueryOver<PermItem>().Where(x => x.ResId == resourceId && x.ResType == resourceType && x.DeleteTime == null).List().ToList();

			//Only want true if we actually have permissions...
			if (!permItems.Any()) {
				//This might be redundant with the !anyFlags check.
				if (defaultAction != null) {
					result = defaultAction(this);
					return true;
				}
				return false;
			}

			List<ResponsibilityGroupModel> groups = null;
			var anyFlags = false;

			foreach (var flag in new[] { PermItem.AccessLevel.View, PermItem.AccessLevel.Edit, PermItem.AccessLevel.Admin }) {
				/*Ordered by cheapest first...*/
				if (level.HasFlag(flag)) {
					//only want to return if we handled some flags.
					anyFlags = true;
					var currentTrue = false;
					//Is a OrgAdmin
					currentTrue = currentTrue || (permItems.Any(x => x.HasFlags(flag) && x.AccessorType == PermItem.AccessType.Admins && IsOrgAdmin(resourceType, resourceId)));
					//Email address
					currentTrue = currentTrue || (permItems.Any(x => x.HasFlags(flag) && x.AccessorType == PermItem.AccessType.RGM && x.AccessorId == caller.Id));
					//Is creator
					currentTrue = currentTrue || (permItems.Any(x => x.HasFlags(flag) && x.AccessorType == PermItem.AccessType.Creator && IsCreator(resourceType, resourceId)));
					//Is a Member
					currentTrue = currentTrue || (permItems.Any(x => x.HasFlags(flag) && x.AccessorType == PermItem.AccessType.Members && IsMember(resourceType, resourceId)));
					//Special UserIds
					currentTrue = currentTrue || (permItems.Any(x => {
						var aretrue = x.HasFlags(flag) && x.AccessorType == PermItem.AccessType.Email && caller.User != null;
						if (aretrue) {
							var e = session.Get<EmailPermItem>(x.AccessorId);
							return e.Email.ToLower() == caller.User.UserName.ToLower();
						}
						return false;
					}));
					//Special Teams/Positions/Etc (only call if necessary)
					if (!currentTrue && permItems.Any(x => x.HasFlags(flag) && x.AccessorType == PermItem.AccessType.RGM && x.AccessorId != caller.Id)) {
						//Expensive, only call once.
						groups = groups ?? ResponsibilitiesAccessor.GetResponsibilityGroupsForUser(session.ToQueryProvider(true), this, caller.Id);
						currentTrue = currentTrue || permItems.Any(pItem => pItem.HasFlags(flag) && pItem.AccessorType == PermItem.AccessType.RGM && groups.Any(group => pItem.AccessorId == group.Id));
					}
					if (!currentTrue && and)
						return false;

					if (currentTrue && !and)
						return true;
				}
			}

			if (!anyFlags)
				return false;

			//Everything passed.
			if (and)
				return true;
			else
				return false;
		}

		/// <summary>
		/// Grabs perm items for explicitly specified permissions(RGM, Email) and implicit (Creator, Admin, Members)
		/// </summary>
		/// <param name="s"></param>
		/// <param name="callerPerms"></param>
		/// <param name="forUserId"></param>
		/// <param name="resourceType"></param>
		/// <returns></returns>
		//DON'T DELETE, COULD BE USEFUL
		public IEnumerable<PermItem> GetAllPermItemsForUser(PermItem.ResourceType resourceType, long forUserId) {
			var s = session;
			var callerPerms = this;
			//return items for RGM and Email
			foreach (var e in PermissionsAccessor.GetExplicitPermItemsForUser(s, callerPerms, forUserId, resourceType))
				yield return e;

			var user = s.Get<UserOrganizationModel>(forUserId);

			var memberOfTheseResourceIds = callerPerms.GetIdsForResourceThatUserIsMemberOf(resourceType, forUserId, true);
			var userCreatedTheseResourceIds = callerPerms.GetIdsForResourcesCreatedByUser(resourceType, forUserId);
			var resourceIdsAtOrganization = callerPerms.GetIdsForResourceForOrganization(resourceType, user.Organization.Id);

			var allResourceIds = new List<long>();
			allResourceIds.AddRange(memberOfTheseResourceIds);
			allResourceIds.AddRange(userCreatedTheseResourceIds);
			allResourceIds.AddRange(resourceIdsAtOrganization);
			allResourceIds = allResourceIds.Distinct().ToList();


			var allPermItemsUnfiltered = s.QueryOver<PermItem>()
				.Where(x => x.ResType == resourceType && x.DeleteTime == null)
				.Where(x => x.AccessorType == PermItem.AccessType.Members || x.AccessorType == PermItem.AccessType.Admins || x.AccessorType == PermItem.AccessType.Creator)
				.WhereRestrictionOn(x => x.ResId).IsIn(allResourceIds)
				.List().ToList();

			//==Get Members==
			{
				//Things I am a member of
				//	Any member permissions?
				if (memberOfTheseResourceIds.Any()) {
					var permItems = allPermItemsUnfiltered
										.Where(x => x.ResType == resourceType && x.AccessorType == PermItem.AccessType.Members && x.DeleteTime == null)
										.Where(x => memberOfTheseResourceIds.Contains(x.ResId))
										.ToList();
					foreach (var p in permItems) {
						//Add the creator perm item(s)
						yield return p;
					}
				}
			}
			//==Get Creators==
			{
				//Things I created..
				if (userCreatedTheseResourceIds.Any()) {
					//	Any creator permissions?
					var permItems = allPermItemsUnfiltered
										.Where(x => x.ResType == resourceType && x.AccessorType == PermItem.AccessType.Creator && x.DeleteTime == null)
										.Where(x => userCreatedTheseResourceIds.Contains(x.ResId))
										.ToList();
					foreach (var p in permItems) {
						//Add the creator perm item(s)
						yield return p;
					}
				}
			}

			//==Get Admins==
			{
				if (user.IsManagingOrganization()) {
					if (resourceIdsAtOrganization.Any()) {
						//	Any admins permissions?
						var permItems = allPermItemsUnfiltered
							.Where(x => x.ResType == resourceType && x.AccessorType == PermItem.AccessType.Admins && x.DeleteTime == null)
							.Where(x => resourceIdsAtOrganization.Contains(x.ResId))
							.ToList();
						foreach (var p in permItems) {
							//Add the admins perm item(s)
							yield return p;
						}
					}
				}
			}
			yield break;
		}

		protected bool IsMember(PermItem.ResourceType resourceType, long resourceId) {
			var isMember_ids = GetMyMemeberUserIds(resourceType, resourceId, true);
			return (isMember_ids.Any(id => id == caller.Id));
		}
		protected bool IsOrgAdmin(PermItem.ResourceType resourceType, long resourceId) {
			return (GetOrganizationId(resourceType, resourceId) == caller.Organization.Id && caller.IsManagingOrganization());
		}


		#region Implement for each resource type

		protected List<long> GetMyMemeberUserIds(PermItem.ResourceType resourceType, long resourceId, bool meOnly = false) {
			switch (resourceType) {
				case PermItem.ResourceType.L10Recurrence: {
						var isMember_idsQ = session.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
						.Where(x => x.L10Recurrence.Id == resourceId && x.DeleteTime == null);
						if (meOnly)
							isMember_idsQ = isMember_idsQ.Where(x => x.User.Id == caller.Id);
						var isMember_ids = isMember_idsQ.Select(x => x.User.Id).List<long>().ToList();
						return isMember_ids;
					}
				case PermItem.ResourceType.AccountabilityHierarchy: {
						var ac = session.Get<AccountabilityChart>(resourceId);
						var isMember_idsQ = session.QueryOver<UserOrganizationModel>()
							.Where(x => x.Organization.Id == ac.OrganizationId && x.DeleteTime == null);
						if (meOnly)
							isMember_idsQ = isMember_idsQ.Where(x => x.Id == caller.Id);
						var isMember_ids = isMember_idsQ.Select(x => x.Id).List<long>().ToList();
						return isMember_ids;
					}
				case PermItem.ResourceType.CoreProcess: {
						return AsyncHelper.RunSync<List<long>>(() => new ProcessDefAccessor().GetCandidateGroupIds_Unsafe(session, resourceId));
					}
				case PermItem.ResourceType.SurveyContainer: {
						var isMember_idsQ = session.QueryOver<Survey>()
							.Where(x => x.SurveyContainerId == resourceId && x.DeleteTime == null)
							.Where(x => x.By.ModelType == ForModel.GetModelType<UserOrganizationModel>());
						if (meOnly)
							isMember_idsQ = isMember_idsQ.Where(x => x.By.ModelId == caller.Id);
						var isMember_ids = isMember_idsQ.Select(x => x.By.ModelId).List<long>().ToList();
						return isMember_ids;
					}
				default:
					throw new ArgumentOutOfRangeException("resourceType");
			}
		}

		public IEnumerable<long> GetIdsForResourceThatUserIsMemberOf(PermItem.ResourceType resourceType, long userId, bool ignoreExceptions = false) {
			try {
				switch (resourceType) {
					case PermItem.ResourceType.L10Recurrence:
						return session.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
										.Where(x => x.User.Id == userId && x.DeleteTime == null)
										.Select(x => x.L10Recurrence.Id)
										.Future<long>().Distinct();
					case PermItem.ResourceType.AccountabilityHierarchy:
						return new long[] { session.Get<UserOrganizationModel>(userId).Organization.AccountabilityChartId };
					case PermItem.ResourceType.SurveyContainer:
						return session.QueryOver<Survey>()
							.Where(x => x.By.ModelType == ForModel.GetModelType<UserOrganizationModel>() && x.By.ModelId == userId && x.DeleteTime == null)
							.Select(x => x.SurveyContainerId)
							.Future<long>().Distinct();
					case PermItem.ResourceType.CoreProcess:
						throw new NotImplementedException();//Too Expensive..
					case PermItem.ResourceType.UpdatePaymentForOrganization:
						return new[] { session.Get<UserOrganizationModel>(userId).Organization.Id };
					default:
						throw new ArgumentOutOfRangeException("resourceType");
				}
			} catch {
				if (!ignoreExceptions)
					throw;
				return new long[] { };
			}
		}

		protected bool IsCreator(PermItem.ResourceType resourceType, long resourceId) {
			switch (resourceType) {
				case PermItem.ResourceType.L10Recurrence:
					return (session.Get<L10Recurrence>(resourceId).CreatedById == caller.Id);
				case PermItem.ResourceType.AccountabilityHierarchy:
					return false;
				case PermItem.ResourceType.CoreProcess:
					var r = session.Get<ProcessDef_Camunda>(resourceId).Creator;
					return (r.ModelId == caller.Id && r.ModelType == ForModel.GetModelType<UserOrganizationModel>());
				case PermItem.ResourceType.SurveyContainer:
					var creator = session.Get<SurveyContainer>(resourceId).CreatedBy;
					return creator.Is<UserOrganizationModel>() && creator.ModelId == caller.Id;
				case PermItem.ResourceType.UpdatePaymentForOrganization:
					return false;
				default:
					throw new ArgumentOutOfRangeException("resourceType");
			}
		}

		public IEnumerable<long> GetIdsForResourcesCreatedByUser(PermItem.ResourceType resourceType, long userId) {
			switch (resourceType) {
				case PermItem.ResourceType.L10Recurrence:
					return session.QueryOver<L10Recurrence>().Where(x => x.CreatedById == userId && x.DeleteTime == null).Select(x => x.Id).Future<long>();
				case PermItem.ResourceType.AccountabilityHierarchy:
					return new long[] { };
				case PermItem.ResourceType.UpgradeUsersForOrganization:
					return new long[] { };
				case PermItem.ResourceType.CoreProcess:
					return session.QueryOver<ProcessDef_Camunda>()
						.Where(x => x.Creator.ModelType == ForModel.GetModelType<UserOrganizationModel>() && x.Creator.ModelId == userId && x.DeleteTime == null)
						.Select(x => x.Id)
						.Future<long>();
				case PermItem.ResourceType.SurveyContainer:
					return session.QueryOver<SurveyContainer>()
						.Where(x => x.CreatedBy.ModelType == ForModel.GetModelType<UserOrganizationModel>() && x.CreatedBy.ModelId == userId && x.DeleteTime == null)
						.Select(x => x.Id)
						.Future<long>();
				default:
					throw new ArgumentOutOfRangeException("resourceType");
			}
		}

		protected long GetOrganizationId(PermItem.ResourceType resourceType, long resourceId) {
			switch (resourceType) {
				case PermItem.ResourceType.L10Recurrence:
					return session.Get<L10Recurrence>(resourceId).OrganizationId;
				case PermItem.ResourceType.AccountabilityHierarchy:
					return session.Get<AccountabilityChart>(resourceId).OrganizationId;
				case PermItem.ResourceType.UpgradeUsersForOrganization:
					return resourceId;
				case PermItem.ResourceType.CoreProcess:
					return session.Get<ProcessDef_Camunda>(resourceId).OrgId;
				case PermItem.ResourceType.SurveyContainer:
					return session.Get<SurveyContainer>(resourceId).OrgId;
				case PermItem.ResourceType.UpdatePaymentForOrganization:
					return resourceId;
				default:
					throw new ArgumentOutOfRangeException("resourceType");
			}
		}

		public IEnumerable<long> GetIdsForResourceForOrganization(PermItem.ResourceType resourceType, long orgId) {
			switch (resourceType) {
				case PermItem.ResourceType.L10Recurrence:
					return session.QueryOver<L10Recurrence>()
									.Where(x => x.OrganizationId == orgId && x.DeleteTime == null)
									.Select(x => x.Id)
									.Future<long>();
				case PermItem.ResourceType.AccountabilityHierarchy:
					return new long[] {
						session.Get<OrganizationModel>(orgId).AccountabilityChartId
					};
				case PermItem.ResourceType.SurveyContainer:
					return session.QueryOver<SurveyContainer>()
						.Where(x => x.OrgId == orgId && x.DeleteTime == null)
						.Select(x => x.Id)
						.Future<long>();
				case PermItem.ResourceType.CoreProcess:
					return session.QueryOver<ProcessDef_Camunda>()
						.Where(x => x.OrgId == orgId && x.DeleteTime == null)
						.Select(x => x.Id)
						.Future<long>();
				default:
					throw new ArgumentOutOfRangeException("resourceType");
			}
		}


		#endregion


	}
}
