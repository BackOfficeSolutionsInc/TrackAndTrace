using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using Amazon.EC2.Model;

using FluentNHibernate.Conventions;
using ImageResizer.Configuration.Issues;
using MathNet.Numerics;
using Microsoft.AspNet.SignalR;
using NHibernate.Criterion;
using NHibernate.Linq;
using NHibernate.Transform;
using RadialReview.Accessors.TodoIntegrations;
using RadialReview.Controllers;
using RadialReview.Exceptions;
using RadialReview.Exceptions.MeetingExceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Angular.Issues;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Application;
using RadialReview.Models.Askables;
using RadialReview.Models.Audit;
using RadialReview.Models.Components;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.L10.AV;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Permissions;
using RadialReview.Models.Scheduler;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Todo;
using RadialReview.Models.ViewModels;
using RadialReview.Properties;
using RadialReview.Utilities;
using NHibernate;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Synchronize;
//using ListExtensions = WebGrease.Css.Extensions.ListExtensions;
using RadialReview.Models.Enums;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Angular.Base;
//using System.Web.WebPages.Html;
using RadialReview.Models.VTO;
using RadialReview.Models.Angular.VTO;
using RadialReview.Utilities.RealTime;
using RadialReview.Models.Periods;
using RadialReview.Models.Interfaces;
using System.Dynamic;
using Newtonsoft.Json;
using RadialReview.Models.Angular.Headlines;
using RadialReview.Models.VideoConference;
using System.Linq.Expressions;
using NHibernate.SqlCommand;
using RadialReview.Models.Rocks;
using RadialReview.Models.Angular.Rocks;
using System.Web.Mvc;
using RadialReview.Hooks;
using RadialReview.Utilities.Hooks;
using static RadialReview.Utilities.EventUtil;
using Twilio;
using Twilio.Types;
using Twilio.Rest.Api.V2010.Account;
using RadialReview.Accessors;
using RadialReview.Models.UserModels;
using static RadialReview.Models.L10.L10Recurrence;

namespace RadialReview.Accessors {
	public partial class L10Accessor : BaseAccessor {

		#region Edit Meeting		
		public static async Task EditL10Recurrence(UserOrganizationModel caller, L10Recurrence l10Recurrence) {
			bool wasCreated = false;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller);
					if (l10Recurrence.Id == 0) {
						perm.CreateL10Recurrence(caller.Organization.Id);
						l10Recurrence.CreatedById = caller.Id;
						wasCreated = true;
						if (l10Recurrence.TeamType == L10TeamType.LeadershipTeam) {
							await Trigger(x => x.Create(s, EventType.CreateLeadershipMeeting, caller, l10Recurrence, message: l10Recurrence.Name));
						} else if (l10Recurrence.TeamType == L10TeamType.DepartmentalTeam) {
							await Trigger(x => x.Create(s, EventType.CreateDepartmentMeeting, caller, l10Recurrence, message: l10Recurrence.Name));
						}
						await Trigger(x => x.Create(s, EventType.CreateMeeting, caller, l10Recurrence, message: l10Recurrence.Name));

					} else
						perm.AdminL10Recurrence(l10Recurrence.Id);

					//s.UpdateLists(l10Recurrence,DateTime.UtcNow,x=>x.DefaultAttendees,x=>x.DefaultMeasurables);
					/*if (l10Recurrence.Id != 0){
                        var old = s.Get<L10Recurrence>(l10Recurrence.Id);
                        //SetUtility.AddRemove(old.DefaultAttendees,l10Recurrence.DefaultAttendees,x=>x.)
                    }*/
					var oldRecur = s.Get<L10Recurrence>(l10Recurrence.Id);
					_LoadRecurrences(s, new LoadMeeting() {
                        LoadMeasurables = true,
                        LoadRocks = true,
                        LoadUsers  = true,                        
                    }, oldRecur);

