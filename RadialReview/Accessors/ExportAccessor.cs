using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Amazon.ElasticTranscoder.Model;
using ImageResizer.Configuration.Issues;
using RadialReview.Models;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;

namespace RadialReview.Accessors
{
	public class ExportAccessor
	{
		public static byte[] Scorecard(UserOrganizationModel caller, long recurrenceId, string type = "csv")
		{
			var scores = L10Accessor.GetScoresForRecurrence(caller, recurrenceId);

			switch (type.ToLower())
			{
				case "csv":
					{
						var csv = new Csv();
						csv.SetTitle("Measurable");
						foreach (var s in scores.GroupBy(x => x.MeasurableId))
						{
							var ss = s.First();
							csv.Add(ss.Measurable.Title, "Owner", ss.Measurable.AccountableUser.NotNull(x=>x.GetName()));
							csv.Add(ss.Measurable.Title, "Admin", ss.Measurable.AdminUser.NotNull(x=>x.GetName()));
							csv.Add(ss.Measurable.Title, "Goal", "" + ss.Measurable.Goal);
							csv.Add(ss.Measurable.Title, "GoalDirection", "" + ss.Measurable.GoalDirection);
						}
						foreach (var s in scores.OrderBy(x => x.ForWeek))
						{
							csv.Add(s.Measurable.Title, s.ForWeek.ToShortDateString(), s.Measured.NotNull(x => x.Value.ToString()) ?? "");
						}
						return new System.Text.UTF8Encoding().GetBytes(csv.ToCsv());
						//break;
					}
				default: throw new Exception("Unrecognized Type");
			}
		}

		public static async Task<byte[]> TodoList(UserOrganizationModel caller, long recurrenceId,bool includeDetails)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var todos = L10Accessor.GetAllTodosForRecurrence(s, PermissionsUtility.Create(s, caller), recurrenceId);
					var csv = new Csv();
                    //foreach (var t in todos) {
                    //    await 
                    //}
                    var tasks = todos.Select(t=>{
                        return GrabTodo(csv, t, includeDetails);
                    });

                    await Task.WhenAll(tasks);

