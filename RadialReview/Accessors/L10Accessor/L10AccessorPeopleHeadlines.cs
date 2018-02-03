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

        #region PeopleHeadlines		
        public static List<PeopleHeadline> GetHeadlinesForMeeting(UserOrganizationModel caller, long recurrenceId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    return GetHeadlinesForMeeting(s, perms, recurrenceId);
                }
            }
        }
        public static List<PeopleHeadline> GetHeadlinesForMeeting(ISession s, PermissionsUtility perms, long recurrenceId, bool includeClosed = false) {
            perms.ViewL10Recurrence(recurrenceId);

            var foundQ = s.QueryOver<PeopleHeadline>().Where(x => x.DeleteTime == null && x.RecurrenceId == recurrenceId);
            if (!includeClosed)
                foundQ = foundQ.Where(x => x.CloseTime == null);

            var found = foundQ.Fetch(x => x.Owner).Eager
                                .Fetch(x => x.About).Eager
                                .List().ToList();

            foreach (var f in found) {
                if (f.Owner != null) {
                    var a = f.Owner.GetName();
                    var b = f.Owner.ImageUrl(true, ImageSize._32);
                }
                if (f.About != null) {
                    var a = f.About.GetName();
                    var b = f.About.GetImageUrl();
                }
            }
            return found;
        }

        [Obsolete("Use Headline accessor", true)]
        public static async Task UpdateHeadline(UserOrganizationModel caller, long headlineId, string message, string connectionId = null) {
            //using (var s = HibernateSession.GetCurrentSession()) {
            //	using (var tx = s.BeginTransaction()) {
            await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateHeadlineMessage(headlineId), async s => {
                var perms = PermissionsUtility.Create(s, caller);
                var headline = s.Get<PeopleHeadline>(headlineId);
                perms.EditL10Recurrence(headline.RecurrenceId);
                //SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateHeadlineMessage(headlineId));
                headline.Message = message;
                s.Update(headline);

                var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
                var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(headline.RecurrenceId), connectionId);
                group.updateHeadlineMessage(headlineId, message);

                group.update(new AngularUpdate() {
                    new AngularHeadline(headlineId) {
                        Name = message
                    }
                });
            });

            //		tx.Commit();
            //		s.Flush();
            //	}
            //}
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public static async Task RemoveHeadline(ISession s, PermissionsUtility perm, RealTimeUtility rt, long headlineId) {
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously


            perm.ViewHeadline(headlineId);

            var r = s.Get<PeopleHeadline>(headlineId);

            if (r.CloseTime != null)
                throw new PermissionsException("Headline already removed.");

            perm.EditL10Recurrence(r.RecurrenceId);

            var now = DateTime.UtcNow;
            r.CloseTime = now;
            s.Update(r);

            await HooksRegistry.Each<IHeadlineHook>((ses, x) => x.ArchiveHeadline(ses, r));
        }

        public static List<PeopleHeadline> GetAllHeadlinesForRecurrence(ISession s, PermissionsUtility perms, long recurrenceId, bool includeClosed, DateRange range) {
            perms.ViewL10Recurrence(recurrenceId);

            var headlineListQ = s.QueryOver<PeopleHeadline>().Where(x => x.DeleteTime == null && x.RecurrenceId == recurrenceId);
            if (range != null && includeClosed) {
                var st = range.StartTime.AddDays(-1);
                var et = range.EndTime.AddDays(1);
                headlineListQ = headlineListQ.Where(x => x.CloseTime == null || (x.CloseTime >= st && x.CloseTime <= et));
            }

            if (!includeClosed) {
                headlineListQ = headlineListQ.Where(x => x.CloseTime == null);
            }
            var headlineList = headlineListQ.List().ToList();
            foreach (var t in headlineList) {
                if (t.About != null) {
                    var a = t.About.GetName();
                    var b = t.About.GetImageUrl();
                }
                if (t.Owner != null) {
                    var a = t.Owner.GetName();
                    var b = t.Owner.GetImageUrl();
                }
            }
            return headlineList;
        }


        //public static void AttachHeadline(UserOrganizationModel caller, long recurrenceId, long headlineId) {
        //	using (var s = HibernateSession.GetCurrentSession()) {
        //		using (var tx = s.BeginTransaction()) {
        //			using (var rt = RealTimeUtility.Create()) {
        //				var perms = PermissionsUtility.Create(s, caller);

        //				AddPeopleHeadline(s, perms, rt, recurrenceId, headlineId);

        //				tx.Commit();
        //				s.Flush();
        //			}
        //		}
        //	}
        //}

        //public static void AddPeopleHeadline(ISession s, PermissionsUtility perm, RealTimeUtility rt, long recurrenceId, long headlineId) {
        //	perm.EditL10Recurrence(recurrenceId);

        //	perm.ViewHeadline(headlineId);

        //	var r1 = s.Get<L10Recurrence>(recurrenceId);
        //	var r = s.Get<PeopleHeadline>(headlineId);

        //	//r1.HeadlinesId = r.HeadlinePadId;
        //	//s.Update(r1); // update recurrence

        //	// need to confirm
        //	rt.UpdateRecurrences(recurrenceId).Update(
        //		new AngularRecurrence(recurrenceId) {
        //			Headlines = AngularList.CreateFrom(AngularListType.Remove, new AngularHeadline(r.Id))
        //		}
        //	);
        //}

        #endregion
    }
}