					var oldMeeting = _GetCurrentL10Meeting(s, perm, l10Recurrence.Id, true, true);
					SetUtility.AddedRemoved<MeasurableModel> updateMeasurables = null;
					VtoModel vto = null;
					if (oldMeeting != null) {
						updateMeasurables = SetUtility.AddRemove(oldMeeting._MeetingMeasurables.Where(x => !x.IsDivider).Select(x => x.Measurable), l10Recurrence._DefaultMeasurables.Select(x => x.Measurable), x => x.Id);
						var updateableMeasurables = ScorecardAccessor.GetVisibleMeasurables(s, perm, l10Recurrence.OrganizationId, false);
						if (!updateMeasurables.AddedValues.All(x => updateableMeasurables.Any(y => y.Id == x.Id)))
							throw new PermissionsException("You do not have access to add one or more measurables.");
						if (oldMeeting.L10Recurrence.VtoId != 0) {
							vto = s.Get<VtoModel>(oldMeeting.L10Recurrence.VtoId);
						}
					}
					SetUtility.AddedRemoved<RockModel> updateRocks = null;
					if (oldMeeting != null) {
						updateRocks = SetUtility.AddRemove(
							oldMeeting._MeetingRocks.Select(x => x.ForRock),
							l10Recurrence._DefaultRocks.Select(x => x.ForRock),
							x => x.Id);

						var updatedRocks = RockAccessor.GetAllVisibleRocksAtOrganization(s, perm, l10Recurrence.OrganizationId, false);
						if (!updateRocks.AddedValues.All(x => updatedRocks.Any(y => y.Id == x.Id)))
							throw new PermissionsException("You do not have access to add one or more rock.");
					}

					SetUtility.AddedRemoved<UserOrganizationModel> updateAttendees = null;
					if (oldMeeting != null) {
						updateAttendees = SetUtility.AddRemove(
							oldMeeting._MeetingAttendees.Select(x => x.User),
							l10Recurrence._DefaultAttendees.Select(x => x.User),
							x => x.Id);
					}

					var now = DateTime.UtcNow;

					if (oldRecur != null) {
						foreach (var m in l10Recurrence._DefaultMeasurables) {
							m._Ordering = oldRecur._DefaultMeasurables.FirstOrDefault(x => x.Measurable != null && m.Measurable != null && x.Measurable.Id == m.Measurable.Id).NotNull(x => x._Ordering);
						}
					}


					// match up attendees, measureables, and rocks
					// 

					l10Recurrence._DefaultAttendees.ToList().ForEach(a => {
						if (oldRecur != null)
							a.Id = oldRecur._DefaultAttendees.FirstOrDefault(x => x.User.Id == a.User.Id).NotNull(x => x.Id);
					});
					l10Recurrence._DefaultRocks.ToList().ForEach(a => {
						if (oldRecur != null)
							a.Id = oldRecur._DefaultRocks.FirstOrDefault(x => x.ForRock.Id == a.ForRock.Id).NotNull(x => x.Id);
					});
					l10Recurrence._DefaultMeasurables.ToList().ForEach(a => {
						if (oldRecur != null) {
							var found = oldRecur._DefaultMeasurables.FirstOrDefault(x => ((x.Measurable == null && a.Measurable == null) || (x.Measurable != null && a.Measurable != null && x.Measurable.Id == a.Measurable.Id)) && !x._Used);
							if (found != null) {
								a.Id = found.Id;
								found._Used = true;
							}
						}
					});


					//Update new measurablse, attendees, rocks

					s.UpdateList(oldRecur.NotNull(x => x._DefaultAttendees), l10Recurrence._DefaultAttendees, now);
					s.UpdateList(oldRecur.NotNull(x => x._DefaultMeasurables.Where(y => !y.IsDivider)), l10Recurrence._DefaultMeasurables, now);
					s.UpdateList(oldRecur.NotNull(x => x._DefaultRocks), l10Recurrence._DefaultRocks, now);


					l10Recurrence.CreateTime = oldRecur.CreateTime;

					/////////////
					//Update rocks on the VTO also

