using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Amazon.ElasticTranscoder.Model;
using FluentNHibernate.Utils;
using Microsoft.AspNet.SignalR;
using NHibernate;
using NHibernate.Linq;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.CompanyValue;
using RadialReview.Models.Angular.VTO;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.L10;
using RadialReview.Models.Periods;
using RadialReview.Models.VTO;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Query;
using RadialReview.Models.Reviews;
using RadialReview.Models.Rocks;
using RadialReview.Utilities.RealTime;
using RadialReview.Model.Enums;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Dashboard;
using System.Threading.Tasks;
using RadialReview.Hooks;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.Synchronize;

namespace RadialReview.Accessors {
	public class RockAndMilestones {
		public RockModel Rock { get; set; }
		public List<Milestone> Milestones { get; set; }
		public bool AnyMilestoneMeetings { get; set; }
	}

	public class RockAccessor {

		#region Milestones
		public static Milestone AddMilestone(UserOrganizationModel caller, long rockId, string milestone, DateTime dueDate) {

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create()) {
						var perm = PermissionsUtility.Create(s, caller);
						perm.EditRock(rockId);
						var ms = new Milestone() {
							DueDate = dueDate,
							Name = milestone,
							Required = true,
							RockId = rockId,

						};
						s.Save(ms);

						var recurrenceIds = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
							.Where(x => x.DeleteTime == null && x.ForRock.Id == rockId)
							.Select(x => x.L10Recurrence.Id).List<long>();

						tx.Commit();
						s.Flush();

						rt.UpdateRecurrences(recurrenceIds).AddLowLevelAction(x => x.setMilestone(ms));

						var rock = s.Get<RockModel>(ms.RockId);
						//var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();

						//var group = hub.Clients.Group(MeetingHub.GenerateUserId(rock.ForUserId));

						//var dashboards = s.QueryOver<Dashboard>().Where(x => x.DeleteTime == null && x.== rock.ForUserId).Select(x => x.Id).List<long>().ToList();


						var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
						var userMeetingHub = hub.Clients.Group(MeetingHub.GenerateUserId(rock.ForUserId));
						//var todoData = TodoData.FromTodo(todo);
						//userMeetingHub.appendTodo(".todo-list", todoData);
						var updates = new AngularRecurrence(-2);
						updates.Todos = AngularList.CreateFrom(AngularListType.Add, new AngularTodo(ms, rock.AccountableUser));
						userMeetingHub.update(updates);


						return ms;
					}
				}
			}
		}
		public static void EditMilestone(UserOrganizationModel caller, long milestoneId, string name = null, DateTime? duedate = null, bool? required = null, MilestoneStatus? status = null, string connectionId = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create(connectionId)) {

						var ms = s.Get<Milestone>(milestoneId);

						var perm = PermissionsUtility.Create(s, caller);
						perm.EditRock(ms.RockId);

						ms.Name = name ?? ms.Name;
						ms.DueDate = duedate ?? ms.DueDate;
						ms.Required = required ?? ms.Required;
						if (status != null) {
							if (status == MilestoneStatus.Done && ms.Status != MilestoneStatus.Done) {
								ms.CompleteTime = DateTime.UtcNow;
							}
							if (status == MilestoneStatus.NotDone && ms.Status != MilestoneStatus.NotDone) {
								ms.CompleteTime = null;
							}
						}

						ms.Status = status ?? ms.Status;

						s.Update(ms);

						var recurrenceIds = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
							.Where(x => x.DeleteTime == null && x.ForRock.Id == ms.RockId)
							.Select(x => x.L10Recurrence.Id).List<long>();

						tx.Commit();
						s.Flush();

						rt.UpdateRecurrences(recurrenceIds).AddLowLevelAction(x => x.setMilestone(ms));

						var rock = s.Get<RockModel>(ms.RockId);

						var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
						var group = hub.Clients.Group(MeetingHub.GenerateUserId(rock.ForUserId), connectionId);
						group.update(new AngularUpdate() { new AngularTodo(ms, rock.AccountableUser) });
					}
				}
			}
		}
		public static void DeleteMilestone(UserOrganizationModel caller, long milestoneId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create()) {

						var ms = s.Get<Milestone>(milestoneId);

						var perm = PermissionsUtility.Create(s, caller);
						perm.EditRock(ms.RockId);

						ms.DeleteTime = ms.DeleteTime ?? DateTime.UtcNow;

						s.Update(ms);

						var recurrenceIds = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
							.Where(x => x.DeleteTime == null && x.ForRock.Id == ms.RockId)
							.Select(x => x.L10Recurrence.Id).List<long>();

						tx.Commit();
						s.Flush();

						rt.UpdateRecurrences(recurrenceIds).AddLowLevelAction(x => x.deleteMilestone(milestoneId));
					}
				}
			}
		}
		public static Milestone GetMilestone(UserOrganizationModel caller, long milestoneId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller);
					var ms = s.Get<Milestone>(milestoneId);
					perm.ViewRock(ms.RockId);
					ms._Rock = s.Get<RockModel>(ms.RockId);
					return ms;
				}
			}
		}
		public static List<Milestone> GetMilestonesForRock(UserOrganizationModel caller, long rockId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewRock(rockId);
					var ms = s.QueryOver<Milestone>().Where(x => x.RockId == rockId && x.DeleteTime == null).List().ToList();
					return ms;
				}
			}
		}
		public static RockAndMilestones GetRockAndMilestones(UserOrganizationModel caller, long rockId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewRock(rockId);
					var ms = s.QueryOver<Milestone>().Where(x => x.RockId == rockId && x.DeleteTime == null).List().ToList();
					var rock = s.Get<RockModel>(rockId);

					var l10Ids = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>().Where(x => x.ForRock.Id == rockId && x.DeleteTime == null).Select(x => x.L10Recurrence.Id).List<long>().ToList();

					var rockTypes = s.QueryOver<L10Recurrence>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(l10Ids).Select(x => x.RockType).List<L10RockType?>().ToList();

					return new RockAndMilestones() {
						Milestones = ms,
						Rock = rock,
						AnyMilestoneMeetings = rockTypes.Any(x => x == L10RockType.Milestones)
					};
				}
			}
		}
		#endregion

		#region Getters

		public static RockModel GetRock(UserOrganizationModel caller, long rockId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller).ViewRock(rockId);
					var rock = s.Get<RockModel>(rockId);
					return rock;
				}
			}
		}
		public static List<RockModel> GetRocks(UserOrganizationModel caller, long forUserId,/* long? periodId,*/ DateRange range = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller);
					return GetRocks(s.ToQueryProvider(true), perm, forUserId, /*periodId,*/ range);
				}
			}
		}
		public static List<RockModel> GetRocks(AbstractQuery queryProvider, PermissionsUtility perms, long forUserId, /*long? periodId,*/ DateRange range) {
			perms.ViewUserOrganization(forUserId, false);
			//if (periodId == null)
			//	return queryProvider.Where<RockModel>(x => x.ForUserId == forUserId).FilterRange(range).ToList();
			return queryProvider.Where<RockModel>(x => x.ForUserId == forUserId /*&& x.PeriodId == periodId*/).FilterRange(range).ToList();
		}

		public static List<RockModel> GetAllRocks(UserOrganizationModel caller, long forUserId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller);
					return GetAllRocks(s.ToQueryProvider(true), perm, forUserId);
				}
			}
		}
		public static List<RockModel> GetAllRocks(ISession s, PermissionsUtility perms, long forUserId) {
			return GetAllRocks(s.ToQueryProvider(true), perms, forUserId);
		}
		public static List<RockModel> GetAllRocks(AbstractQuery queryProvider, PermissionsUtility perms, long forUserId) {
			perms.Or(x => x.ViewUserOrganization(forUserId, false), x => x.ViewOrganization(forUserId));
			return queryProvider.Where<RockModel>(x => x.ForUserId == forUserId && x.DeleteTime == null);
		}
		public static List<RockModel> GetAllVisibleRocksAtOrganization(ISession s, PermissionsUtility perm, long orgId, bool populateUsers) {
			perm.ViewOrganization(orgId);
			var caller = perm.GetCaller();
			IQueryOver<RockModel, RockModel> q;

			var managing = caller.Organization.Id == orgId && caller.ManagingOrganization;

			if (caller.Organization.Settings.OnlySeeRocksAndScorecardBelowYou && !managing) {
				var userIds = DeepAccessor.Users.GetSubordinatesAndSelf(s, caller, caller.Id);
				q = s.QueryOver<RockModel>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null).WhereRestrictionOn(x => x.ForUserId).IsIn(userIds);
			} else {
				q = s.QueryOver<RockModel>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null);
			}

			if (populateUsers)
				q = q.Fetch(x => x.AccountableUser).Eager;
			return q.List().ToList();
		}
		public static List<RockModel> GetAllVisibleRocksAtOrganization(UserOrganizationModel caller, long orgId, bool populateUsers) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//Todo permissions not enough
					var perm = PermissionsUtility.Create(s, caller);
					return GetAllVisibleRocksAtOrganization(s, perm, orgId, populateUsers);
				}
			}
		}
		public static List<RockModel> GetAllRocksAtOrganization(UserOrganizationModel caller, long orgId, bool populateUsers) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//Todo permissions not enough
					var perm = PermissionsUtility.Create(s, caller).ViewOrganization(orgId);
					var q = s.QueryOver<RockModel>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null);
					if (populateUsers)
						q = q.Fetch(x => x.AccountableUser).Eager;
					return q.List().ToList();
				}
			}
		}
		public static L10Meeting.L10Meeting_Rock GetRockInMeeting(UserOrganizationModel caller, long rockId, long meetingId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var p = PermissionsUtility.Create(s, caller).ViewL10Meeting(meetingId);
					var found = s.QueryOver<L10Meeting.L10Meeting_Rock>().Where(x => x.DeleteTime == null && x.L10Meeting.Id == meetingId && x.ForRock.Id == rockId).Take(1).SingleOrDefault();
					if (found == null)
						throw new PermissionsException("Rock not available.");

					var a = found.ForRock.AccountableUser.GetName();
					var b = found.ForRock.AccountableUser.ImageUrl(true);
					var c = found.Completion;
					var d = found.L10Meeting.CreateTime;
					return found;
				}
			}
		}
		public static List<RockModel> GetPotentialMeetingRocks(UserOrganizationModel caller, long recurrenceId, bool loadUsers) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					var rocks = s.QueryOver<RockModel>();
					if (loadUsers)
						rocks = rocks.Fetch(x => x.AccountableUser).Eager;

					var userIds = L10Accessor.GetL10Recurrence(s, perms, recurrenceId, true)._DefaultAttendees.Select(x => x.User.Id).ToList();
					if (caller.Organization.Settings.OnlySeeRocksAndScorecardBelowYou) {
						userIds = DeepAccessor.Users.GetSubordinatesAndSelf(s, caller, caller.Id).Intersect(userIds).ToList();
					}
					return rocks.Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.AccountableUser.Id).IsIn(userIds).List().ToList();
				}
			}
		}
		public static List<RockModel> GetArchivedRocks(UserOrganizationModel caller, long userId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).EditUserDetails(caller.Id);

					var archived = s.QueryOver<RockModel>().Where(x => x.Archived == true && x.AccountableUser.Id == userId).List().ToList();
					return archived;
				}
			}
		}

		public static Csv Listing(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					// var p = s.Get<PeriodModel>(period);

					PermissionsUtility.Create(s, caller).ManagingOrganization(organizationId);



					var rocksQ = s.QueryOver<RockModel>()
						.Where(x => (x.DeleteTime == null || x.Archived) && x.OrganizationId == organizationId);
					//if (!caller.ManagingOrganization && !caller.IsRadialAdmin){
					//    var subs = DeepAccessor.Users.GetSubordinatesAndSelf(s, caller, caller.Id);
					//    rocksQ= rocksQ.WhereRestrictionOn(x=>x.ForUserId).IsIn(subs);
					//}
					var rocks = rocksQ.List().ToList();

					var csv = new Csv();
					csv.SetTitle(caller.Organization.Settings.RockName);

					foreach (var r in rocks) {
						csv.Add(r.Rock, "Owner", r.AccountableUser.GetName());
						//csv.Add(r.Rock, "Manager", string.Join(" & ", r.AccountableUser.ManagedBy.Select(x => x.Manager.GetName())));
						csv.Add(r.Rock, "Status", "" + r.Completion);
						csv.Add(r.Rock, "CreateTime", "" + r.CreateTime);
						csv.Add(r.Rock, "CompleteTime", "" + r.CompleteTime);
						csv.Add(r.Rock, "Archived", "" + r.Archived);
					}
					return csv;
				}
			}
		}

		#endregion

		public static async Task<RockModel> CreateRock(UserOrganizationModel caller, long ownerId, string message = null, long? templateId = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var rock = await CreateRock(s, perms, ownerId, message, templateId);
					tx.Commit();
					s.Flush();
					return rock;
				}
			}
		}

		public static async Task<RockModel> CreateRock(ISession s, PermissionsUtility perms, long ownerId, string message = null, long? templateId = null) {

			perms.CreateRocksForUser(ownerId);
			var owner = s.Get<UserOrganizationModel>(ownerId);


			var category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.EVALUATION);
			var rock = new RockModel() {
				CreateTime = DateTime.UtcNow,
				OrganizationId = owner.Organization.Id,
				Category = category,
				OnlyAsk = AboutType.Self,
				ForUserId = ownerId,
				AccountableUser = s.Load<UserOrganizationModel>(ownerId),
				Rock = message,
				FromTemplateItemId = templateId
			};

			s.Save(rock);

			await HooksRegistry.Each<IRockHook>((ss, x) => x.CreateRock(ss, rock));

			return rock;
		}

		public static async Task UpdateRock(UserOrganizationModel caller, long rockId, string message = null, long? ownerId = null, RockState? completion = null, DateTime? dueDate = null, DateTime? now = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					await UpdateRock(s, perms,rockId,  message, ownerId,completion,dueDate,DateTime.UtcNow);
					tx.Commit();
					s.Flush();
				}
			}
		}


		public static async Task UpdateRock(ISession s, PermissionsUtility perms, long rockId, string message = null, long? ownerId = null, RockState? completion = null, DateTime? dueDate = null, DateTime? now = null) {

			SyncUtil.EnsureStrictlyAfter(perms.GetCaller(), s, SyncAction.UpdateRockCompletion(rockId));

			perms.EditRock(rockId);

			now = now ?? DateTime.UtcNow;

			var rock = s.Get<RockModel>(rockId);
			rock.Name = message ?? rock.Name;
			if (ownerId != null && rock.ForUserId != ownerId) {
				rock.AccountableUser = s.Load<UserOrganizationModel>(ownerId.Value);
				rock.ForUserId = ownerId.Value;
			}


			rock.DueDate = dueDate ?? rock.DueDate;

			if (completion != null) {
				if (completion != RockState.Indeterminate && rock.Completion != completion) {
					if (completion == RockState.Complete)
						rock.CompleteTime = now;
					rock.Completion = completion.Value;
				} else if ((completion == RockState.Indeterminate) && rock.Completion != RockState.Indeterminate) {
					rock.Completion = RockState.Indeterminate;
					rock.CompleteTime = null;
				}
			}

			s.Update(rock);

			await HooksRegistry.Each<IRockHook>((ss, x) => x.UpdateRock(ss, rock));
		}
		

		public static async Task ArchiveRock(UserOrganizationModel caller, long rockId, DateTime? now = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					await ArchiveRock(s, perms, rockId, now);
					tx.Commit();
					s.Flush();
				}
			}
		}
		[Untested("Vto_Rocks", "EditRock cached correctly?")]
		public static async Task ArchiveRock(ISession s, PermissionsUtility perm, long rockId, DateTime? now = null) {
			perm.EditRock(rockId);
			var rock = s.Get<RockModel>(rockId);
			rock.Archived = true;
			rock.DeleteTime = now ?? DateTime.UtcNow;
			s.Update(rock);
#pragma warning disable CS0472 // The result of the expression is always the same since a value of this type is never equal to 'null'
			if (rock.ForUserId != null) {
				s.Flush();
				s.GetFresh<UserOrganizationModel>(rock.ForUserId).UpdateCache(s);
			}
#pragma warning restore CS0472 // The result of the expression is always the same since a value of this type is never equal to 'null'
			await HooksRegistry.Each<IRockHook>((ss, x) => x.ArchiveRock(s, rock, false));
		}
		public static void UndeleteRock(UserOrganizationModel caller, long rockId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var rock = s.Get<RockModel>(rockId);
					var perm = PermissionsUtility.Create(s, caller).EditRock(rock.Id);
					rock.DeleteTime = null;
					s.Update(rock);
					tx.Commit();
					s.Flush();
				}
			}
		}


		[Untested("EditRocks", "AttachRock", "Does this correctly add to L10", "Does this correctly add to VTO", "Remove the Company Rock flag")]
		public static async Task<List<PermissionsException>> EditRocks(UserOrganizationModel caller, long userId, List<RockModel> rocks, bool updateOutstandingReviews, bool updateAllL10s) {
			var output = new List<PermissionsException>();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					if (rocks.Any(x => x.ForUserId != userId))
						throw new PermissionsException("Rock UserId does not match UserId");

					var perm = PermissionsUtility.Create(s, caller);
					var user = s.Get<UserOrganizationModel>(userId);

					long orgId = -1;

					perm.EditQuestionForUser(userId);
					orgId = user.Organization.Id;

					//s.SaveOrUpdate(user);

					List<ReviewsModel> outstanding = null;
					if (updateOutstandingReviews)
						outstanding = ReviewAccessor.OutstandingReviewsForOrganization_Unsafe(s, orgId);

					List<L10Recurrence.L10Recurrence_Attendee> allL10s = null;
					if (updateAllL10s)
						allL10s = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
							.Where(x => x.DeleteTime == null && x.User.Id == userId)
							.List()
							.Where(x => x.L10Recurrence.DeleteTime == null)
							.ToList();


					var category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.EVALUATION);

					foreach (var r in rocks) {
						r.OnlyAsk = AboutType.Self; //|| AboutType.Manager; 
						r.Category = category;
						r.OrganizationId = orgId;
						r.Period = r.PeriodId == null ? null : s.Get<PeriodModel>(r.PeriodId);
						r.AccountableUser = s.Load<UserOrganizationModel>(r.ForUserId);
						var added = r.Id == 0;
						if (added) {
							s.Save(r);
							await HooksRegistry.Each<IRockHook>((ses, x) => x.CreateRock(ses, r));
						} else {
							if (r.DeleteTime != null && r.Archived) {
								await ArchiveRock(s, perm, r.Id, r.DeleteTime);
							} else {
								s.Merge(r);
								await HooksRegistry.Each<IRockHook>((ses, x) => x.UpdateRock(ses, r));
							}
						}

						if (updateOutstandingReviews && added) {
							var r1 = r;
							foreach (var o in outstanding/*.Where(x => x.PeriodId == r1.PeriodId)*/) {
								ReviewAccessor.AddResponsibilityAboutUserToReview(s, perm, o.Id, new Reviewee(userId, null), r.Id);
							}
						}
						if (updateAllL10s && added) {
							var r1 = r;
							foreach (var o in allL10s.Select(x => x.L10Recurrence)) {
								if (o.OrganizationId != caller.Organization.Id)
									throw new PermissionsException("Cannot access the Level 10");
								perm.UnsafeAllow(PermItem.AccessLevel.View, PermItem.ResourceType.L10Recurrence, o.Id);
								perm.UnsafeAllow(PermItem.AccessLevel.Edit, PermItem.ResourceType.L10Recurrence, o.Id);

								await L10Accessor.AttachRock(s, perm, o.Id, r1.Id, false);

								//await L10Accessor.AddExistingRockToL10(s, perm, o.Id, r1);
								r1._AddedToL10 = false;
								r1._AddedToVTO = false;
							}
						}
					}
					user.UpdateCache(s);

					tx.Commit();
					s.Flush();
					return output;
				}
			}
		}


		#region Deleted


		//public static void DeleteRock(UserOrganizationModel caller, long rockId) {
		//	using (var s = HibernateSession.GetCurrentSession()) {
		//		using (var tx = s.BeginTransaction()) {
		//			var rock = s.Get<RockModel>(rockId);
		//			var perm = PermissionsUtility.Create(s, caller).EditRock(rock.Id);
		//			rock.DeleteTime = DateTime.UtcNow;
		//			s.Update(rock);
		//			tx.Commit();
		//			s.Flush();
		//		}
		//	}
		//}

		//[Untested("Vto_Rocks")]
		//[Obsolete("Remove this method",true)]
		//public static void EditCompanyRocks(ISession s, PermissionsUtility perm, long organizationId, List<RockModel> rocks) {
		//	if (rocks.Any(x => x.OrganizationId != organizationId))
		//		throw new PermissionsException("Rock OrgId does not match OrgId");

		//	//var user = s.Get<UserOrganizationModel>(userId);
		//	var org = s.Get<OrganizationModel>(organizationId);

		//	long orgId = -1;

		//	perm.ManagingOrganization(organizationId);
		//	orgId = org.Id;

		//	var ar = SetUtility.AddRemove(OrganizationAccessor.GetAllUserOrganizationIds(s, perm, organizationId), rocks.Select(x => x.ForUserId));
		//	if (ar.AddedValues.Any())
		//		throw new PermissionsException("User does not belong to organization");

		//	var category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.EVALUATION);

		//	foreach (var r in rocks) {
		//		r.AccountableUser = s.Load<UserOrganizationModel>(r.ForUserId);
		//		r.OnlyAsk = AboutType.Self; //|| AboutType.Manager; 
		//		r.Category = category;
		//		r.OrganizationId = orgId;
		//		r.Period = s.Get<PeriodModel>(r.PeriodId);
		//		r.CompanyRock = true;
		//		s.SaveOrUpdate(r);
		//	}
		//	/*
		//	var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
		//	var vtoIds = s.QueryOver<VtoModel>().Where(x => x.Organization.Id == organizationId).Select(x => x.Id).List<long>();
		//	foreach (var vtoId in vtoIds)
		//	{
		//		var group = hub.Clients.Group(VtoHub.GenerateVtoGroupId(vtoId));
		//		var vto = s.Get<VtoModel>(vtoId);
		//		group.update(new AngularQuarterlyRocks(vto.QuarterlyRocks.Id)
		//		{
		//			Rocks = AngularList.Create(AngularListType.ReplaceAll, AngularVtoRock.Create(rocks.t))
		//		});
		//	}*/
		//}
		//[Obsolete("Use the L10Accessor", true)]
		//public static void EditRock(UserOrganizationModel caller, long rockId, string name = null, long? ownerId = null, RockState? completion = null, DateTime? dueDate = null, bool? companyRock = null) {
		//}
		/*
		public static void EditRock(UserOrganizationModel caller, long rockId, string name=null, long? ownerId = null, RockState? completion = null, DateTime? dueDate = null, bool? companyRock = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create()) {
						var perm = PermissionsUtility.Create(s, caller);
						perm.EditRock(rockId);

						var rock = s.Get<RockModel>(rockId);

						rock.Rock = name ?? rock.Rock;
						rock.Completion = completion ?? rock.Completion;
						rock.DueDate = dueDate ?? rock.DueDate;
						rock.CompanyRock = companyRock ?? rock.CompanyRock;
						if (ownerId != null) {
							perm.EditUserDetails(ownerId.Value);
							rock.AccountableUser = s.Load<UserOrganizationModel>(ownerId.Value);
						}

						s.Update(rock);


						var recurrenceIds = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
							.Where(x => x.DeleteTime == null && x.ForRock.Id == rockId)
							.Select(x => x.L10Recurrence.Id).List<long>();

						rt.UpdateOrganization(rock.Id)

						tx.Commit();
						s.Flush();
					}
				}
			}
		}*/

		//[Obsolete("Remove",true)]
		//public static void EditCompanyRocks(UserOrganizationModel caller, long organizationId, List<RockModel> rocks) {
		//	using (var s = HibernateSession.GetCurrentSession()) {
		//		using (var tx = s.BeginTransaction()) {
		//			var perm = PermissionsUtility.Create(s, caller);
		//			EditCompanyRocks(s, perm, organizationId, rocks);
		//			tx.Commit();
		//			s.Flush();
		//		}
		//	}
		//}

		//[Obsolete("Use other method",true)]
		//private static async Task<RockModel> CreateRock(UserOrganizationModel caller, string name, long userId) {
		//	using (var s = HibernateSession.GetCurrentSession()) {
		//		using (var tx = s.BeginTransaction()) {

		//			var perm = PermissionsUtility.Create(s, caller);
		//			var user = s.Get<UserOrganizationModel>(userId);

		//			long orgId = -1;

		//			perm.EditQuestionForUser(userId);
		//			orgId = user.Organization.Id;



		//			var category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.EVALUATION);
		//			var r = new RockModel() {
		//				OnlyAsk = AboutType.Self,//|| AboutType.Manager; 
		//				Category = category,
		//				OrganizationId = orgId,
		//				Period = null,
		//				AccountableUser =user,
		//				ForUserId = userId
		//			};
		//			s.Save(r);
		//			await HooksRegistry.Each<IRockHook>(x => x.CreateRock(s, r));

		//			user.UpdateCache(s);

		//			tx.Commit();
		//			s.Flush();
		//			return r;
		//		}
		//	}
		//}
		#endregion
	}
}
