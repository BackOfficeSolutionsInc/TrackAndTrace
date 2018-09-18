using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using RadialReview.Accessors;
using RadialReview.Models.Angular.Todos;
using RadialReview.Utilities;
using NHibernate;
using RadialReview.Variables;

namespace RadialReview.Controllers {
	public class TileDataController : BaseController {
        // GET: TileData

        [Access(AccessLevel.Any)]
        public PartialViewResult FAQTips(){
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					ViewBag.KB = s.GetSettingOrDefault("KB_URL", "https://tractiontools.happyfox.com/kb/");

					tx.Commit();
					s.Flush();
				}
			}
            return PartialView("FAQTips");
        }



        [Access(AccessLevel.Any)]
        public PartialViewResult OrganizationValues(){
            return PartialView("OrganizationValues");
        }


        [Access(AccessLevel.Any)]
        public PartialViewResult L10Notes(long id){
            var note = L10Accessor.GetNote(GetUser(), id);
            ViewBag.Title = note.Name;
            var url = Config.NotesUrl() + "p/" + note.PadId + "?showControls=true&showChat=false";

            return PartialView("L10Notes", url);
        }

        [Access(AccessLevel.Any)]
        public PartialViewResult UserRoles(){
            return PartialView("UserRoles");
        }

        [Access(AccessLevel.Any)]
        public PartialViewResult UserNotes(){
            var key = Config.NotesUrl("p/"+GetUser().Id + GetUser().User.Id);
            return PartialView("UserNotes",key);
        }


        [Access(AccessLevel.Any)]
        public PartialViewResult UserNotifications() {
            return PartialView("UserNotifications");
        }

        [Access(AccessLevel.Any)]
        public PartialViewResult CoreProcesses() {
            return PartialView("CoreProcesses");
        }
        [Access(AccessLevel.Any)]
        public PartialViewResult Tasks() {
            return PartialView("Tasks");
        }

        [Access(AccessLevel.Any)]
        public PartialViewResult UserTodo2() {
            return PartialView("UserTodo", GetUser().Id);
        }

        [Access(AccessLevel.Any)]
        public PartialViewResult Milestones() {
            return PartialView("Milestones", GetUser().Id);
        }

        [Access(AccessLevel.Any)]
		public PartialViewResult UserScorecard2(){
            ViewBag.NumberOfWeeks = TimingUtility.NumberOfWeeks(GetUser());

			return PartialView("UserScorecard");
		}
		[Access(AccessLevel.Any)]
		public PartialViewResult UserRock2() {
			return PartialView("UserRock");
		}

		[Access(AccessLevel.Any)]
		public PartialViewResult UserManage2() {
			return PartialView("UserManage");
		}

		[Access(AccessLevel.User)]
		public PartialViewResult UserProfile2() {
			return PartialView("UserProfile", GetUser().User);
		}

		[Access(AccessLevel.User)]
		public PartialViewResult UserButtons() {
			return PartialView("UserButtons");
		}

		protected void SetupViewBag(long id) {
			ViewBag.HeadingStyle = "background-color: hsl(" + (new Random(id.GetHashCode()).Next(0, 360)) + ",32%,85%)";
		}
		[Access(AccessLevel.User)]
		public PartialViewResult L10Todos(long id) {
			SetupViewBag(id);
			return PartialView("L10Todos", id);
		}

		[Access(AccessLevel.User)]
		public PartialViewResult L10Headlines(long id) {
			SetupViewBag(id);
			return PartialView("L10Headlines", id);
		}

		[Access(AccessLevel.Any)]
		public PartialViewResult L10Stats(long id) {
			ViewBag.Name = "L10 Stats";
			try {
				ViewBag.Name = L10Accessor.GetL10Recurrence(GetUser(), id, LoadMeeting.False()).Name;
			} catch (Exception) {
			}

			return PartialView("L10Stats", id);
		}

		[Access(AccessLevel.User)]
		public PartialViewResult L10Scorecard(long id) {
			SetupViewBag(id);
			return PartialView("L10Scorecard", id);
		}

		[Access(AccessLevel.User)]
		public PartialViewResult L10Issues(long id) {
			SetupViewBag(id);
			return PartialView("L10Issues", id);
		}

		[Access(AccessLevel.User)]
		public PartialViewResult L10SolvedIssues(long id) {
			SetupViewBag(id);
			return PartialView("L10SolvedIssues", id);
		}

		[Access(AccessLevel.User)]
		public PartialViewResult L10Rocks(long id) {
			SetupViewBag(id);
			return PartialView("L10Rocks", id);
		}

		[Access(AccessLevel.User)]
		public PartialViewResult SoftwareUpdates(int days = 14) {
			var daysAgo = DateTime.UtcNow.Date.Subtract(TimeSpan.FromDays(days));
			var daysStr = daysAgo.ToString("yyyyMMdd");
			var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"SoftwareUpdates\");

			var files = Directory.GetFiles(path).Where(x => x.CompareTo(daysStr) >= 0).ToList();

			var groups = new List<SoftwareUpdateGroup>();

			foreach (var f in files) {
				var date = DateTime.MinValue;
				var html = "<i>Could not read update</i>";
				var title = "";
				try {
					var text = string.Join("\r\n", FileUtilities.WriteSafeReadAllLines(f));
					var file = f.Substring(f.LastIndexOf("\\") + 1);
					html = CommonMark.CommonMarkConverter.Convert(text);
					date = new DateTime(file.Substring(0, 4).ToInt(), file.Substring(4, 2).ToInt(), file.Substring(6, 2).ToInt());
					title = file.Substring(8).Replace(".txt", "");
				} catch (Exception) {

				}
				groups.Add(new SoftwareUpdateGroup() {
					Date = date,
					Markup = new HtmlString(html),
					Title = title
				});
			}

			return PartialView("SoftwareUpdates", groups);
		}

		public class SoftwareUpdateGroup {
			public DateTime Date { get; set; }
			public HtmlString Markup { get; set; }
			public string Title { get; set; }
		}
	}
}