					//we need to make sure to set this or the rocks is duplicated.
					foreach (var r in l10Recurrence._DefaultRocks)
						r.ForRock._AddedToL10 = true;


					//if (oldRecur != null && vto != null) {

					//    var updateRocksRecur = SetUtility.AddRemove(oldRecur._DefaultRocks.Select(y => y.ForRock), l10Recurrence._DefaultRocks.Select(y => y.ForRock), x => x.Id);

					//    foreach (var a in updateRocksRecur.AddedValues) {
					//        await VtoAccessor.AddRock(s, perm, vto.Id, a);
					//    }
					//    foreach (var a in updateRocksRecur.RemovedValues) {
					//        var vtoRocks = s.QueryOver<Vto_Rocks>().Where(x => x.Vto.Id == vto.Id && x.Rock.Id == a.Id && x.DeleteTime == null).List().ToList();

					//        foreach (var r in vtoRocks) {
					//            r.DeleteTime = now;
					//            s.Update(r);
					//        }
					//    }
					//}
					////////////


					s.Evict(oldRecur);

					if (l10Recurrence.ForumCode != null) {
						l10Recurrence.ForumCode = l10Recurrence.ForumCode.ToLower();
						var any = 0 != s.QueryOver<L10Recurrence>().Where(x => x.DeleteTime == null && l10Recurrence.ForumCode == x.ForumCode && x.Id != l10Recurrence.Id).RowCount();
						if (any) {
							l10Recurrence.ForumCode = null;
						}
					}


					s.SaveOrUpdate(l10Recurrence);

					if (wasCreated) {
						vto = VtoAccessor.CreateRecurrenceVTO(s, perm, l10Recurrence.Id);

						s.Save(new PermItem() {
							CanAdmin = true,
							CanEdit = true,
							CanView = true,
							AccessorType = PermItem.AccessType.Creator,
							AccessorId = caller.Id,
							ResType = PermItem.ResourceType.L10Recurrence,
							ResId = l10Recurrence.Id,
							CreatorId = caller.Id,
							OrganizationId = caller.Organization.Id,
							IsArchtype = false,
						});
						s.Save(new PermItem() {
							CanAdmin = true,
							CanEdit = true,
							CanView = true,
							AccessorType = PermItem.AccessType.Members,
							AccessorId = -1,
							ResType = PermItem.ResourceType.L10Recurrence,
							ResId = l10Recurrence.Id,
							CreatorId = caller.Id,
							OrganizationId = caller.Organization.Id,
							IsArchtype = false,
						});
						s.Save(new PermItem() {
							CanAdmin = true,
							CanEdit = true,
							CanView = true,
							AccessorType = PermItem.AccessType.Admins,
							AccessorId = -1,
							ResType = PermItem.ResourceType.L10Recurrence,
							ResId = l10Recurrence.Id,
							CreatorId = caller.Id,
							OrganizationId = caller.Organization.Id,
							IsArchtype = false,
						});
					}



					if (updateMeasurables != null) {
						//Add new values.. probably shouldn't remove stale ones..
						foreach (var a in updateMeasurables.AddedValues) {
							s.Save(new L10Meeting.L10Meeting_Measurable() {
								L10Meeting = oldMeeting,
								Measurable = a,
							});

						}
						foreach (var a in updateMeasurables.RemovedValues) {
							if (a.Id > 0) { //Todo Completion is -10001
								var o = oldMeeting._MeetingMeasurables.First(x => x.Measurable != null && x.Measurable.Id == a.Id);
								if (!o.IsDivider) {
									o.DeleteTime = now;
									s.Update(o);
								}
							}
						}
					}
					if (updateRocks != null) {
						//Add new values.. probably shouldn't remove stale ones..
						foreach (var a in updateRocks.AddedValues) {
							s.Save(new L10Meeting.L10Meeting_Rock() {
								L10Meeting = oldMeeting,
								ForRock = a,
								ForRecurrence = oldMeeting.L10Recurrence,
								VtoRock = false,
							});



						}
						foreach (var a in updateRocks.RemovedValues) {
							var o = oldMeeting._MeetingRocks.First(x => x.ForRock != null && x.ForRock.Id == a.Id);
							o.DeleteTime = now;
							s.Update(o);
						}
					}

