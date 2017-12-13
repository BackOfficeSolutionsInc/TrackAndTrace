﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using Amazon.EC2.Model;
using Amazon.ElasticMapReduce.Model;
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

namespace RadialReview.Accessors {
	public partial class L10Accessor : BaseAccessor {
		#region Attendees
		public static List<UserOrganizationModel> GetAttendees(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create()) {
						var perms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

						var usersRecur = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
							.Fetch(x => x.User).Eager
							.List().ToList();
						var users = usersRecur.Select(x => x.User).ToList();
						foreach (var u in users) {
							try {
								var a = u.GetName();
							} catch (Exception) {

							}
						}
						return users;
					}
				}
			}
		}

		public static void OrderAngularMeasurable(UserOrganizationModel caller, long measurableId, long recurrenceId, int oldOrder, int newOrder) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create()) {
						var perms = PermissionsUtility.Create(s, caller);
						perms.EditL10Recurrence(recurrenceId);
						perms.EditMeasurable(measurableId);

						var recurMeasureables = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
										.Where(x => x.L10Recurrence.Id == recurrenceId && x.DeleteTime == null)
										.List().ToList();
						recurMeasureables = recurMeasureables.Where(x => x.Measurable == null || x.Measurable.DeleteTime == null).ToList();

						var ctx = Reordering.Create(recurMeasureables, measurableId, recurrenceId, oldOrder, newOrder, x => x._Ordering, x => (x.Measurable == null) ? x.Id : x.Measurable.Id);
						ctx.ApplyReorder(rt, s, (id, order, item) => AngularMeasurable.Create(item));

						tx.Commit();
						s.Flush();
					}
				}
			}
		}

		public static async Task AddAttendee(UserOrganizationModel caller, long recurrenceId, long userorgid) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create()) {
						var perms = PermissionsUtility.Create(s, caller);
						await AddAttendee(s, perms, rt, recurrenceId, userorgid);
						tx.Commit();
						s.Flush();
					}

				}
			}
		}

		public static async Task AddAttendee(ISession s, PermissionsUtility perms, RealTimeUtility rt, long recurrenceId, long userorgid) {
			perms.AdminL10Recurrence(recurrenceId);
			perms.ViewUserOrganization(userorgid, false);
			var user = s.Get<UserOrganizationModel>(userorgid);
			var caller = perms.GetCaller();

			var existing = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.User.Id == userorgid && x.L10Recurrence.Id == recurrenceId).List().ToList();
			if (existing.Any())
				throw new PermissionsException("User is already an attendee.");
			var recur = s.Get<L10Recurrence>(recurrenceId);
			//recur.Pristine = false;
			await L10Accessor.Depristine_Unsafe(s, caller, recur);
			s.Update(recur);

			var attendee = new L10Recurrence.L10Recurrence_Attendee() {
				L10Recurrence = recur,
				User = user,
			};

			s.Save(attendee);

			if (caller.Organization.Settings.DisableUpgradeUsers && user.EvalOnly) {
				throw new PermissionsException("This user is set to participate in " + Config.ReviewName() + " only.");
			}

			if (user.EvalOnly) {
				perms.CanUpgradeUser(user.Id);
				user.EvalOnly = false;
				s.Update(user);
				user.UpdateCache(s);
			}

			var curr = _GetCurrentL10Meeting(s, perms, recurrenceId, true, false, false);
			if (curr != null) {
				s.Save(new L10Meeting.L10Meeting_Attendee() {
					L10Meeting = curr,
					User = user,
				});
			}

			await HooksRegistry.Each<IMeetingEvents>((ses, x) => x.AddAttendee(ses, recurrenceId, user, attendee));
		}

		public class VtoSharable {
			public bool CanShareVto { get; set; }
			public string ErrorMessage { get; set; }
		}
		public static VtoSharable IsVtoSharable(UserOrganizationModel caller, long? recurrenceId = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var orgId = caller.Organization.Id;
					var anySharable = s.QueryOver<L10Recurrence>()
										.Where(x => x.DeleteTime == null && x.OrganizationId == orgId && x.Id != recurrenceId)
										.Select(x => x.ShareVto, x => x.Name, x => x.Id)
										.List<object[]>()
										.Select(x => new {
											Shared = ((bool?)x[0]) ?? false,
											Name = (string)x[1],
											Id = (long)x[2]
										}).ToList();
					var onlyShared = anySharable.FirstOrDefault(x => x.Shared);
					var output = new VtoSharable() {
						CanShareVto = onlyShared == null,
						ErrorMessage = onlyShared.NotNull(x => "You can only share one V/TO. Unshare the V/TO associated with <a href='/l10/edit/" + x.Id + "'>" + x.Name + "</a>.")
					};
					return output;
				}
			}
		}

		//public static Task<bool> ReorderL10Recurrence(UserOrganizationModel caller,long userId, long recurrenceId, int oldOrder, int newOrder) {

		//    using (var s = HibernateSession.GetCurrentSession()) {
		//        using (var tx = s.BeginTransaction()) {
		//            var perms = PermissionsUtility.Create(s, caller);
		//            perms.Self(userId).ViewL10Recurrence(recurrenceId);

		//            var existingl10s = GetVisibleL10Meetings_Tiny(s, perms, userId, false, false);
		//            var res = s.QueryOver<L10RecurrenceOrder>().Where(x => x.UserId == userId).List().ToList();
		//            var selected = res.FirstOrDefault(x => x.RecurrenceId == recurrenceId);


		//            Reordering.Create(res, pageId, found.L10RecurrenceId, oldOrder, newOrder, x => x._Ordering, x => x.Id)
		//                      .ApplyReorder(s);


		//        }
		//    }
		//}
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public static async Task RemoveAttendee(ISession s, PermissionsUtility perms, RealTimeUtility rt, long recurrenceId, long userorgid) {
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
			perms.AdminL10Recurrence(recurrenceId);
			perms.ViewUserOrganization(userorgid, false);
			var user = s.Get<UserOrganizationModel>(userorgid);

			var existing = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.User.Id == userorgid && x.L10Recurrence.Id == recurrenceId).List().ToList();
			if (!existing.Any())
				throw new PermissionsException("User is not an attendee.");

			foreach (var e in existing) {
				e.DeleteTime = DateTime.UtcNow;
				s.Update(e);
			}

			var curr = _GetCurrentL10Meeting(s, perms, recurrenceId, true, false, false);
			if (curr != null) {
				var curAttendee = s.QueryOver<L10Meeting.L10Meeting_Attendee>().Where(x => x.DeleteTime == null && x.User.Id == userorgid && x.L10Meeting.Id == curr.Id).List().ToList();

				foreach (var e in curAttendee) {
					e.DeleteTime = DateTime.UtcNow;
					s.Update(e);
				}
			}

			await HooksRegistry.Each<IMeetingEvents>((ses, x) => x.RemoveAttendee(ses, recurrenceId, userorgid));
		}
		public static async Task RemoveAttendee(UserOrganizationModel caller, long recurrenceId, long userorgid) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create()) {
						var perms = PermissionsUtility.Create(s, caller);
						await RemoveAttendee(s, perms, rt, recurrenceId, userorgid);
						tx.Commit();
						s.Flush();
					}
				}
			}
		}


		public static long GuessUserId(IssueModel issueModel, long deflt = 0) {
			try {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						if (issueModel == null)
							return deflt;
						if (issueModel.ForModel != null && issueModel.ForModel.ToLower() == "issuemodel" && issueModel.Id == issueModel.ForModelId)
							return deflt;
						var found = GetModel_Unsafe(s, issueModel.ForModel, issueModel.ForModelId);
						if (found == null)
							return deflt;
						if (found is MeasurableModel)
							return ((MeasurableModel)found).AccountableUserId;
						if (found is TodoModel)
							return ((TodoModel)found).AccountableUserId;
						if (found is IssueModel)
							return GuessUserId((IssueModel)found, deflt);
						return deflt;
					}
				}
			} catch (Exception) {
				return deflt;
			}
		}
		#endregion

	}
}