using System;
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

		#region Video

		public static void SetVideoProvider(UserOrganizationModel caller, long recurrenceId, long vcProviderId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create()) {
						var perms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

						var found = s.Get<AbstractVCProvider>(vcProviderId);
						if (found.DeleteTime != null)
							throw new PermissionsException("Video Provider does not exist");
						perms.ViewUserOrganization(found.OwnerId, false);

						var user = s.Get<UserOrganizationModel>(found.OwnerId);
						if (user.DeleteTime != null)
							throw new PermissionsException("Owner of the Video Conference Provider no longer exists");

						found.LastUsed = DateTime.UtcNow;
						s.Update(found);

						var l10Meeting = _GetCurrentL10Meeting(s, perms, recurrenceId, true);
						if (l10Meeting != null) {
							l10Meeting.SelectedVideoProvider = found;

							s.Update(l10Meeting);
						}

						rt.UpdateRecurrences(recurrenceId)
						  .AddLowLevelAction(x => {
							  var resolved = (AbstractVCProvider)s.GetSessionImplementation().PersistenceContext.Unproxy(found);
							  x.setSelectedVideoProvider(resolved);
						  });

						tx.Commit();
						s.Flush();
					}
				}
			}
		}

		public static void SetJoinedVideo(UserOrganizationModel caller, long userId, long recurrenceId, long vcProviderId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create()) {
						var perms = PermissionsUtility.Create(s, caller)
							.ViewL10Recurrence(recurrenceId)
							.Self(userId);

						var found = s.Get<AbstractVCProvider>(vcProviderId);
						if (found.DeleteTime != null)
							throw new PermissionsException("Video Provider does not exist");
						perms.ViewUserOrganization(found.OwnerId, false);

						var user = s.Get<UserOrganizationModel>(found.OwnerId);
						if (user.DeleteTime != null)
							throw new PermissionsException("Owner of the Video Conference Provider no longer exists");

						found.LastUsed = DateTime.UtcNow;
						s.Update(found);

						var link = new JoinedVideo() {
							RecurrenceId = recurrenceId,
							UserId = userId,
							VideoProvider = vcProviderId,
						};

						var recur = s.Get<L10Recurrence>(recurrenceId);
						recur.SelectedVideoProviderId = found.Id;
						s.Update(recur);

						var l10Meeting = _GetCurrentL10Meeting(s, perms, recurrenceId, true);
						if (l10Meeting != null) {
							link.MeetingId = l10Meeting.Id;
						}

						rt.UpdateRecurrences(recurrenceId).AddLowLevelAction(x => {
							x.setSelectedVideoProvider(found);
						});

						s.Save(link);

						tx.Commit();
						s.Flush();
					}
				}
			}
		}


		#endregion
	}
}