					if (updateAttendees != null) {
						//Add new values.. probably shouldn't remove stale ones..
						foreach (var a in updateAttendees.AddedValues) {
							s.Save(new L10Meeting.L10Meeting_Attendee() {
								L10Meeting = oldMeeting,
								User = a,
							});
						}
						foreach (var a in updateAttendees.RemovedValues) {
							var o = oldMeeting._MeetingAttendees.First(x => x.User != null && x.User.Id == a.Id);
							o.DeleteTime = now;
							s.Update(o);
						}
					}

					Audit.L10Log(s, caller, l10Recurrence.Id, "EditL10Recurrence", ForModel.Create(l10Recurrence));

					tx.Commit();
					s.Flush();
				}
			}

		}
		public static string UpdatePage(UserOrganizationModel caller, long forUserId, long recurrenceId, string pageName, string connection) {
			string pageType = null;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var meeting = _GetCurrentL10Meeting(s, perms, recurrenceId, true, false, true);
					if (meeting == null)
						return pageType;
					//if (caller.Id != meeting.MeetingLeader.Id)	return;


					var forUser = s.Get<UserOrganizationModel>(forUserId);
					if (meeting.MeetingLeaderId == 0) {
						meeting.MeetingLeaderId = forUser.Id;
						meeting.MeetingLeader = forUser;
					}

					if (caller.Id != forUserId)
						PermissionsUtility.Create(s, forUser).ViewL10Meeting(meeting.Id);

					var log = _GetCurrentLog(s, caller, meeting.Id, forUserId, true);

					var now = DateTime.UtcNow;
					var addNew = true;
					if (log != null) {
						addNew = log.Page != pageName;
						if (addNew) {
							log.EndTime = now;//new DateTime(Math.Min(log.StartTime.AddMinutes(1).Ticks,now.Ticks));
							s.Update(log);
						}
					}

					if (addNew) {
						var newLog = new L10Meeting.L10Meeting_Log() {
							User = forUser,
							StartTime = now,
							L10Meeting = meeting,
							Page = pageName,
						};

						s.Save(newLog);



						if (meeting.MeetingLeader.NotNull(x => x.Id) == forUserId) {
							if (log != null) {
								//Add additional minutes from current page
								var cur = meeting._MeetingLeaderPageDurations.FirstOrDefault(x => x.Item1 == log.Page);
								var duration = (log.EndTime.Value - log.StartTime).TotalMinutes;
								if (cur == null) {
									meeting._MeetingLeaderPageDurations.Add(Tuple.Create(log.Page, duration));
								} else {
									for (var i = 0; i < meeting._MeetingLeaderPageDurations.Count; i++) {
										var x = meeting._MeetingLeaderPageDurations[i];
										if (x.Item1 == log.Page) {
											meeting._MeetingLeaderPageDurations[i] = Tuple.Create(x.Item1, x.Item2 + duration);
										}
									}
								}
							}
							var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
							var meetingHub = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(meeting));
							var baseMins = meeting._MeetingLeaderPageDurations.SingleOrDefault(x => x.Item1 == pageName).NotNull(x => x.Item2);
							meetingHub.setCurrentPage(pageName.ToLower(), now.ToJavascriptMilliseconds(), baseMins/*, pageType*/);

							meetingHub.update(new AngularMeeting(recurrenceId) { CurrentPage = pageName });

							foreach (var a in meeting._MeetingLeaderPageDurations) {
								if (a.Item1 != pageName) {
									meetingHub.setPageTime(a.Item1, a.Item2);
								}
							}
						}

					}

					var p = pageName;
					if (!string.IsNullOrEmpty(p))
						p = p.ToUpper()[0] + p.Substring(1);

					long pageId;
					var friendlyPageName = p;
					pageType = p;
					if (long.TryParse(pageName.SubstringAfter("-"), out pageId)) {
						try {
#pragma warning disable CS0618 // Type or member is obsolete
							var l10Page = GetPage(s, perms, pageId);
#pragma warning restore CS0618 // Type or member is obsolete
							friendlyPageName = l10Page.Title;
							pageType = l10Page.PageTypeStr;
						} catch (Exception) {

						}
					}

					Audit.L10Log(s, caller, recurrenceId, "UpdatePage", ForModel.Create(meeting), friendlyPageName);
					tx.Commit();
					s.Flush();
					return pageType;
				}
			}
		}
		public static async Task DeleteL10Recurrence(UserOrganizationModel caller, long recurrenceId) {
			L10Recurrence r;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).AdminL10Recurrence(recurrenceId);
					r = s.Get<L10Recurrence>(recurrenceId);
					if (r.DeleteTime != null) {
						throw new PermissionsException();
					}

					r.DeleteTime = DateTime.UtcNow;
					s.Update(r);


					Audit.L10Log(s, caller, recurrenceId, "DeleteL10", ForModel.Create(r), r.Name);
					tx.Commit();
					s.Flush();
				}
			}
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					await EventUtil.Trigger(x => x.Create(s, EventType.DeleteMeeting, caller, r, message: r.Name + "(Deleted)"));
					await HooksRegistry.Each<IMeetingEvents>((ses, x) => x.DeleteRecurrence(ses, r));
					tx.Commit();
					s.Flush();
				}
			}

		}

		public static async Task UndeleteL10Recurrence(UserOrganizationModel caller, long recurrenceId) {
			L10Recurrence r;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).AdminL10Recurrence(recurrenceId);
					r = s.Get<L10Recurrence>(recurrenceId);
					r.DeleteTime = null;
					s.Update(r);
					Audit.L10Log(s, caller, recurrenceId, "UndeleteL10", ForModel.Create(r), r.Name);
					tx.Commit();
					s.Flush();
				}
			}
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					await EventUtil.Trigger(x => x.Create(s, EventType.UndeleteMeeting, caller, r, message: r.Name + "(Undeleted)"));
					await HooksRegistry.Each<IMeetingEvents>((ses, x) => x.UndeleteRecurrence(ses, r));
					tx.Commit();
					s.Flush();
				}
			}
		}
		public static async Task DeleteL10Meeting(UserOrganizationModel caller, long meetingId) {
			L10Meeting meeting;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					meeting = s.Get<L10Meeting>(meetingId);
					PermissionsUtility.Create(s, caller).AdminL10Recurrence(meeting.L10RecurrenceId);
					meeting.DeleteTime = DateTime.UtcNow;
					s.Update(meeting);
					tx.Commit();
					s.Flush();
				}
			}
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					await HooksRegistry.Each<IMeetingEvents>((ses, x) => x.DeleteMeeting(ses, meeting));
					tx.Commit();
					s.Flush();
				}
			}
		}
		public static L10Meeting EditMeetingTimes(UserOrganizationModel caller, long meetingId, string startOrEnd, DateTime? time) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var meeting = s.Get<L10Meeting>(meetingId);
					PermissionsUtility.Create(s, caller).AdminL10Recurrence(meeting.L10RecurrenceId);
					switch (startOrEnd.ToLower()) {
						case "start": { meeting.StartTime = time ?? meeting.StartTime; break; }
						case "end": {
								if (meeting.CompleteTime == null)
									throw new PermissionsException("Meeting has not been concluded.");
								if (time == null)
									throw new PermissionsException("You must specify an end time.");
								meeting.CompleteTime = time;
								break;
							}
						default:
							break;
					}
					s.Update(meeting);

					//EventUtil.Trigger(x => x.Create(s, EventType.DeleteMeeting, caller, r, message: r.Name + "(Deleted)"));
					//Audit.L10Log(s, caller, recurrenceId, "DeleteL10", ForModel.Create(r), r.Name);

					tx.Commit();
					s.Flush();
					return meeting;
				}
			}
		}
		#endregion
	}
}