					return new System.Text.UTF8Encoding().GetBytes(csv.ToCsv(false));
				}
			}
		}

        private static async Task GrabTodo(Csv csv, Models.Todo.TodoModel t,bool includeDetails)
        {

            csv.Add("" + t.Id, "Owner", t.AccountableUser.NotNull(x => x.GetName()));
            csv.Add("" + t.Id, "Created", t.CreateTime.ToShortDateString());
            csv.Add("" + t.Id, "Due Date", t.DueDate.ToShortDateString());
            var time = "";
            if (t.CompleteTime != null)
                time = t.CompleteTime.Value.ToShortDateString();
            csv.Add("" + t.Id, "Completed", time);
            csv.Add("" + t.Id, "To-Do", "" + t.Message);

            if (includeDetails) {
                var padDetails = await PadAccessor.GetText(t.PadId);
                csv.Add("" + t.Id, "Details", "" + padDetails);
            }
        }

		public static async Task<byte[]> IssuesList(UserOrganizationModel caller, long recurrenceId,bool includeDetails)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var issues = L10Accessor.GetAllIssuesForRecurrence(s, PermissionsUtility.Create(s, caller), recurrenceId);
					//var csv = new Csv();
					//foreach (var t in issues)
					//{
					//	csv.Add("" + t.Id, "Owner", t.Owner.NotNull(x=>x.GetName()));
					//	csv.Add("" + t.Id, "Created", t.CreateTime.ToShortDateString());
					//	var time = "";
					//	if (t.CloseTime != null)
					//		time = t.CloseTime.Value.ToShortDateString();
					//	csv.Add("" + t.Id, "Closed", time);
					//	csv.Add("" + t.Id, "Issue", "" + t.Issue.Message);
					//	csv.Add("" + t.Id, "Details", "" + t.Issue.Description);
					//}
					var sb = new StringBuilder();

                    sb.Append("Id,Depth,Owner,Created,Closed,Issue");
                    if (includeDetails){
                        sb.Append(",Details");
                    } 
                    sb.AppendLine();
					//var id = 0;

					var rows = new List<Tuple<long, List<string>>>();

                    var tasks = issues.Select((i, id) => {
                        return RecurseIssue(rows, id, i, 0, includeDetails);
                    });
                    //foreach (var i in issues){
                    //    id++;
                    //    await ;
                    //}
                    await Task.WhenAll(tasks);

					foreach (var r in rows.OrderBy(x => x.Item1)) {
						foreach (var c in r.Item2) {
							sb.Append(c).Append(",");
						}
						sb.AppendLine();
					}


					return new System.Text.UTF8Encoding().GetBytes(sb.ToString());
				}
			}
		}

		public async static Task<List<Tuple<string, byte[]>>> Notes(UserOrganizationModel caller, long recurrenceId)
		{
			var recur = L10Accessor.GetL10Recurrence(caller, recurrenceId, true);
			var lists = new List<Tuple<string, byte[]>>();
			var existing = new Dictionary<string, int>();
			foreach (var note in recur._MeetingNotes)
			{
				var padDetails = await PadAccessor.GetText(note.PadId);
				var bytes = new System.Text.UTF8Encoding().GetBytes(padDetails);
				var append = "";
				if (!existing.ContainsKey(note.Name))
					existing.Add(note.Name, 1);
				else{
					append = " (" + existing[note.Name] + ")";
					existing[note.Name] += 1;
				}
				lists.Add(Tuple.Create(note.Name + append + ".txt", bytes));

			}
			return lists;
		}

		public static byte[] Rocks(UserOrganizationModel caller, long recurrenceId)
		{
			var meetingId = L10Accessor.GetLatestMeetingId(caller, recurrenceId);
			var rocksAndMilestones = L10Accessor.GetRocksForMeeting(caller, recurrenceId, meetingId);
			var csv = new Csv();
			foreach (var rockMilestones in rocksAndMilestones)
			{
				var t = rockMilestones.Rock;
				csv.Add("" + t.Id, "Owner", t.ForRock.Rock);
				csv.Add("" + t.Id, "Created", t.CreateTime.ToShortDateString());
				var time = "";
				if (t.CompleteTime != null)
					time = t.CompleteTime.Value.ToShortDateString();
				csv.Add("" + t.Id, "Completed", time);
				csv.Add("" + t.Id, "Status", t.Completion.ToString());
			}

			return new System.Text.UTF8Encoding().GetBytes(csv.ToCsv(false));
		}
		public static byte[] MeetingSummary(UserOrganizationModel caller, long recurrenceId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					var ratings = s.QueryOver<L10Meeting.L10Meeting_Attendee>().Where(x => x.DeleteTime == null)
						.JoinQueryOver(x=>x.L10Meeting)
						.Where(x=>x.L10RecurrenceId == recurrenceId)
						.Fetch(x=>x.L10Meeting).Eager
						.List().ToList();

					var csv = new Csv();
					foreach (var t in ratings.OrderBy(x => x.L10Meeting.CompleteTime).GroupBy(x => x.L10Meeting.Id)){
						var sum = t.Where(x => x.Rating.HasValue).Select(x => x.Rating.Value).Sum();
						decimal count = t.Where(x => x.Rating.HasValue).Select(x => x.Rating.Value).Count();

						var first = t.First();
						csv.Add("" + first.L10Meeting.Id, "Start Time", first.L10Meeting.CreateTime.ToString("MM/dd/yyyy HH:mm:ss"));
						var time = "";
						if (first.L10Meeting.CompleteTime != null)
							time = first.L10Meeting.CompleteTime.Value.ToString("MM/dd/yyyy HH:mm:ss");
						csv.Add("" + first.L10Meeting.Id, "End Time", time);
						var avg = "";
						if (count > 0)
							avg = String.Format("{0:##.###}", sum/count);
						csv.Add("" + first.L10Meeting.Id, "Average Rating", avg);
					}

					foreach (var t in ratings){
						csv.Add("" + t.L10Meeting.Id, t.User.GetName(), ""+t.Rating);
					}

					return new System.Text.UTF8Encoding().GetBytes(csv.ToCsv(false));
				}
			}
		
			
		}
        public static async Task RecurseIssue(List<Tuple<long,List<string>>> rows, int index, IssueModel.IssueModel_Recurrence parent, int depth, bool includeDetails)
		{
			var cells = new List<string>();
			var row = Tuple.Create((long)index, cells);

			var time = "";
			if (parent.CloseTime != null)
				time = parent.CloseTime.Value.ToShortDateString();
			cells.Add("" + index);
			cells.Add("" + depth);
			cells.Add("" + Csv.CsvQuote(parent.Owner.NotNull(x => x.GetName())));
			cells.Add("" + parent.CreateTime.ToShortDateString());
			cells.Add("" + time);
			cells.Add("" + Csv.CsvQuote(parent.Issue.Message));


			//.Append(depth).Append(",")
			//.Append().Append(",")
			//.Append().Append(",")
			//.Append().Append(",");
			/*		for (var d = 0; d < depth - 1; d++)
						sb.Append(",");*/
			//sb.Append(Csv.CsvQuote(parent.Issue.Message)).Append(",");

            if (includeDetails) {
                var padDetails = await PadAccessor.GetText(parent.Issue.PadId);
				//var bytes = new System.Text.UTF8Encoding().GetBytes(padDetails);
				cells.Add(Csv.CsvQuote(padDetails));
            }

			rows.Add(row);

			foreach(var child in parent._ChildIssues)
                await RecurseIssue(rows, index, child, depth + 1, includeDetails);
		}
	
		

	}
}