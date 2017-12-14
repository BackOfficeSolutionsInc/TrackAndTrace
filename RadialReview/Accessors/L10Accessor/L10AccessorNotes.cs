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

		#region Notes
		public static L10Note GetNote(UserOrganizationModel caller, long noteId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewL10Note(noteId);
					return s.Get<L10Note>(noteId);
				}
			}
		}
		public static List<L10Note> GetVisibleL10Notes_Unsafe(List<long> recurrences) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var notes = s.QueryOver<L10Note>().Where(x => x.DeleteTime == null)
						.WhereRestrictionOn(x => x.Recurrence).IsIn(recurrences.ToArray())
						.List().ToList();
					return notes;
				}
			}
		}
		public static string CreateNote(UserOrganizationModel caller, long recurrenceId, string name) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					var note = new L10Note() {
						Name = name,
						Contents = "",
						Recurrence = s.Load<L10Recurrence>(recurrenceId)
					};
					s.Save(note);
					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));
					group.createNote(note.Id, name);
					var rec = new AngularRecurrence(recurrenceId) {
						Notes = new List<AngularMeetingNotes>(){
							new AngularMeetingNotes(note)
						}
					};
					group.update(rec);

					Audit.L10Log(s, caller, recurrenceId, "CreateNote", ForModel.Create(note), name);
					tx.Commit();
					s.Flush();
					return note.PadId;
				}
			}
		}
		public static void EditNote(UserOrganizationModel caller, long noteId, /*string contents = null,*/ string name = null, string connectionId = null, bool? delete = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var note = s.Get<L10Note>(noteId);
					PermissionsUtility.Create(s, caller).EditL10Recurrence(note.Recurrence.Id);
					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					var now = DateTime.UtcNow;
					//if (contents != null) {
					//    note.Contents = contents;
					//    hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(note.Recurrence.Id), connectionId).updateNoteContents(noteId, contents, now.ToJavascriptMilliseconds());
					//}
					if (name != null) {
						note.Name = name;
						hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(note.Recurrence.Id), connectionId).updateNoteName(noteId, name);
					}
					_ProcessDeleted(s, note, delete);
					s.Update(note);
					Audit.L10Log(s, caller, note.Recurrence.Id, "EditNote", ForModel.Create(note), note.Name + ":\n" + note.Contents);
					tx.Commit();
					s.Flush();
				}
			}
		}
		#endregion
	}
}