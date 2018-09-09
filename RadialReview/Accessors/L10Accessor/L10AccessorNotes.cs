using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.L10;
using RadialReview.Utilities;
//using ListExtensions = WebGrease.Css.Extensions.ListExtensions;
//using System.Web.WebPages.Html;

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